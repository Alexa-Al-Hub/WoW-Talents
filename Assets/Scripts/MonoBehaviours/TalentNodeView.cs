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

    [Header("Settings")]
    [Tooltip("Unique talant Id")]
    [SerializeField] private string _talentId;

    public string TalentId => _talentId;

    public event Action<string> OnTalentClicked;

    private void Awake()
    {
        _button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        OnTalentClicked?.Invoke(_talentId);
    }

    public void Initialize(TalentDefinitionSO talentDef, int currentRank)
    {
        _talentId = talentDef.Id;
        _iconImage.sprite = talentDef.Icon;
        UpdateState(currentRank, talentDef.MaxRank, isAvailable: true);
    }

    private void UpdateState(int currentRank, int maxRank, bool isAvailable)
    {
        _rankText.text = $"{currentRank}/{maxRank}";
        _button.interactable = isAvailable;

        if (_grayScaleOverlay != null)
        {
            _grayScaleOverlay.gameObject.SetActive(!isAvailable);
        }
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveAllListeners();
    }
}