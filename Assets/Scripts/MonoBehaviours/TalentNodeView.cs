using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TalentTree
{
    public class TalentNodeView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Links")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private Button _button;
        [SerializeField] private Image _grayScaleOverlay;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _hoverOverlay;

        [Header("Frame Colors")]
        [SerializeField] private Color _availableColor = Color.green;
        [SerializeField] private Color _maxedColor = new(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _lockedColor = new(0.35f, 0.35f, 0.35f, 1f);

        private string _talentId;
        private TalentDefinitionSO _assignedData;

        public string TalentId => _talentId;
        public TalentDefinitionSO AssignedData => _assignedData;

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

            if (_iconImage != null)
            {
                _iconImage.sprite = talentDef.Icon;
            }
            if (_hoverOverlay != null)
            {
                _hoverOverlay.enabled = false;
            }
        }

        public void UpdateState(int currentRank, int maxRank, bool canInvest, bool requirementsMet)
        {
            bool isMaxed = currentRank >= maxRank;
            bool isLocked = !requirementsMet && !isMaxed;

            if (_rankText != null)
            {
                _rankText.text = $"{currentRank}/{maxRank}";
                _rankText.color = isLocked ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
            }

            if (_button != null)
            {
                _button.interactable = canInvest;
            }
            if (_grayScaleOverlay != null)
            {
                _grayScaleOverlay.enabled = isLocked;
            }

            if (_frame != null)
            {
                _frame.color = isMaxed ? _maxedColor : (isLocked ? _lockedColor : _availableColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || _assignedData == null)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnInvestRequested?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRemoveRequested?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hoverOverlay != null)
            {
                _hoverOverlay.enabled = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoverOverlay != null)
            {
                _hoverOverlay.enabled = false;
            }
        }
    }
}
