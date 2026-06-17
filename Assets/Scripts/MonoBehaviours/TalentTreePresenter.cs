using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentTreePresenter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CharacterSO _classData;

    [Header("UI")]
    [SerializeField] private TalentNodeView  _nodePrefab;
    [SerializeField] private TreePanelView   _treePanelPrefab;
    [SerializeField] private RectTransform   _container;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI _classHeaderText;
    [SerializeField] private TextMeshProUGUI _pointsLeftText;

    [Header("Tabs")]
    [SerializeField] private TreeTabView _tabPrefab;
    [SerializeField] private RectTransform _tabsContainer;

    [Header("Layout")]
    [SerializeField] private float _cellSize = 64f;
    [SerializeField] private float _cellSpacing = 10f;
    [SerializeField] private float _nodeTopPadding = 10f;

    private PlayerTalentManager _talentManager;
    private readonly List<TalentNodeView> _spawnedNodes = new();
    private readonly List<GameObject> _spawnedPanels = new();
    private readonly List<TreeTabView> _spawnedTabs = new();

    private void OnEnable()  => StartCoroutine(BuildAfterLayout());
    private void OnDisable() => ClearSpawned();

    private IEnumerator BuildAfterLayout()
    {
        yield return null;
        yield return null;
        var rootCanvasRectTransform = _container.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvasRectTransform);
        Canvas.ForceUpdateCanvases();
        Build();
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build()
    {
        ClearSpawned();

        if (_classData == null || _nodePrefab == null || _container == null)
            return;

        _talentManager = new PlayerTalentManager(51);

        // Keep the trees container behind its siblings (Header, Tabs) so backgrounds render behind everything.
        _container.SetAsFirstSibling();

        BuildTrees();
        BuildTabs();
        RefreshAllNodes();
    }

    private void ClearSpawned()
    {
        _spawnedNodes.Clear();
        _spawnedPanels.Clear();
        _spawnedTabs.Clear();

        if (_container != null)
            for (int i = _container.childCount - 1; i >= 0; i--)
                Destroy(_container.GetChild(i).gameObject);

        if (_tabsContainer != null)
            for (int i = _tabsContainer.childCount - 1; i >= 0; i--)
                Destroy(_tabsContainer.GetChild(i).gameObject);
    }

    private void BuildTrees()
    {
        var layouts = CollectLayouts();
        if (layouts.Count == 0) return;

        var treesGridLayout = _container.GetComponent<GridLayoutGroup>();
        if (treesGridLayout == null) return;

        // Nodes render at a fixed, comfortable size; the canvas scaling (Constant Pixel Size)
        // decides their final on-screen pixels, not any tree-fitting math.
        float step = _cellSize + _cellSpacing;

        // The GridLayoutGroup needs an explicit cell size, so derive one column from the
        // container, accounting for the grid's own spacing and padding.
        float totalColumnSpacing = treesGridLayout.spacing.x * (layouts.Count - 1);
        float horizontalPadding  = treesGridLayout.padding.left + treesGridLayout.padding.right;
        float verticalPadding    = treesGridLayout.padding.top + treesGridLayout.padding.bottom;
        float columnWidth        = (_container.rect.width  - totalColumnSpacing - horizontalPadding) / layouts.Count;
        float columnHeight       =  _container.rect.height - verticalPadding;

        // Every panel is the same full-column size, so the background (stretched to fill
        // the panel) lands in the same place for every tree regardless of node count.
        treesGridLayout.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        treesGridLayout.startAxis       = GridLayoutGroup.Axis.Horizontal;
        treesGridLayout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        treesGridLayout.constraintCount = layouts.Count;
        treesGridLayout.cellSize        = new Vector2(columnWidth, columnHeight);

        foreach (var (tree, nodes) in layouts)
        {
            var panel = SpawnPanel(tree);

            // Center the tree horizontally inside its column; node size stays fixed.
            float contentWidth     = nodes.Max(node => node.X) * step + _cellSize;
            float horizontalOffset = Mathf.Max(0f, (columnWidth - contentWidth) * 0.5f);

            foreach (var nodeData in nodes)
                SpawnNode(nodeData, panel, step, horizontalOffset);
        }

        // Newly instantiated panels don't always get picked up by Unity's automatic
        // layout pass in the same frame, so force it to apply the grid immediately.
        LayoutRebuilder.ForceRebuildLayoutImmediate(_container);
    }

    private void BuildTabs()
    {
        if (_tabPrefab == null || _tabsContainer == null) return;

        foreach (var tree in _classData.Trees)
        {
            if (tree == null) continue;
            var tab = Instantiate(_tabPrefab, _tabsContainer);
            tab.Initialize(tree, tree.TabDefinition);
            _spawnedTabs.Add(tab);
        }
    }

    private List<(TalentTreeSO tree, List<TalentNodeData> nodes)> CollectLayouts()
    {
        var layouts = new List<(TalentTreeSO, List<TalentNodeData>)>();
        foreach (var tree in _classData.Trees)
        {
            if (tree == null || tree.Nodes == null) continue;
            var nodes = tree.Nodes.Where(node => node != null).ToList();
            if (nodes.Count == 0) continue;
            layouts.Add((tree, nodes));
        }
        return layouts;
    }

    private RectTransform SpawnPanel(TalentTreeSO tree)
    {
        RectTransform panelRectTransform;
        if (_treePanelPrefab != null)
        {
            var panelView = Instantiate(_treePanelPrefab, _container);
            panelView.Initialize(tree.BackgroundPrefab);
            panelRectTransform = panelView.GetComponent<RectTransform>();
        }
        else
        {
            panelRectTransform = new GameObject(tree.TreeName).AddComponent<RectTransform>();
            panelRectTransform.SetParent(_container, false);
        }

        // Anchoring, sizing and position are driven by the GridLayoutGroup on _container.
        panelRectTransform.name = tree.TreeName;
        _spawnedPanels.Add(panelRectTransform.gameObject);
        return panelRectTransform;
    }

    private void SpawnNode(TalentNodeData nodeData, RectTransform parentPanel, float step, float horizontalOffset)
    {
        if (nodeData.Definition == null) return;

        var nodeView = Instantiate(_nodePrefab, parentPanel);
        nodeView.Initialize(nodeData.Definition);
        nodeView.OnTalentClicked += HandleTalentClicked;

        var nodeRectTransform = nodeView.GetComponent<RectTransform>();
        nodeRectTransform.anchorMin        = new Vector2(0f, 1f);
        nodeRectTransform.anchorMax        = new Vector2(0f, 1f);
        nodeRectTransform.pivot            = new Vector2(0f, 1f);
        nodeRectTransform.sizeDelta        = new Vector2(_cellSize, _cellSize);
        nodeRectTransform.anchoredPosition = new Vector2(
            horizontalOffset + nodeData.X * step,
            -(nodeData.Y * step) - _nodeTopPadding);

        _spawnedNodes.Add(nodeView);
    }

    // ── Talent logic ──────────────────────────────────────────────────────────

    private void RefreshAllNodes()
    {
        foreach (var nodeView in _spawnedNodes)
        {
            var definition = nodeView.AssignedData;
            if (definition == null) continue;

            int  rank      = _talentManager.GetTalentRank(definition.Id);
            bool canInvest = _talentManager.CanInvestInTalent(definition);
            nodeView.UpdateState(rank, definition.MaxRank, canInvest);
        }

        foreach (var tab in _spawnedTabs)
        {
            if (tab?.Tree == null) continue;
            tab.UpdatePoints(GetPointsInTree(tab.Tree));
        }

        RefreshHeader();
    }

    private void RefreshHeader()
    {
        if (_classHeaderText != null)
        {
            var pointsPerTree = _classData.Trees
                .Where(tree => tree != null)
                .Select(tree => GetPointsInTree(tree).ToString());
            _classHeaderText.text = $"{_classData.CharacterName} ({string.Join("/", pointsPerTree)})";
        }

        if (_pointsLeftText != null)
            _pointsLeftText.text = $"Points left: {_talentManager.AvailablePoints}";
    }

    private int GetPointsInTree(TalentTreeSO tree)
    {
        var firstValidNode = tree.Nodes?.FirstOrDefault(node => node?.Definition != null);
        if (firstValidNode == null) return 0;
        return _talentManager.GetPointsInTree(firstValidNode.Definition.TreeId);
    }

    private void HandleTalentClicked(string talentId)
    {
        foreach (var tree in _classData.Trees)
        {
            if (tree == null) continue;
            var node = tree.Nodes.FirstOrDefault(candidate => candidate?.Definition != null && candidate.Definition.Id == talentId);
            if (node == null) continue;

            if (_talentManager.TryInvestPoint(node.Definition))
                RefreshAllNodes();
            return;
        }
    }
}
