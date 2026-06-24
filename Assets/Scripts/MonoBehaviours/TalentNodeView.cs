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
        [SerializeField] private Button _button;
        [SerializeField] private Image _grayScaleOverlay;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _hoverOverlay;

        [Header("Badge UI")]
        [SerializeField] private GameObject _badgeContainer;
        [SerializeField] private TextMeshProUGUI _rankText;

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
        public event Action<TalentNodeView> OnHoverEnter;
        public event Action<TalentNodeView> OnHoverExit;

        private void Awake()
        {
            if (_iconImage == null || _button == null || _grayScaleOverlay == null || _frame == null
                || _hoverOverlay == null || _badgeContainer == null || _rankText == null)
            {
                Debug.LogError($"{nameof(TalentNodeView)} is missing a serialized UI reference.", this);
            }
        }

        public void Initialize(TalentDefinitionSO talentDef)
        {
            if (talentDef == null)
            {
                Debug.LogError($"{nameof(TalentNodeView)} was initialized with a null talent definition.", this);
                return;
            }

            _assignedData = talentDef;
            _talentId = talentDef.Id;

            _iconImage.sprite = talentDef.Icon;
            _hoverOverlay.enabled = false;
        }

        public void UpdateState(TalentDisplayState state)
        {
            bool isMaxed = state.Rank >= state.MaxRank;
            bool isLocked = !state.RequirementsMet && !isMaxed;
            Color frameColor = isMaxed ? _maxedColor : (isLocked ? _lockedColor : _availableColor);

            _button.interactable = state.CanInvest;
            _grayScaleOverlay.enabled = isLocked;
            _frame.color = frameColor;

            UpdateRankBadge(state, isLocked, frameColor);
        }

        private void UpdateRankBadge(TalentDisplayState state, bool isLocked, Color frameColor)
        {
            _badgeContainer.SetActive(!isLocked);
            if (!isLocked)
            {
                _rankText.text = state.Rank.ToString();
                _rankText.color = frameColor;
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
            _hoverOverlay.enabled = true;
            OnHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverOverlay.enabled = false;
            OnHoverExit?.Invoke(this);
        }
    }
}
