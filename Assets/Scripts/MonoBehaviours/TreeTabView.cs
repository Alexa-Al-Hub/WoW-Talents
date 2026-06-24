using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TalentTree
{
    public class TreeTabView : MonoBehaviour
    {
        [SerializeField] private Image _specIcon;
        [SerializeField] private TextMeshProUGUI _treeNameText;
        [SerializeField] private TextMeshProUGUI _pointsText;
        [SerializeField] private Button _cancelButton;

        public event Action<TalentTreeSO> OnCancelClicked;

        public TalentTreeSO Tree { get; private set; }
        public TabDefinitionSO TabData { get; private set; }

        public void Initialize(TalentTreeSO tree)
        {
            Tree = tree;
            TabData = tree != null ? tree.TabDefinition : null;
            UpdatePoints(0);

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(RaiseCancelClicked);
                _cancelButton.onClick.AddListener(RaiseCancelClicked);
            }

            if (TabData == null)
            {
                return;
            }
            if (_specIcon != null)
            {
                _specIcon.sprite = TabData.Icon;
            }
            if (_treeNameText != null)
            {
                _treeNameText.text = TabData.DisplayName;
            }
        }

        public void UpdatePoints(int points)
        {
            if (_pointsText != null)
            {
                _pointsText.text = $"({points})";
            }
        }

        private void RaiseCancelClicked() => OnCancelClicked?.Invoke(Tree);

        private void OnDestroy()
        {
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(RaiseCancelClicked);
            }
        }
    }
}
