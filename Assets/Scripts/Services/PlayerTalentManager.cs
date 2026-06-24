using System.Collections.Generic;
using UnityEngine;

namespace TalentTree
{
    public class PlayerTalentManager
    {
        private readonly int _totalTalentPoints;
        private readonly Dictionary<TalentDefinitionSO, int> _ranksByTalent = new();

        public PlayerTalentManager(int startingPoints)
        {
            _totalTalentPoints = startingPoints;
        }

        public int AvailablePoints => _totalTalentPoints - SpentPoints();

        private int SpentPoints()
        {
            int spent = 0;
            foreach (int rank in _ranksByTalent.Values)
            {
                spent += rank;
            }
            return spent;
        }

        public int GetTalentRank(TalentDefinitionSO talent)
        {
            if (talent == null)
            {
                return 0;
            }

            return _ranksByTalent.TryGetValue(talent, out int rank) ? rank : 0;
        }

        public int GetPointsInTree(TalentTreeSO tree)
        {
            if (tree?.Nodes == null)
            {
                return 0;
            }

            int points = 0;
            foreach (var node in tree.Nodes)
            {
                if (node?.Definition != null)
                {
                    points += GetTalentRank(node.Definition);
                }
            }
            return points;
        }

        public bool AreRequirementsMet(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (GetPointsInTree(tree) < talentDef.RequiredTreePoints)
            {
                return false;
            }

            return ArePrerequisitesMaxed(talentDef);
        }

        public bool CanInvestInTalent(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (AvailablePoints <= 0)
            {
                return false;
            }

            if (GetTalentRank(talentDef) >= talentDef.MaxRank)
            {
                return false;
            }

            return AreRequirementsMet(tree, talentDef);
        }

        public TalentDisplayState GetDisplayState(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            int rank = GetTalentRank(talentDef);
            bool requirementsMet = AreRequirementsMet(tree, talentDef);
            bool canInvest = requirementsMet && rank < talentDef.MaxRank && AvailablePoints > 0;

            return new TalentDisplayState(rank, talentDef.MaxRank, requirementsMet, canInvest);
        }

        public bool TryInvestPoint(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            if (!CanInvestInTalent(tree, talentDef))
            {
                Debug.LogWarning($"[Talent Manager] Could not invest in talent {talentDef.DisplayName}");
                return false;
            }

            _ranksByTalent[talentDef] = GetTalentRank(talentDef) + 1;
            return true;
        }

        public bool TryRemovePoint(TalentTreeSO tree, TalentDefinitionSO talentDef)
        {
            int currentRank = GetTalentRank(talentDef);
            if (currentRank <= 0)
            {
                return false;
            }

            if (WouldInvalidateTree(tree, talentDef))
            {
                Debug.LogWarning($"[Talent Manager] Cannot remove {talentDef.DisplayName}: a higher talent still requires these points.");
                return false;
            }

            int newRank = currentRank - 1;
            if (newRank == 0)
            {
                _ranksByTalent.Remove(talentDef);
            }
            else
            {
                _ranksByTalent[talentDef] = newRank;
            }

            return true;
        }

        public int ResetTree(TalentTreeSO tree)
        {
            int refundedPoints = GetPointsInTree(tree);
            if (refundedPoints <= 0)
            {
                return 0;
            }

            if (tree.Nodes != null)
            {
                foreach (var node in tree.Nodes)
                {
                    if (node?.Definition != null)
                    {
                        _ranksByTalent.Remove(node.Definition);
                    }
                }
            }

            return refundedPoints;
        }

        private bool ArePrerequisitesMaxed(TalentDefinitionSO talentDef)
        {
            if (talentDef.RequiredTalents == null)
            {
                return true;
            }

            foreach (var requiredTalent in talentDef.RequiredTalents)
            {
                if (requiredTalent == null)
                {
                    continue;
                }

                if (GetTalentRank(requiredTalent) < requiredTalent.MaxRank)
                {
                    return false;
                }
            }

            return true;
        }

        private bool WouldInvalidateTree(TalentTreeSO tree, TalentDefinitionSO removedTalent)
        {
            if (tree?.Nodes == null)
            {
                return false;
            }

            int treePointsAfterRemoval = GetPointsInTree(tree) - 1;

            foreach (var node in tree.Nodes)
            {
                var dependentTalent = node?.Definition;
                if (dependentTalent == null)
                {
                    continue;
                }
                if (RankAfterRemoval(dependentTalent, removedTalent) <= 0)
                {
                    continue;
                }

                if (treePointsAfterRemoval < dependentTalent.RequiredTreePoints)
                {
                    return true;
                }

                if (dependentTalent.RequiredTalents == null)
                {
                    continue;
                }
                foreach (var requiredTalent in dependentTalent.RequiredTalents)
                {
                    if (requiredTalent == null)
                    {
                        continue;
                    }
                    if (RankAfterRemoval(requiredTalent, removedTalent) < requiredTalent.MaxRank)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int RankAfterRemoval(TalentDefinitionSO talent, TalentDefinitionSO removedTalent)
        {
            int rank = GetTalentRank(talent);

            return talent == removedTalent ? rank - 1 : rank;
        }
    }
}
