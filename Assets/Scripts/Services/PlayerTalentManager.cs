using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
    public class PlayerTalentManager
    {
        public int AvailablePoints { get; private set; }

        private readonly Dictionary<string, int> _investedTalents = new();
        private readonly Dictionary<TalentTreeSO, int> _pointsPerTree = new();

        public PlayerTalentManager(int startingPoints)
        {
            AvailablePoints = startingPoints;
        }

        public int GetTalentRank(string talentId)
        {
            return _investedTalents.TryGetValue(talentId, out int rank) ? rank : 0;
        }

        public int GetPointsInTree(TalentTreeSO tree)
        {
            if (tree == null) return 0;
            return _pointsPerTree.TryGetValue(tree, out int points) ? points : 0;
        }

        public bool CanInvestInTalent(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (AvailablePoints <= 0) return false;

            if (GetTalentRank(talentDef.Id) >= talentDef.MaxRank) return false;

            if (GetPointsInTree(tree) < talentDef.RequiredTreePoints) return false;

            return ArePrerequisitesMaxed(talentDef);
        }

        public bool TryInvestPoint(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (!CanInvestInTalent(tree, talentDef))
            {
                Debug.LogWarning($"[Talent Manager] Could not invest in talent {talentDef.DisplayName}");
                return false;
            }

            AvailablePoints--;
            _investedTalents[talentDef.Id] = GetTalentRank(talentDef.Id) + 1;

            if (tree != null)
            {
                _pointsPerTree[tree] = GetPointsInTree(tree) + 1;

            }

            return true;
        }

        public bool TryRemovePoint(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            int currentRank = GetTalentRank(talentDef.Id);
            if (currentRank <= 0) return false;

            if (WouldInvalidateTree(tree, talentDef))
            {
                Debug.LogWarning($"[Talent Manager] Cannot remove {talentDef.DisplayName}: a higher talent still requires these points.");
                return false;
            }

            int newRank = currentRank - 1;
            if (newRank == 0)
                _investedTalents.Remove(talentDef.Id);
            else
                _investedTalents[talentDef.Id] = newRank;

            AvailablePoints++;

            if (tree != null)
                _pointsPerTree[tree] = Mathf.Max(0, GetPointsInTree(tree) - 1);

            return true;
        }

        public int ResetTree(TalentTreeSO tree)
        {
            int refundedPoints = GetPointsInTree(tree);
            if (refundedPoints <= 0) return 0;

            if (tree.Nodes != null)
            {
                foreach (var node in tree.Nodes)
                {
                    if (node?.Definition != null)
                        _investedTalents.Remove(node.Definition.Id);
                }
            }

            _pointsPerTree[tree] = 0;
            AvailablePoints += refundedPoints;

            return refundedPoints;
        }

        private bool ArePrerequisitesMaxed(TalentDefinitionSO talentDef)
        {
            if (talentDef.RequiredTalents == null) return true;

            foreach (var requiredTalent in talentDef.RequiredTalents)
            {
                if (requiredTalent == null) continue;
                if (GetTalentRank(requiredTalent.Id) < requiredTalent.MaxRank) return false;
            }

            return true;
        }

        private bool WouldInvalidateTree(TalentTreeSO tree, TalentDefinitionSO removedTalent)
        {
            if (tree?.Nodes == null) return false;

            foreach (var node in tree.Nodes)
            {
                var dependentTalent = node?.Definition;
                if (dependentTalent == null) continue;
                if (RankAfterRemoval(dependentTalent, removedTalent) <= 0) continue;

                if (dependentTalent.RequiredTreePoints > 0 &&
                    SupportingPointsAfterRemoval(tree, dependentTalent, removedTalent) < dependentTalent.RequiredTreePoints)
                    return true;

                if (dependentTalent.RequiredTalents == null) continue;
                foreach (var requiredTalent in dependentTalent.RequiredTalents)
                {
                    if (requiredTalent == null) continue;
                    if (RankAfterRemoval(requiredTalent, removedTalent) < requiredTalent.MaxRank) return true;
                }
            }

            return false;
        }

        private int SupportingPointsAfterRemoval(TalentTreeSO tree, TalentDefinitionSO dependentTalent, TalentDefinitionSO removedTalent)
        {
            int supportingPoints = 0;
            foreach (var node in tree.Nodes)
            {
                var lowerTalent = node?.Definition;
                if (lowerTalent == null) continue;
                if (lowerTalent.RequiredTreePoints >= dependentTalent.RequiredTreePoints) continue;
                supportingPoints += RankAfterRemoval(lowerTalent, removedTalent);
            }
            return supportingPoints;
        }

        private int RankAfterRemoval(TalentDefinitionSO talent, TalentDefinitionSO removedTalent)
        {
            int rank = GetTalentRank(talent.Id);
            return talent.Id == removedTalent.Id ? rank - 1 : rank;
        }
    }
}
