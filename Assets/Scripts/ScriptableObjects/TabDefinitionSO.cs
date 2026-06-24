using UnityEngine;

namespace TalentTree
{
    [CreateAssetMenu(fileName = "TabDefinitionSO", menuName = "Scriptable Objects/TabDefinition")]
    public class TabDefinitionSO : ScriptableObject
    {
        [Header("Tab data")]
        public string DisplayName;
        public Sprite Icon;
        public int TotalTreePoints;
    }
}
