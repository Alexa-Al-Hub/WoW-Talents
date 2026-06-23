using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TalentTree
{
    public class TalentTreeEditorWindow : EditorWindow
    {
        private TalentTreeSO _tree;

        private int _selectedNodeIndex = -1;
        private bool _isDragging;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartNodePos;
        private bool _snapToGrid = true;
        private Vector2 _sidebarScroll;

        // Must match TalentTreePresenter serialized fields
        private const float RuntimeCellSize = 64f;
        private const float RuntimeSpacing = 10f;
        private const float RuntimeStep = RuntimeCellSize + RuntimeSpacing;

        // Grid extent of the tree. WoW Classic trees are 4 columns wide; rows vary by tree.
        private int _columns = 4;
        private int _rows = 7;

        private const float PanelHeight = 190f;

        private static readonly Color BgColor = new Color(0.14f, 0.14f, 0.14f);
        private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BgDimOverlay = new Color(0f, 0f, 0f, 0.4f);
        private static readonly Color NodeColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color SelectedColor = new Color(0.85f, 0.65f, 0.05f, 0.9f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color SidebarColor = new Color(0.18f, 0.18f, 0.18f);

        private static GUIStyle _labelStyle;
        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            normal = { textColor = Color.white }
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
            if (Selection.activeObject is TalentTreeSO selectedTree)
            {
                SetTree(selectedTree);
            }
        }

        private void SetTree(TalentTreeSO tree)
        {
            _tree = tree;
            _selectedNodeIndex = -1;
            Repaint();
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawToolbar();

            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            float panelHeight = Mathf.Clamp(PanelHeight, 120f, position.height - toolbarHeight - 80f);

            Rect canvasRect = new Rect(0f, toolbarHeight,
                                       position.width, position.height - toolbarHeight - panelHeight);
            Rect panelRect = new Rect(0f, canvasRect.yMax,
                                      position.width, panelHeight);

            if (_tree != null)
            {
                DrawCanvas(canvasRect);
            }
            else
            {
                EditorGUI.LabelField(canvasRect, "Load a tree from the panel below ↓", EditorStyles.centeredGreyMiniLabel);
            }

            DrawSidebar(panelRect);
        }

        // ── Toolbar ───────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_tree != null ? _tree.TreeName : "—", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                _snapToGrid = GUILayout.Toggle(_snapToGrid, "Snap", EditorStyles.toolbarButton, GUILayout.Width(56));
                if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
                {
                    AddNode();
                }
                GUI.enabled = _selectedNodeIndex >= 0;
                if (GUILayout.Button("Remove", EditorStyles.toolbarButton))
                {
                    RemoveSelected();
                }
                GUI.enabled = true;
            }
        }

        // ── Canvas ────────────────────────────────────────────────────────────────

        private void DrawCanvas(Rect viewRect)
        {
            float scaledStep = RuntimeStep;
            float scaledCell = RuntimeCellSize;

            // Canvas spans the full column × row grid, in runtime units.
            float canvasWidth = Mathf.Max(_columns, 1) * RuntimeStep;
            float canvasHeight = Mathf.Max(_rows, 1) * RuntimeStep;

            // Shrink the whole preview so the entire grid — and every node — fits the view
            // without scrolling. Uniform scale keeps the grid's aspect ratio.
            float fitScale = Mathf.Min(viewRect.width / canvasWidth, viewRect.height / canvasHeight);
            canvasWidth *= fitScale;
            canvasHeight *= fitScale;
            scaledStep *= fitScale;
            scaledCell *= fitScale;

            // Computed in window space, before BeginGroup re-bases the mouse to group-local coords.
            bool mouseOverCanvas = new Rect(viewRect.x, viewRect.y, canvasWidth, canvasHeight)
                .Contains(Event.current.mousePosition);

            GUI.BeginGroup(viewRect);

            EditorGUI.DrawRect(new Rect(0, 0, canvasWidth, canvasHeight), BgColor);

            var backgroundSprite = GetBackgroundSprite();
            if (backgroundSprite != null)
            {
                GUI.DrawTexture(new Rect(0, 0, canvasWidth, canvasHeight), backgroundSprite.texture, ScaleMode.StretchToFill);
                EditorGUI.DrawRect(new Rect(0, 0, canvasWidth, canvasHeight), BgDimOverlay);
            }

            DrawGridLines(canvasWidth, canvasHeight, scaledStep);

            if (_tree.Nodes != null)
            {
                for (int i = 0; i < _tree.Nodes.Count; i++)
                {
                    DrawNode(i, scaledStep, scaledCell);
                }
            }

            // Only the canvas consumes pointer input, so clicks in the panel don't deselect nodes.
            if (mouseOverCanvas)
            {
                HandleCanvasEvents(scaledStep, scaledCell);
            }

            GUI.EndGroup();
        }

        // Pulls the preview sprite from the first Image found in the background prefab.
        private Sprite GetBackgroundSprite()
        {
            if (_tree.BackgroundPrefab == null)
            {
                return null;
            }

            var backgroundImage = _tree.BackgroundPrefab.GetComponentInChildren<Image>(true);
            return backgroundImage != null ? backgroundImage.sprite : null;
        }

        private void DrawGridLines(float canvasWidth, float canvasHeight, float scaledStep)
        {
            for (float x = 0; x < canvasWidth; x += scaledStep)
            {
                EditorGUI.DrawRect(new Rect(x, 0, 1, canvasHeight), GridColor);
            }
            for (float y = 0; y < canvasHeight; y += scaledStep)
            {
                EditorGUI.DrawRect(new Rect(0, y, canvasWidth, 1), GridColor);
            }
        }

        private void DrawNode(int nodeIndex, float scaledStep, float scaledCell)
        {
            var node = _tree.Nodes[nodeIndex];
            if (node == null)
            {
                return;
            }

            Rect nodeRect = NodeRect(node.X, node.Y, scaledStep, scaledCell);
            bool selected = nodeIndex == _selectedNodeIndex;

            EditorGUI.DrawRect(new Rect(nodeRect.x + 2, nodeRect.y + 2, nodeRect.width, nodeRect.height), ShadowColor);
            EditorGUI.DrawRect(nodeRect, selected ? SelectedColor : NodeColor);

            var icon = node.Definition?.Icon;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(nodeRect.x + 4, nodeRect.y + 4, nodeRect.width - 8, nodeRect.height - 20),
                                icon.texture, ScaleMode.ScaleToFit);
            }

            string label = node.Definition ? node.Definition.DisplayName : "(empty)";
            GUI.Label(new Rect(nodeRect.x, nodeRect.yMax - 18, nodeRect.width, 18), label, LabelStyle);
        }

        // ── Events ────────────────────────────────────────────────────────────────

        private void HandleCanvasEvents(float scaledStep, float scaledCell)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            switch (currentEvent.type)
            {
                case EventType.MouseDown when currentEvent.button == 0:
                {
                    int clickedNodeIndex = NodeAt(currentEvent.mousePosition, scaledStep, scaledCell);
                    _selectedNodeIndex = clickedNodeIndex;
                    if (clickedNodeIndex >= 0)
                    {
                        _isDragging = true;
                        _dragStartMouse = currentEvent.mousePosition;
                        _dragStartNodePos = new Vector2(_tree.Nodes[clickedNodeIndex].X, _tree.Nodes[clickedNodeIndex].Y);
                    }
                    Repaint();
                    currentEvent.Use();
                    break;
                }

                case EventType.MouseDrag when _isDragging && _selectedNodeIndex >= 0:
                {
                    Vector2 delta = currentEvent.mousePosition - _dragStartMouse;
                    float newX = _dragStartNodePos.x + delta.x / scaledStep;
                    float newY = _dragStartNodePos.y + delta.y / scaledStep;
                    if (_snapToGrid) { newX = Mathf.Round(newX); newY = Mathf.Round(newY); }
                    newX = Mathf.Clamp(newX, 0, _columns - 1);
                    newY = Mathf.Clamp(newY, 0, _rows - 1);
                    Undo.RecordObject(_tree, "Move Talent Node");
                    _tree.Nodes[_selectedNodeIndex].X = newX;
                    _tree.Nodes[_selectedNodeIndex].Y = newY;
                    EditorUtility.SetDirty(_tree);
                    Repaint();
                    currentEvent.Use();
                    break;
                }

                case EventType.MouseUp when currentEvent.button == 0:
                    _isDragging = false;
                    currentEvent.Use();
                    break;

                case EventType.KeyDown when currentEvent.keyCode == KeyCode.Delete && _selectedNodeIndex >= 0:
                    RemoveSelected();
                    currentEvent.Use();
                    break;
            }
        }

        // ── Inspector panel ───────────────────────────────────────────────────────

        private void DrawSidebar(Rect panelRect)
        {
            EditorGUI.DrawRect(panelRect, SidebarColor);

            GUILayout.BeginArea(new RectOffset(8, 8, 8, 8).Remove(panelRect));

            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);
            DrawNodeProperties();
            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawTreeLoader()
        {
            EditorGUILayout.LabelField("Tree", EditorStyles.boldLabel);

            string currentLabel = _tree != null ? _tree.name : "(none selected)";
            if (EditorGUILayout.DropdownButton(new GUIContent(currentLabel), FocusType.Keyboard))
            {
                ShowTreeMenu();
            }
        }

        private void ShowTreeMenu()
        {
            var menu = new GenericMenu();
            var treeAssetGuids = AssetDatabase.FindAssets("t:TalentTreeSO");

            if (treeAssetGuids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No TalentTreeSO assets found"));
            }

            foreach (string guid in treeAssetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<TalentTreeSO>(assetPath);
                if (tree == null)
                {
                    continue;
                }

                menu.AddItem(new GUIContent(tree.name), tree == _tree, () => SetTree(tree));
            }

            menu.ShowAsContext();
        }

        private void DrawNodeProperties()
        {
            DrawTreeLoader();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            _columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _columns));
            _rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _rows));
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (_tree == null)
            {
                EditorGUILayout.HelpBox("Load a tree to edit its nodes.", MessageType.Info);
                return;
            }

            if (_selectedNodeIndex < 0 || _tree.Nodes == null || _selectedNodeIndex >= _tree.Nodes.Count)
            {
                EditorGUILayout.HelpBox("Click a node to select it.", MessageType.None);
                return;
            }

            var node = _tree.Nodes[_selectedNodeIndex];

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

        private int NodeAt(Vector2 pointerPosition, float scaledStep, float scaledCell)
        {
            if (_tree.Nodes == null)
            {
                return -1;
            }
            for (int i = _tree.Nodes.Count - 1; i >= 0; i--)
            {
                var node = _tree.Nodes[i];
                if (node != null && NodeRect(node.X, node.Y, scaledStep, scaledCell).Contains(pointerPosition))
                {
                    return i;
                }
            }
            return -1;
        }

        private void AddNode()
        {
            if (_tree == null)
            {
                return;
            }
            Undo.RecordObject(_tree, "Add Talent Node");
            if (_tree.Nodes == null)
            {
                _tree.Nodes = new List<TalentNodeData>();
            }
            _tree.Nodes.Add(new TalentNodeData());
            EditorUtility.SetDirty(_tree);
            _selectedNodeIndex = _tree.Nodes.Count - 1;
            Repaint();
        }

        private void RemoveSelected()
        {
            if (_selectedNodeIndex < 0 || _tree?.Nodes == null || _selectedNodeIndex >= _tree.Nodes.Count)
            {
                return;
            }
            Undo.RecordObject(_tree, "Remove Talent Node");
            _tree.Nodes.RemoveAt(_selectedNodeIndex);
            EditorUtility.SetDirty(_tree);
            _selectedNodeIndex = Mathf.Min(_selectedNodeIndex, _tree.Nodes.Count - 1);
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
            {
                TalentTreeEditorWindow.OpenWith((TalentTreeSO)target);
            }

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }
}
