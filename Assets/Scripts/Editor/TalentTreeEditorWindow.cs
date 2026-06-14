using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TalentTreeEditorWindow : EditorWindow
{
    private TalentTreeSO _tree;

    private int     _selectedIdx     = -1;
    private bool    _isDragging;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartNodePos;
    private bool    _snapToGrid = true;
    private Vector2 _scroll;

    private const float CELL = 72f;
    private const float GAP  = 14f;
    private const float STEP = CELL + GAP;
    private const int   COLS = 8;
    private const int   ROWS = 10;

    private const float InspectorH = 96f;

    private static readonly Color BgColor       = new Color(0.14f, 0.14f, 0.14f);
    private static readonly Color GridColor      = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color NodeColor      = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color SelectedColor  = new Color(0.85f, 0.65f, 0.05f, 0.9f);
    private static readonly Color ShadowColor    = new Color(0f, 0f, 0f, 0.45f);

    private static GUIStyle _labelStyle;
    private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleCenter,
        wordWrap  = false,
        normal    = { textColor = Color.white }
    };

    // ── Open ─────────────────────────────────────────────────────────────────────

    [MenuItem("WoW Talents/Talent Tree Editor")]
    public static void Open() => GetWindow<TalentTreeEditorWindow>("Talent Tree Editor");

    public static void OpenWith(TalentTreeSO tree)
    {
        var w = GetWindow<TalentTreeEditorWindow>("Talent Tree Editor");
        w.SetTree(tree);
    }

    // ── Unity callbacks ───────────────────────────────────────────────────────────

    private void OnSelectionChange()
    {
        if (Selection.activeObject is TalentTreeSO t) SetTree(t);
    }

    private void SetTree(TalentTreeSO tree)
    {
        _tree        = tree;
        _selectedIdx = -1;
        Repaint();
    }

    // ── OnGUI ────────────────────────────────────────────────────────────────────

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

    // ── Toolbar ──────────────────────────────────────────────────────────────────

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

    // ── Canvas ───────────────────────────────────────────────────────────────────

    private void DrawCanvas()
    {
        float toolbarH    = EditorStyles.toolbar.fixedHeight;
        float canvasViewH = position.height - toolbarH - InspectorH - 8f;

        float canvasW = COLS * STEP + GAP;
        float canvasH = ROWS * STEP + GAP;

        Rect viewRect = GUILayoutUtility.GetRect(position.width, canvasViewH);

        _scroll = GUI.BeginScrollView(viewRect, _scroll, new Rect(0, 0, canvasW, canvasH));

        EditorGUI.DrawRect(new Rect(0, 0, canvasW, canvasH), BgColor);
        DrawGridLines(canvasW, canvasH);

        if (_tree.Nodes != null)
            for (int i = 0; i < _tree.Nodes.Count; i++)
                DrawNode(i);

        HandleCanvasEvents();

        GUI.EndScrollView();
    }

    private void DrawGridLines(float canvasW, float canvasH)
    {
        for (int c = 0; c <= COLS; c++)
        {
            float x = GAP * 0.5f + c * STEP - 0.5f;
            EditorGUI.DrawRect(new Rect(x, 0, 1, canvasH), GridColor);
        }
        for (int r = 0; r <= ROWS; r++)
        {
            float y = GAP * 0.5f + r * STEP - 0.5f;
            EditorGUI.DrawRect(new Rect(0, y, canvasW, 1), GridColor);
        }
    }

    private void DrawNode(int idx)
    {
        var node = _tree.Nodes[idx];
        if (node == null) return;

        Rect r   = NodeRect(node.X, node.Y);
        bool sel = idx == _selectedIdx;

        EditorGUI.DrawRect(new Rect(r.x + 2, r.y + 2, r.width, r.height), ShadowColor);
        EditorGUI.DrawRect(r, sel ? SelectedColor : NodeColor);

        var icon = node.Definition?.Icon;
        if (icon != null)
            GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 20),
                            icon.texture, ScaleMode.ScaleToFit);

        string label = node.Definition ? node.Definition.DisplayName : "(empty)";
        GUI.Label(new Rect(r.x, r.yMax - 18, r.width, 18), label, LabelStyle);
    }

    // ── Events ───────────────────────────────────────────────────────────────────

    private void HandleCanvasEvents()
    {
        Event e = Event.current;
        if (e == null) return;

        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
            {
                int hit = NodeAt(e.mousePosition);
                _selectedIdx = hit;
                if (hit >= 0)
                {
                    _isDragging       = true;
                    _dragStartMouse   = e.mousePosition;
                    _dragStartNodePos = new Vector2(_tree.Nodes[hit].X, _tree.Nodes[hit].Y);
                }
                Repaint();
                e.Use();
                break;
            }

            case EventType.MouseDrag when _isDragging && _selectedIdx >= 0:
            {
                Vector2 delta = e.mousePosition - _dragStartMouse;
                float newX = _dragStartNodePos.x + delta.x / STEP;
                float newY = _dragStartNodePos.y + delta.y / STEP;
                if (_snapToGrid) { newX = Mathf.Round(newX); newY = Mathf.Round(newY); }
                newX = Mathf.Clamp(newX, 0, COLS - 1);
                newY = Mathf.Clamp(newY, 0, ROWS - 1);
                Undo.RecordObject(_tree, "Move Talent Node");
                _tree.Nodes[_selectedIdx].X = newX;
                _tree.Nodes[_selectedIdx].Y = newY;
                EditorUtility.SetDirty(_tree);
                Repaint();
                e.Use();
                break;
            }

            case EventType.MouseUp when e.button == 0:
                _isDragging = false;
                e.Use();
                break;

            case EventType.KeyDown when e.keyCode == KeyCode.Delete && _selectedIdx >= 0:
                RemoveSelected();
                e.Use();
                break;
        }
    }

    // ── Inspector panel ───────────────────────────────────────────────────────────

    private void DrawInspectorPanel()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);

        if (_selectedIdx < 0 || _tree.Nodes == null || _selectedIdx >= _tree.Nodes.Count)
        {
            EditorGUILayout.HelpBox("Click a node to select it.", MessageType.None);
            return;
        }

        var node = _tree.Nodes[_selectedIdx];

        EditorGUI.BeginChangeCheck();

        var newDef = (TalentDefinitionSO)EditorGUILayout.ObjectField(
            "Talent", node.Definition, typeof(TalentDefinitionSO), false);

        float newX = EditorGUILayout.FloatField("X (col)", node.X);
        float newY = EditorGUILayout.FloatField("Y (row)", node.Y);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_tree, "Edit Talent Node");
            node.Definition = newDef;
            node.X = _snapToGrid ? Mathf.Round(newX) : newX;
            node.Y = _snapToGrid ? Mathf.Round(newY) : newY;
            EditorUtility.SetDirty(_tree);
            Repaint();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private Rect NodeRect(float x, float y) =>
        new Rect(GAP * 0.5f + x * STEP, GAP * 0.5f + y * STEP, CELL, CELL);

    private int NodeAt(Vector2 pos)
    {
        if (_tree.Nodes == null) return -1;
        for (int i = _tree.Nodes.Count - 1; i >= 0; i--)
        {
            var n = _tree.Nodes[i];
            if (n != null && NodeRect(n.X, n.Y).Contains(pos)) return i;
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

// ── Custom Inspector button ───────────────────────────────────────────────────────

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
