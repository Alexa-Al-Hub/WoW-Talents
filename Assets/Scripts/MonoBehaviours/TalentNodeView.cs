using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TalentNodeView : MonoBehaviour
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

    public event Action<string> OnTalentClicked;

    private void Awake()
    {
        _button.onClick.AddListener(HandleClick);
    }

    public void Initialize(TalentDefinitionSO talentDef)
    {
        _assignedData = talentDef;
        _talentId = talentDef.Id;
        _iconImage.sprite = talentDef.Icon;
    }

    public void UpdateState(int currentRank, int maxRank, bool canInvest)
    {
        bool isMaxed  = currentRank >= maxRank;
        bool isLocked = !canInvest && !isMaxed;

        _rankText.text = $"{currentRank}/{maxRank}";
        _button.interactable = canInvest;

        if (_grayScaleOverlay != null) _grayScaleOverlay.enabled = isLocked;
        _rankText.color = isLocked ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }

    private void HandleClick()
    {
        OnTalentClicked?.Invoke(_talentId);
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveAllListeners();
    }
}
