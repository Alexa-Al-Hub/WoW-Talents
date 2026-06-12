using System.Collections.Generic;
using UnityEngine;

public class PlayerTalentManager
{
    public int AvailablePoints;

    private Dictionary<string, int> _investedTalents;

    public PlayerTalentManager(int startingPoints)
    {
        AvailablePoints = startingPoints;
        _investedTalents = new Dictionary<string, int>();
    }

    public int GetTalentRank(string talentId)
    {
        if (_investedTalents.TryGetValue(talentId, out int rank))
        {
            return rank;
        }
        return 0;
    }

    public bool CanInvestInTalent(TalentDefinitionSO talentDef)
    {
        if (AvailablePoints <= 0) return false;

        int currentRank = GetTalentRank(talentDef.Id);
        if (currentRank >= talentDef.MaxRank) return false;

        // review dependencies

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

        Debug.Log($"[Talent Manager] Successfully upgraded talent {talentDef.DisplayName}. Current rank: {GetTalentRank(talentDef.Id)}. Points left: {AvailablePoints}");

        return true;
    }
}