using System.Collections.Generic;
using Robotech.TBS.Data;
using Robotech.TBS.Map;
using Robotech.TBS.Systems;
using Robotech.TBS.Units;

namespace Robotech.TBS.Combat
{
    /// <summary>
    /// Reaction-shot system. When a unit enters a hex during movement, any enemy unit that
    /// (a) has the overwatch ability, (b) is currently in overwatch, (c) has not yet attacked
    /// this turn, and (d) can see + reach the mover via CombatResolver.CanAttack, gets one
    /// free attack via CombatResolver.ResolveAttack. The overwatcher's overwatch flag is
    /// cleared after firing.
    /// </summary>
    public static class OverwatchSystem
    {
        /// <summary>
        /// Trigger reaction shots from enemy overwatchers against the mover at its current hex.
        /// Pulls enemies via UnitRegistry; if no registry is present, returns false (no-op).
        /// </summary>
        /// <param name="mover">The unit that just stepped into a new hex.</param>
        /// <param name="mapGen">Map generator (used by CombatResolver for LoS + cover lookups).</param>
        /// <returns>True if the mover was destroyed by overwatch fire this call.</returns>
        public static bool TriggerOnMove(Unit mover, MapGenerator mapGen)
        {
            if (mover == null || mover.definition == null) return false;
            if (UnitRegistry.Instance == null) return false;

            var enemies = UnitRegistry.Instance.GetEnemyUnits(mover.definition.faction);
            if (enemies == null || enemies.Count == 0) return false;

            // Snapshot eligible overwatchers so we can mutate their flags during iteration.
            var overwatchers = new List<Unit>();
            foreach (var enemy in enemies)
            {
                if (IsEligibleOverwatcher(enemy)) overwatchers.Add(enemy);
            }

            foreach (var ow in overwatchers)
            {
                if (mover == null || mover.currentHP <= 0) return true;
                if (!CombatResolver.CanAttack(ow, mover, mapGen)) continue;

                var result = CombatResolver.ResolveAttack(ow, mover, mapGen);
                ow.hasAttackedThisTurn = true;
                ow.isOverwatching = false; // overwatch is one-shot

                if (result.TargetDestroyed) return true;
            }

            return false;
        }

        /// <summary>
        /// Predicate check: is this unit a valid overwatcher (alive, canOverwatch, in overwatch,
        /// hasn't fired this turn)? Pure logic — no scene dependencies, used by tests.
        /// </summary>
        public static bool IsEligibleOverwatcher(Unit unit)
        {
            if (unit == null || unit.definition == null) return false;
            if (!unit.definition.canOverwatch) return false;
            if (!unit.isOverwatching) return false;
            if (unit.hasAttackedThisTurn) return false;
            if (unit.currentHP <= 0) return false;
            return true;
        }
    }
}
