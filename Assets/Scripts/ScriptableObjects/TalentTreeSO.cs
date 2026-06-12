using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTalentTree", menuName = "Scriptable Objects/TalentTree")]
public class TalentTreeSO : ScriptableObject
{
    public string TreeName;
    public List<TalentNodeData> Nodes;

}