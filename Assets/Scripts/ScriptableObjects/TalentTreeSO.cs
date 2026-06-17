using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTalentTree", menuName = "Scriptable Objects/TalentTree")]
public class TalentTreeSO : ScriptableObject
{
    [Header("Main data")]
    public string TreeName;
    public Sprite SpecIcon;
    public GameObject BackgroundPrefab;
    public TabDefenitionSO TabDefinition;

    public List<TalentNodeData> Nodes;
}