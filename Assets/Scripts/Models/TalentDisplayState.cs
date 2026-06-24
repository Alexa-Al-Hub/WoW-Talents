namespace TalentTree
{
    public record TalentDisplayState(int Rank, int MaxRank, bool CanInvest, TalentLockReason LockReason, TalentDefinitionSO BlockingPrerequisite)
    {
        public bool RequirementsMet => LockReason == TalentLockReason.None;
    }
}
