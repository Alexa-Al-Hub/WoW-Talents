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
    [SerializeField] private float _containerPadding = 20f;

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
        var rootCanvasRT = _container.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvasRT);
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
        float step = _cellSize + _cellSpacing;

        var layouts = CollectLayouts(step);
        if (layouts.Count == 0) return;

        float containerWidth  = _container.rect.width;
        float containerHeight = _container.rect.height;

        float equalPanelWidth  = containerWidth / layouts.Count;
        float maxContentWidth  = layouts.Max(layout => layout.panelWidth);
        float maxContentHeight = layouts.Max(layout => layout.panelHeight);

        float scale = maxContentWidth > 0f ? equalPanelWidth / maxContentWidth : 1f;
        if (maxContentHeight > 0f) scale = Mathf.Min(scale, (containerHeight - _containerPadding * 2f) / maxContentHeight);

        float scaledStep      = step      * scale;
        float scaledCellSize  = _cellSize * scale;
        float cursorX         = -containerWidth * 0.5f;

        foreach (var (tree, nodes, panelWidth, panelHeight) in layouts)
        {
            float scaledContentW = panelWidth  * scale;
            float scaledContentH = panelHeight * scale;
            var panel = SpawnPanel(tree, cursorX, containerHeight, scaledContentW, scaledContentH);
            cursorX += equalPanelWidth;

            foreach (var nodeData in nodes)
                SpawnNode(nodeData, panel, scaledCellSize, scaledStep);
        }
    }

    private void BuildTabs()
    {
        if (_tabPrefab == null || _tabsContainer == null) return;

        foreach (var tree in _classData.Trees)
        {
            if (tree == null) continue;
            var tab = Instantiate(_tabPrefab, _tabsContainer);
            tab.Initialize(tree);
            _spawnedTabs.Add(tab);
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

    private RectTransform SpawnPanel(TalentTreeSO tree, float posX, float maxHeight, float contentW, float contentH)
    {
        RectTransform panelRectTransform;
        if (_treePanelPrefab != null)
        {
            var panelView = Instantiate(_treePanelPrefab, _container);
            panelView.Initialize(tree.BackgroundSprite);
            panelRectTransform = panelView.GetComponent<RectTransform>();
        }
        else
        {
            panelRectTransform = new GameObject(tree.TreeName).AddComponent<RectTransform>();
            panelRectTransform.SetParent(_container, false);
        }

        panelRectTransform.name             = tree.TreeName;
        panelRectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRectTransform.pivot            = new Vector2(0f, 1f);
        panelRectTransform.sizeDelta        = new Vector2(contentW, contentH);
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
        var firstValidNode = tree.Nodes?.FirstOrDefault(n => n?.Definition != null);
        if (firstValidNode == null) return 0;
        return _talentManager.GetPointsInTree(firstValidNode.Definition.TreeId);
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
