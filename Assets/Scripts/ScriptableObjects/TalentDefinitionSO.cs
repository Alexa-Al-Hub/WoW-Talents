using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

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

        [Header("Requirements")]
        [Tooltip("Required points in the tree before this talent can be learned")]
        public int RequiredTreePoints;

        [Header("Descriptions")]
        [Tooltip("Each line is a description for a new rank. The number of lines equals the maximum talent rank.")]
        [TextArea(2, 4)]
        [RequiredMember]
        public string[] RankDescriptions;

        [Header("Talent dependency")]
        [Tooltip("Every listed talent must be fully maxed before this talent can be learned")]
        public List<TalentDefinitionSO> RequiredTalents = new();

        public int MaxRank
        {
            get
            {
                if (RankDescriptions == null || RankDescriptions.Length == 0)
                {
                    return 1;
                }

                return RankDescriptions.Length;
            }
        }

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
