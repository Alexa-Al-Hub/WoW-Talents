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

        [Header("Tooltip")]
        [SerializeField] private TalentTooltipView _tooltipPrefab;

        [Header("Layout")]
        [SerializeField] private float _cellSize = 64f;
        [SerializeField] private float _cellSpacing = 10f;
        [SerializeField] private float _nodeTopPadding = 10f;

        private const string PointsLeftFormat = "Points left: {0}";

        private PlayerTalentManager _talentManager;
        private TalentTreeBuilder _treeBuilder;
        private TreeTabBarBuilder _tabBarBuilder;
        private TalentTooltipBuilder _tooltipBuilder;
        private TalentTreeSO _hoveredTree;
        private TalentNodeView _hoveredNode;
        private readonly Dictionary<TalentTreeSO, List<TalentNodeView>> _nodesByTree = new();
        private readonly Dictionary<TalentTreeSO, TreeTabView> _tabsByTree = new();
        private readonly Dictionary<CharacterSO, PlayerTalentManager> _managersByCharacter = new();

        private void OnEnable() => Build();
        private void OnDisable() => ClearSpawned();
        private void OnDestroy() => _tooltipBuilder?.Destroy();

        public void SetCharacter(CharacterSO character)
        {
            if (character == null || character == _classData)
            {
                return;
            }

            _classData = character;
            if (isActiveAndEnabled)
            {
                Build();
            }
        }

        // ── Build ─────────────────────────────────────────────────────────────────

        private void Build()
        {
            ClearSpawned();

            if (_classData == null || _nodePrefab == null || _container == null)
            {
                return;
            }

            var parentCanvas = _container.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError($"{nameof(TalentTreePresenter)}: container is not under a Canvas.", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (parentCanvas.rootCanvas.transform is RectTransform rootCanvasRectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootCanvasRectTransform);
            }

            _talentManager = GetOrCreateManager(_classData);
            _container.SetAsFirstSibling();

            CreateTooltip(parentCanvas.rootCanvas);
            SpawnTrees();
            BuildTabs();
            RefreshAllNodes();
        }

        private PlayerTalentManager GetOrCreateManager(CharacterSO character)
        {
            if (!_managersByCharacter.TryGetValue(character, out var manager))
            {
                manager = new PlayerTalentManager(_totalTalentPoints);
                _managersByCharacter[character] = manager;
            }
            return manager;
        }

        private void CreateTooltip(Canvas rootCanvas)
        {
            if (_tooltipBuilder != null || _tooltipPrefab == null || rootCanvas == null)
            {
                return;
            }

            _tooltipBuilder = new TalentTooltipBuilder(_tooltipPrefab, rootCanvas);
        }

        private void ClearSpawned()
        {
            _nodesByTree.Clear();
            _tabsByTree.Clear();

            // The spawned nodes are about to be destroyed, so drop any hover state and hide the
            // (reused) tooltip instead of destroying it.
            _hoveredTree = null;
            _hoveredNode = null;
            _tooltipBuilder?.Hide();

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

        private void SpawnTrees()
        {
            _treeBuilder ??= new TalentTreeBuilder(
                _nodePrefab, _treePanelPrefab, _container, _cellSize, _cellSpacing, _nodeTopPadding);

            foreach (var spawnedTree in _treeBuilder.Build(_classData))
            {
                var tree = spawnedTree.Key;
                var nodeViews = spawnedTree.Value;
                _nodesByTree[tree] = nodeViews;

                foreach (var nodeView in nodeViews)
                {
                    WireNode(tree, nodeView);
                }
            }
        }

        private void WireNode(TalentTreeSO tree, TalentNodeView nodeView)
        {
            nodeView.OnInvestRequested += clickedView => HandleInvestRequested(tree, clickedView.AssignedData);
            nodeView.OnRemoveRequested += clickedView => HandleRemoveRequested(tree, clickedView.AssignedData);
            nodeView.OnHoverEnter += hoveredView => ShowTooltip(tree, hoveredView);
            nodeView.OnHoverExit += _ => HideTooltip();
        }

        private void BuildTabs()
        {
            _tabBarBuilder ??= new TreeTabBarBuilder(_tabPrefab, _tabsContainer);

            var displayedTrees = _classData.Trees
                .Where(tree => tree != null && _nodesByTree.ContainsKey(tree));

            foreach (var spawnedTab in _tabBarBuilder.Build(displayedTrees))
            {
                var tab = spawnedTab.Value;
                _tabsByTree[spawnedTab.Key] = tab;
                tab.OnCancelClicked += HandleTreeCancelClicked;
            }
        }

        // ── Tooltip ───────────────────────────────────────────────────────────────

        private void ShowTooltip(TalentTreeSO tree, TalentNodeView nodeView)
        {
            if (_tooltipBuilder == null || nodeView == null || nodeView.AssignedData == null)
            {
                return;
            }

            _hoveredTree = tree;
            _hoveredNode = nodeView;

            var definition = nodeView.AssignedData;
            var state = _talentManager.GetDisplayState(tree, definition);
            string lockReason = BuildLockReason(tree, definition, state);

            _tooltipBuilder.Show(definition, state, lockReason, (RectTransform)nodeView.transform);
        }

        private void HideTooltip()
        {
            _hoveredTree = null;
            _hoveredNode = null;
            _tooltipBuilder?.Hide();
        }

        private void RefreshTooltip()
        {
            if (_hoveredNode != null)
            {
                ShowTooltip(_hoveredTree, _hoveredNode);
            }
        }

        private string BuildLockReason(TalentTreeSO tree, TalentDefinitionSO definition, TalentDisplayState state)
        {
            switch (state.LockReason)
            {
                case TalentLockReason.NotEnoughTreePoints:
                    {
                        return $"Requires {definition.RequiredTreePoints} points in {TalentTreeBuilder.DisplayName(tree)}";
                    }
                case TalentLockReason.PrerequisiteNotMaxed:
                    {
                        var prerequisite = state.BlockingPrerequisite;
                        return $"Requires {prerequisite.MaxRank} point(s) in {prerequisite.DisplayName}";
                    }
                default:
                    return null;
            }
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

                nodeView.UpdateState(_talentManager.GetDisplayState(tree, definition));
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

            RefreshTooltip();
        }

        private void RefreshHeader()
        {
            if (_classHeaderText != null)
            {
                var pointsPerTree = _classData.Trees
                    .Where(tree => tree != null && _nodesByTree.ContainsKey(tree))
                    .Select(tree => _talentManager.GetPointsInTree(tree).ToString());
                _classHeaderText.text = $"{_classData.CharacterName} ({string.Join("/", pointsPerTree)})";
            }

            if (_pointsLeftText != null)
            {
                _pointsLeftText.text = string.Format(PointsLeftFormat, _talentManager.AvailablePoints);
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
