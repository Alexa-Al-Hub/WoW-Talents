using System.Collections.Generic;

namespace TalentTree
{
    // Guards the talent prerequisite graph (TalentDefinitionSO.RequiredTalents).
    // A cycle means every talent in it requires another to be maxed first, so none can
    // ever be learned. That is never valid in a talent tree, so we reject it on edit.
    public static class TalentDependencyValidator
    {
        // True if the talent (transitively) lists itself as a prerequisite. OnValidate runs
        // on the edited talent, and any new cycle must pass through it, so checking whether
        // it depends on itself is enough — no need to scan the whole tree for arbitrary loops.
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
