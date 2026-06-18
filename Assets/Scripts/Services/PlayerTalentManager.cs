using System.Collections.Generic;
using UnityEngine;

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

        if (talentDef.RequiredTalent != null && GetTalentRank(talentDef.RequiredTalent.Id) < 1) return false;

        return true;
    }

    public bool TryInvestPoint(TalentTreeSO tree, TalentDefinitionSO talentDef)
    {
        if (!CanInvestInTalent(tree, talentDef))
        {
            Debug.LogWarning($"[Talent Manager] Could not save the talent {talentDef.DisplayName}");
            return false;
        }

        AvailablePoints--;
        _investedTalents[talentDef.Id] = GetTalentRank(talentDef.Id) + 1;

        if (tree != null)
            _pointsPerTree[tree] = GetPointsInTree(tree) + 1;

        Debug.Log($"[Talent Manager] Successfully upgraded talent {talentDef.DisplayName}. Current rank: {GetTalentRank(talentDef.Id)}. Points left: {AvailablePoints}");

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

        Debug.Log($"[Talent Manager] Reset tree {tree.TreeName}. Refunded {refundedPoints} points. Points left: {AvailablePoints}");

        return refundedPoints;
    }
}
