using UnityEngine.Serialization;

namespace TalentTree
{
    [System.Serializable]
    public class TalentNodeData
    {
        public TalentDefinitionSO Definition;
        [FormerlySerializedAs("Column")] public float X;
        [FormerlySerializedAs("Row")] public float Y;
    }
}
