using System;
using System.Collections.Generic;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Targeting
{
    /// <summary>
    /// Chooses the target of a walking-around action from the candidates in reach (controls.md, Targeting).
    /// Candidates that another player has a queued command on (<see cref="SimWorld.PendingTargets"/>) are skipped.
    /// </summary>
    public static class SmartTarget
    {
        /// <summary>
        /// The best eligible candidate for <paramref name="action"/>, or null if none is eligible. Ranked by the strategy;
        /// ties go to the lowest entity id.
        /// </summary>
        /// <param name="action">The strategy for the action, e.g. <see cref="WaterTargetStrategy"/>.</param>
        /// <param name="player">The player doing the action. Their own pending claims do not exclude a target.</param>
        /// <param name="candidates">Candidates in reach.</param>
        /// <param name="world">The sim, for the pending claims.</param>
        public static TargetCandidate Pick(ISmartTargetStrategy action, PlayerId player,
            IEnumerable<TargetCandidate> candidates, SimWorld world)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (world == null) throw new ArgumentNullException(nameof(world));

            var claimed = new HashSet<string>(world.PendingTargets(player));
            TargetCandidate best = null;
            foreach (var c in candidates)
            {
                if (c == null || claimed.Contains(c.TargetKey) || !action.IsEligible(c)) continue;
                if (best == null) { best = c; continue; }
                var order = action.Compare(c, best);
                if (order < 0 || (order == 0 && c.EntityId < best.EntityId)) best = c;
            }
            return best;
        }
    }
}
