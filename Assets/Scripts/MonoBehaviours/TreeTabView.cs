using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeTabView : MonoBehaviour
{
    [SerializeField] private Image            _specIcon;
    [SerializeField] private TextMeshProUGUI  _treeNameText;
    [SerializeField] private TextMeshProUGUI  _pointsText;

    public TalentTreeSO Tree { get; private set; }

    public void Initialize(TalentTreeSO tree)
    {
        Tree = tree;
        if (_specIcon    != null) _specIcon.sprite  = tree.SpecIcon;
        if (_treeNameText != null) _treeNameText.text = tree.TreeName;
        UpdatePoints(0);
    }

    public void UpdatePoints(int points)
    {
        if (_pointsText != null) _pointsText.text = $"({points})";
    }
}
