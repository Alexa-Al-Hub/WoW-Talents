using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
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
        [Tooltip("Required points in the tree before this talent can be learned")]
        public int RequiredTreePoints;

        [Header("Talent dependency")]
        [Tooltip("Every listed talent must be fully maxed before this talent can be learned")]
        public List<TalentDefinitionSO> RequiredTalents = new();

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = name;
            }

            if (TalentDependencyValidator.HasCircularDependency(this))
            {
                Debug.LogError(
                    $"[Talent] '{name}' has a circular dependency in RequiredTalents — " +
                    "a talent that (directly or indirectly) requires itself can never be learned.",
                    this);
            }
        }
    }
}
