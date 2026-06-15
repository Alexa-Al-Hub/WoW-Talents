using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TalentTreeEditorWindow : EditorWindow
{
    private TalentTreeSO _tree;

    private int     _selectedIdx      = -1;
    private bool    _isDragging;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartNodePos;
    private bool    _snapToGrid = true;
    private Vector2 _scroll;

    // Must match TalentTreePresenter serialized fields
    private const float RuntimeCellSize = 64f;
    private const float RuntimeSpacing  = 10f;
    private const float RuntimeStep     = RuntimeCellSize + RuntimeSpacing;

    // Max grid extent used only for drag clamping
    private const int MaxCols = 8;
    private const int MaxRows = 11;

    // Preview panel size — set to (containerWidth / treeCount) × containerHeight from the game
    private Vector2 _panelSize = new Vector2(600f, 890f);

    private const float InspectorH = 96f;

    private static readonly Color BgColor      = new Color(0.14f, 0.14f, 0.14f);
    private static readonly Color GridColor     = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color BgDimOverlay  = new Color(0f, 0f, 0f, 0.4f);
    private static readonly Color NodeColor     = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color SelectedColor = new Color(0.85f, 0.65f, 0.05f, 0.9f);
    private static readonly Color ShadowColor   = new Color(0f, 0f, 0f, 0.45f);

    private static GUIStyle _labelStyle;
    private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleCenter,
        wordWrap  = false,
        normal    = { textColor = Color.white }
    };

    // ── Open ──────────────────────────────────────────────────────────────────

    [MenuItem("WoW Talents/Talent Tree Editor")]
    public static void Open() => GetWindow<TalentTreeEditorWindow>("Talent Tree Editor");

    public static void OpenWith(TalentTreeSO tree)
    {
        var window = GetWindow<TalentTreeEditorWindow>("Talent Tree Editor");
        window.SetTree(tree);
    }

    // ── Unity callbacks ───────────────────────────────────────────────────────

    private void OnSelectionChange()
    {
        if (Selection.activeObject is TalentTreeSO selectedTree) SetTree(selectedTree);
    }

    private void SetTree(TalentTreeSO tree)
    {
        _tree        = tree;
        _selectedIdx = -1;
        Repaint();
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawToolbar();

        if (_tree == null)
        {
            EditorGUILayout.HelpBox("Select a TalentTreeSO asset in the Project window.", MessageType.Info);
            return;
        }

        DrawCanvas();
        DrawInspectorPanel();
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(_tree != null ? _tree.TreeName : "—", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _snapToGrid = GUILayout.Toggle(_snapToGrid, "Snap", EditorStyles.toolbarButton, GUILayout.Width(56));
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton)) AddNode();
            GUI.enabled = _selectedIdx >= 0;
            if (GUILayout.Button("Remove", EditorStyles.toolbarButton)) RemoveSelected();
            GUI.enabled = true;
        }
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private void DrawCanvas()
    {
        float toolbarH    = EditorStyles.toolbar.fixedHeight;
        float canvasViewH = position.height - toolbarH - InspectorH - 8f;

        (float scaledStep, float scaledCell) = ComputePreviewScale();
        float canvasW = Mathf.Max(_panelSize.x, 1f);
        float canvasH = Mathf.Max(_panelSize.y, 1f);

        Rect viewRect = GUILayoutUtility.GetRect(position.width, canvasViewH);
        _scroll = GUI.BeginScrollView(viewRect, _scroll, new Rect(0, 0, canvasW, canvasH));

        EditorGUI.DrawRect(new Rect(0, 0, canvasW, canvasH), BgColor);

        if (_tree.BackgroundSprite != null)
        {
            GUI.DrawTexture(new Rect(0, 0, canvasW, canvasH), _tree.BackgroundSprite.texture, ScaleMode.StretchToFill);
            EditorGUI.DrawRect(new Rect(0, 0, canvasW, canvasH), BgDimOverlay);
        }

        DrawGridLines(canvasW, canvasH, scaledStep);

        if (_tree.Nodes != null)
            for (int i = 0; i < _tree.Nodes.Count; i++)
                DrawNode(i, scaledStep, scaledCell);

        HandleCanvasEvents(scaledStep, scaledCell);

        GUI.EndScrollView();
    }

    // Mirrors TalentTreePresenter.BuildTrees() scale so editor and game match exactly.
    private (float scaledStep, float scaledCell) ComputePreviewScale()
    {
        float maxNodeX = 0f, maxNodeY = 0f;
        if (_tree.Nodes != null)
            foreach (var node in _tree.Nodes)
            {
                if (node == null) continue;
                maxNodeX = Mathf.Max(maxNodeX, node.X);
                maxNodeY = Mathf.Max(maxNodeY, node.Y);
            }

        float contentWidth  = maxNodeX * RuntimeStep + RuntimeCellSize;
        float contentHeight = maxNodeY * RuntimeStep + RuntimeCellSize;

        if (contentWidth <= 0f || contentHeight <= 0f)
            return (RuntimeStep, RuntimeCellSize);

        float panelW = Mathf.Max(_panelSize.x, 1f);
        float panelH = Mathf.Max(_panelSize.y, 1f);

        float scale = Mathf.Min(panelW / contentWidth, panelH / contentHeight);
        return (RuntimeStep * scale, RuntimeCellSize * scale);
    }

    private void DrawGridLines(float canvasW, float canvasH, float scaledStep)
    {
        for (float x = 0; x < canvasW; x += scaledStep)
            EditorGUI.DrawRect(new Rect(x, 0, 1, canvasH), GridColor);
        for (float y = 0; y < canvasH; y += scaledStep)
            EditorGUI.DrawRect(new Rect(0, y, canvasW, 1), GridColor);
    }

    private void DrawNode(int idx, float scaledStep, float scaledCell)
    {
        var node = _tree.Nodes[idx];
        if (node == null) return;

        Rect nodeRect = NodeRect(node.X, node.Y, scaledStep, scaledCell);
        bool selected = idx == _selectedIdx;

        EditorGUI.DrawRect(new Rect(nodeRect.x + 2, nodeRect.y + 2, nodeRect.width, nodeRect.height), ShadowColor);
        EditorGUI.DrawRect(nodeRect, selected ? SelectedColor : NodeColor);

        var icon = node.Definition?.Icon;
        if (icon != null)
            GUI.DrawTexture(new Rect(nodeRect.x + 4, nodeRect.y + 4, nodeRect.width - 8, nodeRect.height - 20),
                            icon.texture, ScaleMode.ScaleToFit);

        string label = node.Definition ? node.Definition.DisplayName : "(empty)";
        GUI.Label(new Rect(nodeRect.x, nodeRect.yMax - 18, nodeRect.width, 18), label, LabelStyle);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void HandleCanvasEvents(float scaledStep, float scaledCell)
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;

        switch (currentEvent.type)
        {
            case EventType.MouseDown when currentEvent.button == 0:
            {
                int hit = NodeAt(currentEvent.mousePosition, scaledStep, scaledCell);
                _selectedIdx = hit;
                if (hit >= 0)
                {
                    _isDragging       = true;
                    _dragStartMouse   = currentEvent.mousePosition;
                    _dragStartNodePos = new Vector2(_tree.Nodes[hit].X, _tree.Nodes[hit].Y);
                }
                Repaint();
                currentEvent.Use();
                break;
            }

            case EventType.MouseDrag when _isDragging && _selectedIdx >= 0:
            {
                Vector2 delta = currentEvent.mousePosition - _dragStartMouse;
                float newX = _dragStartNodePos.x + delta.x / scaledStep;
                float newY = _dragStartNodePos.y + delta.y / scaledStep;
                if (_snapToGrid) { newX = Mathf.Round(newX); newY = Mathf.Round(newY); }
                newX = Mathf.Clamp(newX, 0, MaxCols - 1);
                newY = Mathf.Clamp(newY, 0, MaxRows - 1);
                Undo.RecordObject(_tree, "Move Talent Node");
                _tree.Nodes[_selectedIdx].X = newX;
                _tree.Nodes[_selectedIdx].Y = newY;
                EditorUtility.SetDirty(_tree);
                Repaint();
                currentEvent.Use();
                break;
            }

            case EventType.MouseUp when currentEvent.button == 0:
                _isDragging = false;
                currentEvent.Use();
                break;

            case EventType.KeyDown when currentEvent.keyCode == KeyCode.Delete && _selectedIdx >= 0:
                RemoveSelected();
                currentEvent.Use();
                break;
        }
    }

    // ── Inspector panel ───────────────────────────────────────────────────────

    private void DrawInspectorPanel()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);

        _panelSize = EditorGUILayout.Vector2Field("Panel size (W × H)", _panelSize);
        EditorGUILayout.Space(4);

        if (_selectedIdx < 0 || _tree.Nodes == null || _selectedIdx >= _tree.Nodes.Count)
        {
            EditorGUILayout.HelpBox("Click a node to select it.", MessageType.None);
            return;
        }

        var node = _tree.Nodes[_selectedIdx];

        EditorGUI.BeginChangeCheck();

        var newDefinition = (TalentDefinitionSO)EditorGUILayout.ObjectField(
            "Talent", node.Definition, typeof(TalentDefinitionSO), false);

        float newX = EditorGUILayout.FloatField("X (col)", node.X);
        float newY = EditorGUILayout.FloatField("Y (row)", node.Y);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_tree, "Edit Talent Node");
            node.Definition = newDefinition;
            node.X = _snapToGrid ? Mathf.Round(newX) : newX;
            node.Y = _snapToGrid ? Mathf.Round(newY) : newY;
            EditorUtility.SetDirty(_tree);
            Repaint();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Rect NodeRect(float x, float y, float scaledStep, float scaledCell) =>
        new Rect(x * scaledStep, y * scaledStep, scaledCell, scaledCell);

    private int NodeAt(Vector2 pos, float scaledStep, float scaledCell)
    {
        if (_tree.Nodes == null) return -1;
        for (int i = _tree.Nodes.Count - 1; i >= 0; i--)
        {
            var node = _tree.Nodes[i];
            if (node != null && NodeRect(node.X, node.Y, scaledStep, scaledCell).Contains(pos)) return i;
        }
        return -1;
    }

    private void AddNode()
    {
        if (_tree == null) return;
        Undo.RecordObject(_tree, "Add Talent Node");
        if (_tree.Nodes == null) _tree.Nodes = new List<TalentNodeData>();
        _tree.Nodes.Add(new TalentNodeData());
        EditorUtility.SetDirty(_tree);
        _selectedIdx = _tree.Nodes.Count - 1;
        Repaint();
    }

    private void RemoveSelected()
    {
        if (_selectedIdx < 0 || _tree?.Nodes == null || _selectedIdx >= _tree.Nodes.Count) return;
        Undo.RecordObject(_tree, "Remove Talent Node");
        _tree.Nodes.RemoveAt(_selectedIdx);
        EditorUtility.SetDirty(_tree);
        _selectedIdx = Mathf.Min(_selectedIdx, _tree.Nodes.Count - 1);
        Repaint();
    }
}

// ── Custom Inspector button ───────────────────────────────────────────────────

[CustomEditor(typeof(TalentTreeSO))]
public class TalentTreeSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open in Talent Tree Editor", GUILayout.Height(28)))
            TalentTreeEditorWindow.OpenWith((TalentTreeSO)target);

        EditorGUILayout.Space(4);
        DrawDefaultInspector();
    }
}
