using System.Collections.Generic;

namespace TalentTree
{
    public static class TalentDependencyValidator
    {
        public static bool HasCircularDependency(TalentDefinitionSO talent)
        {
            return talent != null && DependsOn(talent, talent, new HashSet<TalentDefinitionSO>());
        }

        private static bool DependsOn(
            TalentDefinitionSO talent,
            TalentDefinitionSO target,
            HashSet<TalentDefinitionSO> visited)
        {
            if (talent.RequiredTalents == null)
            {
                return false;
            }

            foreach (var requiredTalent in talent.RequiredTalents)
            {
                if (requiredTalent == null)
                {
                    continue;
                }

                if (requiredTalent == target)
                {
                    return true;
                }

                if (visited.Add(requiredTalent) && DependsOn(requiredTalent, target, visited))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
