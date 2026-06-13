using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TalentTreePresenter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterSO _classData;

    [Header("UI")]
    [SerializeField] private TalentNodeView _nodePrefab;
    [SerializeField] private RectTransform  _arrowPrefab;
    [SerializeField] private RectTransform  _container;

    [Header("Layout")]
    [SerializeField] private float _cellSize    = 64f;
    [SerializeField] private float _cellSpacing = 10f;
    [SerializeField] private float _treePadding = 40f;

    private PlayerTalentManager _talentManager;
    private readonly List<TalentNodeView> _spawnedNodes = new();

    private void Start()
    {
        _talentManager = new PlayerTalentManager(51);
        BuildTrees();
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    private void BuildTrees()
    {
        if (_classData == null || _nodePrefab == null || _container == null)
        {
            Debug.LogError("TalentTreePresenter: assign all references in the Inspector.");
            return;
        }

        float totalWidth   = 0f;
        int   globalMaxRow = 0;

        foreach (var tree in _classData.Trees)
        {
            int maxCol = tree.Nodes.Count > 0 ? tree.Nodes.Max(n => n.Column) : 0;
            totalWidth += (maxCol + 1) * (_cellSize + _cellSpacing);

            int maxRow = tree.Nodes.Count > 0 ? tree.Nodes.Max(n => n.Row) : 0;
            if (maxRow > globalMaxRow) globalMaxRow = maxRow;
        }

        totalWidth -= _cellSpacing;
        totalWidth += (_classData.Trees.Count - 1) * _treePadding;

        float totalHeight = (globalMaxRow + 1) * (_cellSize + _cellSpacing) - _cellSpacing;
        float currentX    = -totalWidth  / 2f;
        float startY      =  totalHeight / 2f;

        foreach (var tree in _classData.Trees)
        {
            var treeRoot = new GameObject(tree.TreeName).AddComponent<RectTransform>();
            treeRoot.SetParent(_container, false);
            treeRoot.anchorMin        = new Vector2(0.5f, 0.5f);
            treeRoot.anchorMax        = new Vector2(0.5f, 0.5f);
            treeRoot.pivot            = new Vector2(0f, 1f);
            treeRoot.anchoredPosition = new Vector2(currentX, startY);

            BuildTree(tree, treeRoot);

            int maxCol  = tree.Nodes.Count > 0 ? tree.Nodes.Max(n => n.Column) : 0;
            currentX   += (maxCol + 1) * (_cellSize + _cellSpacing) + _treePadding;
        }

        RefreshAllNodes();
    }

    private void BuildTree(TalentTreeSO treeData, RectTransform treeRoot)
    {
        float step = _cellSize + _cellSpacing;
        float half = _cellSize * 0.5f;

        // Arrows first so node backgrounds render on top and mask where they touch cell edges
        if (_arrowPrefab != null)
            DrawConnections(treeData, treeRoot, step, half);

        foreach (var nodeData in treeData.Nodes)
        {
            var nodeView = Instantiate(_nodePrefab, treeRoot);
            nodeView.Initialize(nodeData.Definition);

            var rect = nodeView.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.sizeDelta        = new Vector2(_cellSize, _cellSize);
            rect.anchoredPosition = CellCenter(nodeData.Column, nodeData.Row, step, half);

            nodeView.OnTalentClicked += HandleTalentClicked;
            _spawnedNodes.Add(nodeView);
        }
    }

    // ── Connections ──────────────────────────────────────────────────────────

    private void DrawConnections(TalentTreeSO treeData, RectTransform treeRoot, float step, float half)
    {
        var byId = new Dictionary<string, TalentNodeData>();
        foreach (var nd in treeData.Nodes)
            if (nd.Definition != null && !string.IsNullOrEmpty(nd.Definition.Id))
                byId[nd.Definition.Id] = nd;

        float lw = _cellSize * 0.2f;

        foreach (var nodeData in treeData.Nodes)
        {
            var req = nodeData.Definition?.RequiredTalent;
            if (req == null) continue;
            if (!byId.TryGetValue(req.Id, out var parentData)) continue;

            Vector2 parentCenter = CellCenter(parentData.Column, parentData.Row, step, half);
            Vector2 childCenter  = CellCenter(nodeData.Column,   nodeData.Row,   step, half);

            // Single arrow from the parent's edge to the child's edge.
            // The direction is computed from center-to-center; each endpoint is
            // pulled back by half a cell so the arrow sits in the gap between cells.
            Vector2 dir  = (childCenter - parentCenter).normalized;
            Vector2 from = parentCenter + dir * half;
            Vector2 to   = childCenter  - dir * half;

            SpawnArrow(treeRoot, from, to, lw);
        }
    }

    private void SpawnArrow(RectTransform parent, Vector2 from, Vector2 to, float width)
    {
        Vector2 dir = to - from;
        if (dir.magnitude < 0.5f) return;

        var rt  = Instantiate(_arrowPrefab, parent, false);
        var img = rt.GetComponent<Image>();
        if (img != null)
            img.enabled = true;

        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (from + to) * 0.5f;
        rt.sizeDelta        = new Vector2(width, dir.magnitude);
        rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
    }

    private static Vector2 CellCenter(int col, int row, float step, float half)
        => new Vector2(col * step + half, -(row * step + half));

    // ── Talent logic ─────────────────────────────────────────────────────────

    private void RefreshAllNodes()
    {
        foreach (var nodeView in _spawnedNodes)
        {
            var definition = nodeView.AssignedData;
            if (definition == null) continue;

            int  currentRank = _talentManager.GetTalentRank(definition.Id);
            bool canInvest   = _talentManager.CanInvestInTalent(definition);
            nodeView.UpdateState(currentRank, definition.MaxRank, canInvest);
        }
    }

    private void HandleTalentClicked(string talentId)
    {
        foreach (var tree in _classData.Trees)
        {
            var node = tree.Nodes.FirstOrDefault(n => n.Definition.Id == talentId);
            if (node == null) continue;

            if (_talentManager.TryInvestPoint(node.Definition))
                RefreshAllNodes();
            return;
        }
    }
}
