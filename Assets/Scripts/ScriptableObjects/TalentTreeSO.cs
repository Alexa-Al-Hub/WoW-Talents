using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
    [CreateAssetMenu(fileName = "NewTalentTree", menuName = "Scriptable Objects/TalentTree")]
    public class TalentTreeSO : ScriptableObject
    {
        [Header("Main data")]
        public GameObject BackgroundPrefab;
        public TabDefinitionSO TabDefinition;

        public List<TalentNodeData> Nodes;
    }
}
