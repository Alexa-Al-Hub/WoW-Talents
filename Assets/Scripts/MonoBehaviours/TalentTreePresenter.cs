using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TalentTree
{
    public class TalentTreePresenter : MonoBehaviour
    {
        [Header("Character Data")]
        [SerializeField] private CharacterSO _classData;

        [Header("Talent Points")]
        [SerializeField] private int _totalTalentPoints = 51;

        [Header("Talent Tree UI")]
        [SerializeField] private TalentNodeView _nodePrefab;
        [SerializeField] private TreePanelView _treePanelPrefab;
        [SerializeField] private RectTransform _container;

        [Header("Header Panel")]
        [SerializeField] private TextMeshProUGUI _classHeaderText;
        [SerializeField] private TextMeshProUGUI _pointsLeftText;

        [Header("Tree Tab UI")]
        [SerializeField] private TreeTabView _tabPrefab;
        [SerializeField] private RectTransform _tabsContainer;

        [Header("Layout")]
        [SerializeField] private float _cellSize = 64f;
        [SerializeField] private float _cellSpacing = 10f;
        [SerializeField] private float _nodeTopPadding = 10f;

        private PlayerTalentManager _talentManager;
        private readonly Dictionary<TalentTreeSO, List<TalentNodeView>> _nodesByTree = new();
        private readonly Dictionary<TalentTreeSO, TreeTabView> _tabsByTree = new();
        private readonly List<GameObject> _spawnedPanels = new();

        private void OnEnable() => StartCoroutine(BuildAfterLayout());
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
            {
                return;
            }

            _talentManager = new PlayerTalentManager(_totalTalentPoints);

            // Keep the trees container behind its siblings (Header, Tabs) so backgrounds render behind everything.
            _container.SetAsFirstSibling();

            BuildTrees();
            BuildTabs();
            RefreshAllNodes();
        }

        private void ClearSpawned()
        {
            _nodesByTree.Clear();
            _tabsByTree.Clear();
            _spawnedPanels.Clear();

            if (_container != null)
            {
                for (int i = _container.childCount - 1; i >= 0; i--)
                {
                    Destroy(_container.GetChild(i).gameObject);
                }
            }

            if (_tabsContainer != null)
            {
                for (int i = _tabsContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(_tabsContainer.GetChild(i).gameObject);
                }
            }
        }

        private void BuildTrees()
        {
            var layouts = CollectLayouts();
            if (layouts.Count == 0)
            {
                return;
            }

            if (!_container.TryGetComponent<GridLayoutGroup>(out var treesGridLayout))
            {
                return;
            }

            // Nodes render at a fixed, comfortable size; the canvas scaling (Constant Pixel Size)
            // decides their final on-screen pixels, not any tree-fitting math.
            float step = _cellSize + _cellSpacing;

            // The GridLayoutGroup needs an explicit cell size, so derive one column from the
            // container, accounting for the grid's own spacing and padding.
            float totalColumnSpacing = treesGridLayout.spacing.x * (layouts.Count - 1);
            float horizontalPadding = treesGridLayout.padding.left + treesGridLayout.padding.right;
            float verticalPadding = treesGridLayout.padding.top + treesGridLayout.padding.bottom;
            float columnWidth = (_container.rect.width - totalColumnSpacing - horizontalPadding) / layouts.Count;
            float columnHeight = _container.rect.height - verticalPadding;

            // Every panel is the same full-column size, so the background (stretched to fill
            // the panel) lands in the same place for every tree regardless of node count.
            treesGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            treesGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            treesGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            treesGridLayout.constraintCount = layouts.Count;
            treesGridLayout.cellSize = new Vector2(columnWidth, columnHeight);

            foreach (var (tree, nodes) in layouts)
            {
                var panel = SpawnPanel(tree);

                // Center the tree horizontally inside its column; node size stays fixed.
                float contentWidth = nodes.Max(node => node.X) * step + _cellSize;
                float horizontalOffset = Mathf.Max(0f, (columnWidth - contentWidth) * 0.5f);

                foreach (var nodeData in nodes)
                {
                    SpawnNode(tree, nodeData, panel, step, horizontalOffset);
                }
            }

            // Newly instantiated panels don't always get picked up by Unity's automatic
            // layout pass in the same frame, so force it to apply the grid immediately.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_container);
        }

        private void BuildTabs()
        {
            if (_tabPrefab == null || _tabsContainer == null)
            {
                return;
            }

            foreach (var tree in _classData.Trees)
            {
                if (tree == null)
                {
                    continue;
                }

                var tab = Instantiate(_tabPrefab, _tabsContainer);
                tab.Initialize(tree, tree.TabDefinition);
                tab.OnCancelClicked += HandleTreeCancelClicked;
                _tabsByTree[tree] = tab;
            }
        }

        private List<(TalentTreeSO tree, List<TalentNodeData> nodes)> CollectLayouts()
        {
            var layouts = new List<(TalentTreeSO, List<TalentNodeData>)>();
            foreach (var tree in _classData.Trees)
            {
                if (tree == null || tree.Nodes == null)
                {
                    continue;
                }

                var nodes = tree.Nodes.Where(node => node != null).ToList();
                if (nodes.Count == 0)
                {
                    continue;
                }

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

        private void SpawnNode(TalentTreeSO tree, TalentNodeData nodeData, RectTransform parentPanel, float step, float horizontalOffset)
        {
            if (nodeData.Definition == null)
            {
                return;
            }

            var nodeView = Instantiate(_nodePrefab, parentPanel);
            nodeView.Initialize(nodeData.Definition);

            nodeView.OnInvestRequested += clickedView => HandleInvestRequested(tree, clickedView.AssignedData);
            nodeView.OnRemoveRequested += clickedView => HandleRemoveRequested(tree, clickedView.AssignedData);

            var nodeRectTransform = nodeView.GetComponent<RectTransform>();
            nodeRectTransform.anchorMin = new Vector2(0f, 1f);
            nodeRectTransform.anchorMax = new Vector2(0f, 1f);
            nodeRectTransform.pivot = new Vector2(0f, 1f);
            nodeRectTransform.sizeDelta = new Vector2(_cellSize, _cellSize);
            nodeRectTransform.anchoredPosition = new Vector2(
                horizontalOffset + nodeData.X * step,
                -(nodeData.Y * step) - _nodeTopPadding);

            if (!_nodesByTree.TryGetValue(tree, out var nodeViews))
            {
                nodeViews = new List<TalentNodeView>();
                _nodesByTree[tree] = nodeViews;
            }
            nodeViews.Add(nodeView);
        }

        // ── Talent logic ──────────────────────────────────────────────────────────

        private void RefreshAllNodes()
        {
            foreach (var treeNodes in _nodesByTree)
            {
                RefreshTreeNodes(treeNodes.Key, treeNodes.Value);
            }

            foreach (var treeTab in _tabsByTree)
            {
                treeTab.Value.UpdatePoints(_talentManager.GetPointsInTree(treeTab.Key));
            }

            RefreshHeader();
        }

        private void RefreshTree(TalentTreeSO tree)
        {
            if (tree == null)
            {
                return;
            }

            if (_nodesByTree.TryGetValue(tree, out var nodeViews))
            {
                RefreshTreeNodes(tree, nodeViews);
            }

            if (_tabsByTree.TryGetValue(tree, out var tab))
            {
                tab.UpdatePoints(_talentManager.GetPointsInTree(tree));
            }

            RefreshHeader();
        }

        private void RefreshTreeNodes(TalentTreeSO tree, List<TalentNodeView> nodeViews)
        {
            foreach (var nodeView in nodeViews)
            {
                var definition = nodeView.AssignedData;
                if (definition == null)
                {
                    continue;
                }

                int rank = _talentManager.GetTalentRank(definition.Id);
                bool canInvest = _talentManager.CanInvestInTalent(tree, definition);
                bool requirementsMet = _talentManager.AreRequirementsMet(tree, definition);
                nodeView.UpdateState(rank, definition.MaxRank, canInvest, requirementsMet);
            }
        }

        private void RefreshAfterChange(TalentTreeSO tree, int pointsBeforeChange)
        {
            bool availabilityGateFlipped = (pointsBeforeChange == 0) != (_talentManager.AvailablePoints == 0);
            if (availabilityGateFlipped)
            {
                RefreshAllNodes();
            }
            else
            {
                RefreshTree(tree);
            }
        }

        private void RefreshHeader()
        {
            if (_classHeaderText != null)
            {
                var pointsPerTree = _classData.Trees
                    .Where(tree => tree != null)
                    .Select(tree => _talentManager.GetPointsInTree(tree).ToString());
                _classHeaderText.text = $"{_classData.CharacterName} ({string.Join("/", pointsPerTree)})";
            }

            if (_pointsLeftText != null)
            {
                _pointsLeftText.text = $"Points left: {_talentManager.AvailablePoints}";
            }
        }

        private void HandleInvestRequested(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (talentDef == null)
            {
                return;
            }

            int pointsBeforeChange = _talentManager.AvailablePoints;
            if (_talentManager.TryInvestPoint(tree, talentDef))
            {
                RefreshAfterChange(tree, pointsBeforeChange);
            }
        }

        private void HandleRemoveRequested(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (talentDef == null)
            {
                return;
            }

            int pointsBeforeChange = _talentManager.AvailablePoints;
            if (_talentManager.TryRemovePoint(tree, talentDef))
            {
                RefreshAfterChange(tree, pointsBeforeChange);
            }
        }

        private void HandleTreeCancelClicked(TalentTreeSO tree)
        {
            if (tree == null)
            {
                return;
            }

            int pointsBeforeChange = _talentManager.AvailablePoints;
            if (_talentManager.ResetTree(tree) > 0)
            {
                RefreshAfterChange(tree, pointsBeforeChange);
            }
        }
    }
}
