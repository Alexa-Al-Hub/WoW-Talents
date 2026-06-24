using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
    public class TreeTabBarBuilder
    {
        private readonly TreeTabView _tabPrefab;
        private readonly RectTransform _tabsContainer;

        public TreeTabBarBuilder(TreeTabView tabPrefab, RectTransform tabsContainer)
        {
            _tabPrefab = tabPrefab;
            _tabsContainer = tabsContainer;
        }

        public Dictionary<TalentTreeSO, TreeTabView> Build(IEnumerable<TalentTreeSO> trees)
        {
            var tabsByTree = new Dictionary<TalentTreeSO, TreeTabView>();
            if (_tabPrefab == null || _tabsContainer == null)
            {
                return tabsByTree;
            }

            foreach (var tree in trees)
            {
                if (tree == null)
                {
                    continue;
                }

                var tab = Object.Instantiate(_tabPrefab, _tabsContainer);
                tab.Initialize(tree);
                tabsByTree[tree] = tab;
            }

            return tabsByTree;
        }
    }
}
