using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TalentTree
{
    public class TalentNodeView : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Links")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private Button _button;
        [SerializeField] private Image _grayScaleOverlay;

        private string _talentId;
        private TalentDefinitionSO _assignedData;

        public string TalentId => _talentId;
        public TalentDefinitionSO AssignedData => _assignedData;

        // Left click asks to invest a point, right click asks to refund one.
        public event Action<TalentNodeView> OnInvestRequested;
        public event Action<TalentNodeView> OnRemoveRequested;

        public void Initialize(TalentDefinitionSO talentDef)
        {
            if (talentDef == null)
            {
                Debug.LogError($"{nameof(TalentNodeView)} was initialized with a null talent definition.", this);
                return;
            }

            _assignedData = talentDef;
            _talentId = talentDef.Id;

            if (_iconImage != null) _iconImage.sprite = talentDef.Icon;
        }

        public void UpdateState(int currentRank, int maxRank, bool canInvest)
        {
            bool isMaxed = currentRank >= maxRank;
            bool isLocked = !canInvest && !isMaxed;

            if (_rankText != null)
            {
                _rankText.text = $"{currentRank}/{maxRank}";
                _rankText.color = isLocked ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
            }

            if (_button != null) _button.interactable = canInvest;
            if (_grayScaleOverlay != null) _grayScaleOverlay.enabled = isLocked;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || _assignedData == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
                OnInvestRequested?.Invoke(this);
            else if (eventData.button == PointerEventData.InputButton.Right)
                OnRemoveRequested?.Invoke(this);
        }
    }
}
