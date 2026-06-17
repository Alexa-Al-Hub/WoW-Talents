using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeTabView : MonoBehaviour
{
    [SerializeField] private Image _specIcon;
    [SerializeField] private TextMeshProUGUI _treeNameText;
    [SerializeField] private TextMeshProUGUI _pointsText;

    public TalentTreeSO Tree { get; private set; }
    public TabDefenitionSO TabData { get; private set; }

    public void Initialize(TalentTreeSO tree, TabDefenitionSO tabDefenition)
    {
        Tree = tree;
        TabData = tabDefenition;
        UpdatePoints(0);
        if (tabDefenition == null) return;
        if (_specIcon != null) _specIcon.sprite = tabDefenition.Icon;
        if (_treeNameText != null) _treeNameText.text = tabDefenition.DisplayName;
    }

    public void UpdatePoints(int points)
    {
        if (_pointsText != null) _pointsText.text = $"({points})";
    }
}
