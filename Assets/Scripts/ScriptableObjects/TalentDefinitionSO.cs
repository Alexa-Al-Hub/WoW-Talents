using UnityEngine;

[CreateAssetMenu(fileName = "TalentDefinitionSO", menuName = "Scriptable Objects/TalentDefinition")]
public class TalentDefinitionSO : ScriptableObject
{
    [Header("Main data")]
    public string Id;
    public string DisplayName;
    public Sprite Icon;

    [Header("Update rules")]
    [Min(1)] public int MaxRank = 1;
    public int RequiredPointsInTree = 0;

    [Header("Talent dependency")]
    public TalentDefinitionSO RequiredTalent;
}
