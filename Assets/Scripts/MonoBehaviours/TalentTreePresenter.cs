using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TalentTreePresenter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterSO _classData;

    [Header("UI")]
    [SerializeField] private TalentNodeView _nodePrefab;
    [SerializeField] private RectTransform  _container;

    [Header("Layout")]
    [SerializeField] private float _cellSize         = 64f;
    [SerializeField] private float _cellSpacing      = 10f;
    [SerializeField] private float _containerPadding = 20f;

    private PlayerTalentManager           _talentManager;
    private readonly List<TalentNodeView> _spawnedNodes  = new();
    private readonly List<GameObject>     _spawnedPanels = new();

    private void OnEnable()  => Build();
    private void OnDisable() => ClearSpawned();

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build()
    {
        ClearSpawned();

        if (_classData == null || _nodePrefab == null || _container == null)
            return;

        _talentManager = new PlayerTalentManager(51);

        BuildTrees();
        RefreshAllNodes();
    }

    private void ClearSpawned()
    {
        _spawnedNodes.Clear();
        _spawnedPanels.Clear();

        if (_container == null) return;

        for (int i = _container.childCount - 1; i >= 0; i--)
            Destroy(_container.GetChild(i).gameObject);
    }

    private void BuildTrees()
    {
        float step    = _cellSize + _cellSpacing;
        float treeGap = _cellSpacing * 4f;

        var layouts = CollectLayouts(step);
        if (layouts.Count == 0) return;

        float totalWidth  = layouts.Sum(layout => layout.panelWidth) + treeGap * (layouts.Count - 1);
        float maxHeight   = layouts.Max(layout => layout.panelHeight);
        float scale       = ComputeScale(totalWidth, maxHeight);

        float scaledStep      = step      * scale;
        float scaledCellSize  = _cellSize * scale;
        float scaledTreeGap   = treeGap   * scale;
        float scaledMaxHeight = maxHeight * scale;
        float cursorX         = -totalWidth * scale * 0.5f;

        foreach (var (tree, nodes, panelWidth, panelHeight) in layouts)
        {
            var panel = SpawnPanel(tree, panelWidth * scale, panelHeight * scale, cursorX, scaledMaxHeight);
            cursorX += panelWidth * scale + scaledTreeGap;

            foreach (var nodeData in nodes)
                SpawnNode(nodeData, panel, scaledCellSize, scaledStep);
        }
    }

    private List<(TalentTreeSO tree, List<TalentNodeData> nodes, float panelWidth, float panelHeight)> CollectLayouts(float step)
    {
        var layouts = new List<(TalentTreeSO, List<TalentNodeData>, float, float)>();
        foreach (var tree in _classData.Trees)
        {
            if (tree == null || tree.Nodes == null) continue;
            var nodes = tree.Nodes.Where(n => n != null).ToList();
            if (nodes.Count == 0) continue;
            float panelWidth  = nodes.Max(n => n.X) * step + _cellSize;
            float panelHeight = nodes.Max(n => n.Y) * step + _cellSize;
            layouts.Add((tree, nodes, panelWidth, panelHeight));
        }
        return layouts;
    }

    private float ComputeScale(float totalWidth, float maxHeight)
    {
        float scale           = 1f;
        float containerWidth  = _container.rect.width;
        float containerHeight = _container.rect.height;
        if (containerWidth  > 0f) scale = Mathf.Min(scale, (containerWidth  - _containerPadding * 2f) / totalWidth);
        if (containerHeight > 0f) scale = Mathf.Min(scale, (containerHeight - _containerPadding * 2f) / maxHeight);
        return scale;
    }

    private RectTransform SpawnPanel(TalentTreeSO tree, float width, float height, float posX, float maxHeight)
    {
        RectTransform panelRectTransform;
        if (tree.TreePanelPrefab != null)
        {
            var prefabInstance = Instantiate(tree.TreePanelPrefab, _container);
            panelRectTransform = prefabInstance.GetComponent<RectTransform>();
            if (panelRectTransform == null) panelRectTransform = prefabInstance.AddComponent<RectTransform>();
            panelRectTransform.name = tree.TreeName;
        }
        else
        {
            panelRectTransform = new GameObject(tree.TreeName).AddComponent<RectTransform>();
            panelRectTransform.SetParent(_container, false);
        }

        panelRectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRectTransform.pivot            = new Vector2(0f, 1f);
        panelRectTransform.sizeDelta        = new Vector2(width, height);
        panelRectTransform.anchoredPosition = new Vector2(posX, maxHeight * 0.5f);
        _spawnedPanels.Add(panelRectTransform.gameObject);
        return panelRectTransform;
    }

    private void SpawnNode(TalentNodeData nodeData, RectTransform parentPanel, float cellSize, float step)
    {
        if (nodeData.Definition == null) return;

        var nodeView = Instantiate(_nodePrefab, parentPanel);
        nodeView.Initialize(nodeData.Definition);
        nodeView.OnTalentClicked += HandleTalentClicked;

        var nodeRectTransform = nodeView.GetComponent<RectTransform>();
        nodeRectTransform.anchorMin        = new Vector2(0f, 1f);
        nodeRectTransform.anchorMax        = new Vector2(0f, 1f);
        nodeRectTransform.pivot            = new Vector2(0f, 1f);
        nodeRectTransform.sizeDelta        = new Vector2(cellSize, cellSize);
        nodeRectTransform.anchoredPosition = new Vector2(nodeData.X * step, -(nodeData.Y * step));

        _spawnedNodes.Add(nodeView);
    }

    // ── Talent logic ──────────────────────────────────────────────────────────

    private void RefreshAllNodes()
    {
        foreach (var nodeView in _spawnedNodes)
        {
            var def = nodeView.AssignedData;
            if (def == null) continue;

            int  rank      = _talentManager.GetTalentRank(def.Id);
            bool canInvest = _talentManager.CanInvestInTalent(def);
            nodeView.UpdateState(rank, def.MaxRank, canInvest);
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
