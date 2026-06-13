using System.ComponentModel;
using UnityEngine;

[CreateAssetMenu(fileName = "TalentDefinitionSO", menuName = "Scriptable Objects/TalentDefinition")]
public class TalentDefinitionSO : ScriptableObject
{
    [Header("Main data")]
    public string Id;
    public string DisplayName;
    public Sprite Icon;
    public string Description;

    [Header("Update rules")]
    [Min(1)] public int MaxRank = 1;

    [Header("Requirements")]
    [Tooltip("Tree Id")]
    public string TreeId;
    [Tooltip("Required points in the tree")]
    public int RequiredTreePoints;

    [Header("Talent dependency")]
    public TalentDefinitionSO RequiredTalent;
}
