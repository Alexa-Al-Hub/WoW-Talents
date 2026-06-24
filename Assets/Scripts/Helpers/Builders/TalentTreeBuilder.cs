using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TalentTree
{
    public class TalentTreeBuilder
    {
        private readonly TalentNodeView _nodePrefab;
        private readonly TreePanelView _panelPrefab;
        private readonly RectTransform _container;
        private readonly float _cellSize;
        private readonly float _cellSpacing;
        private readonly float _nodeTopPadding;

        public TalentTreeBuilder(
            TalentNodeView nodePrefab,
            TreePanelView panelPrefab,
            RectTransform container,
            float cellSize,
            float cellSpacing,
            float nodeTopPadding)
        {
            _nodePrefab = nodePrefab;
            _panelPrefab = panelPrefab;
            _container = container;
            _cellSize = cellSize;
            _cellSpacing = cellSpacing;
            _nodeTopPadding = nodeTopPadding;
        }

        public static string DisplayName(TalentTreeSO tree)
        {
            if (tree.TabDefinition != null && !string.IsNullOrEmpty(tree.TabDefinition.DisplayName))
            {
                return tree.TabDefinition.DisplayName;
            }
            return tree.name;
        }

        // Spawns every tree's panel and nodes, returning the spawned node views grouped by tree.
        public Dictionary<TalentTreeSO, List<TalentNodeView>> Build(CharacterSO classData)
        {
            var nodesByTree = new Dictionary<TalentTreeSO, List<TalentNodeView>>();

            var layouts = CollectLayouts(classData);
            if (layouts.Count == 0)
            {
                return nodesByTree;
            }

            if (!_container.TryGetComponent<GridLayoutGroup>(out var treesGridLayout))
            {
                return nodesByTree;
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

                var nodeViews = new List<TalentNodeView>();
                foreach (var nodeData in nodes)
                {
                    var nodeView = SpawnNode(nodeData, panel, step, horizontalOffset);
                    if (nodeView != null)
                    {
                        nodeViews.Add(nodeView);
                    }
                }
                nodesByTree[tree] = nodeViews;
            }

            // Newly instantiated panels don't always get picked up by Unity's automatic
            // layout pass in the same frame, so force it to apply the grid immediately.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_container);
            return nodesByTree;
        }

        private List<(TalentTreeSO tree, List<TalentNodeData> nodes)> CollectLayouts(CharacterSO classData)
        {
            var layouts = new List<(TalentTreeSO, List<TalentNodeData>)>();
            foreach (var tree in classData.Trees)
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
            if (_panelPrefab != null)
            {
                var panelView = Object.Instantiate(_panelPrefab, _container);
                panelView.Initialize(tree.BackgroundPrefab);
                panelRectTransform = panelView.GetComponent<RectTransform>();
            }
            else
            {
                panelRectTransform = new GameObject(DisplayName(tree)).AddComponent<RectTransform>();
                panelRectTransform.SetParent(_container, false);
            }

            // Anchoring, sizing and position are driven by the GridLayoutGroup on _container.
            panelRectTransform.name = DisplayName(tree);
            return panelRectTransform;
        }

        private TalentNodeView SpawnNode(TalentNodeData nodeData, RectTransform parentPanel, float step, float horizontalOffset)
        {
            if (nodeData.Definition == null)
            {
                return null;
            }

            var nodeView = Object.Instantiate(_nodePrefab, parentPanel);
            nodeView.Initialize(nodeData.Definition);

            var nodeRectTransform = nodeView.GetComponent<RectTransform>();
            nodeRectTransform.anchorMin = new Vector2(0f, 1f);
            nodeRectTransform.anchorMax = new Vector2(0f, 1f);
            nodeRectTransform.pivot = new Vector2(0f, 1f);
            nodeRectTransform.sizeDelta = new Vector2(_cellSize, _cellSize);
            nodeRectTransform.anchoredPosition = new Vector2(
                horizontalOffset + nodeData.X * step,
                -(nodeData.Y * step) - _nodeTopPadding);

            return nodeView;
        }
    }
}
