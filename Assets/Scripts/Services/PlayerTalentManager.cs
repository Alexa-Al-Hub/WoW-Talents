using System.Collections.Generic;
using UnityEngine;

public class PlayerTalentManager
{
    public int AvailablePoints { get; private set; }

    private Dictionary<string, int> _investedTalents;
    private Dictionary<string, int> _pointsPerTree;

    public PlayerTalentManager(int startingPoints)
    {
        AvailablePoints = startingPoints;
        _investedTalents = new Dictionary<string, int>();
        _pointsPerTree = new Dictionary<string, int>();
    }

    public int GetTalentRank(string talentId)
    {
        return _investedTalents.TryGetValue(talentId, out int rank) ? rank : 0;
    }

    public int GetPointsInTree(string treeId)
    {
        if (string.IsNullOrEmpty(treeId)) return 0;
        return _pointsPerTree.TryGetValue(treeId, out int points) ? points : 0;
    }

    public bool CanInvestInTalent(TalentDefinitionSO talentDef)
    {
        if (AvailablePoints <= 0) return false;

        int currentRank = GetTalentRank(talentDef.Id);
        if (currentRank >= talentDef.MaxRank) return false;

        int pointsInThisTree = GetPointsInTree(talentDef.TreeId);
        if (pointsInThisTree < talentDef.RequiredTreePoints) return false;

        if (talentDef.RequiredTalent != null)
        {
            int requiredTalentRank = GetTalentRank(talentDef.RequiredTalent.Id);

            if (requiredTalentRank < 1)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryInvestPoint(TalentDefinitionSO talentDef)
    {
        if (!CanInvestInTalent(talentDef))
        {
            Debug.LogWarning($"[Talent Manager] Could not save the talent {talentDef.DisplayName}");
            return false;
        }

        AvailablePoints--;

        int currentRank = GetTalentRank(talentDef.Id);
        _investedTalents[talentDef.Id] = currentRank + 1;

        if (!string.IsNullOrEmpty(talentDef.TreeId))
        {
            int currentTreePoints = GetPointsInTree(talentDef.TreeId);
            _pointsPerTree[talentDef.TreeId] = currentTreePoints + 1;
        }

        Debug.Log($"[Talent Manager] Successfully upgraded talent {talentDef.DisplayName}. Current rank: {GetTalentRank(talentDef.Id)}. Points left: {AvailablePoints}");

        return true;
    }
}