using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTalentTree", menuName = "Scriptable Objects/TalentTree")]
public class TalentTreeSO : ScriptableObject
{
    public string TreeName;
    public List<TalentNodeData> Nodes;
}

[System.Serializable]
public class TalentNodeData
{
    public TalentDefinitionSO Definition;
    public int Row;    // 0 = top row
    public int Column; // 0 = left column
}
