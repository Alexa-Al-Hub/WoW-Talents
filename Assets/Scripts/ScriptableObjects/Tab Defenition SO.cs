using UnityEngine;

[CreateAssetMenu(fileName = "TabDefenitionSO", menuName = "Scriptable Objects/TabDefenition")]
public class TabDefenitionSO : ScriptableObject
{
    [Header("Tab data")]
    public string DisplayName;
    public Sprite Icon;
    public int TotalTreePoints;
}
