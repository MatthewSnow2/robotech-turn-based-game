using UnityEngine;
using Robotech.TBS.Units;
using Robotech.TBS.Data;
using Robotech.TBS.Map;

namespace Robotech.TBS.Combat
{
    /// <summary>
    /// Combat result returned by ResolveAttack for UI/logging purposes.
    /// </summary>
    public struct CombatResult
    {
        public bool Succeeded;
        public int TotalDamage;
        public int HitsLanded;
        public bool TargetDestroyed;
        public string FailureReason;

        public static CombatResult Failed(string reason) => new CombatResult
        {
            Succeeded = false,
            FailureReason = reason
        };
    }

    public static class CombatResolver
    {
        /// <summary>
        /// Resolve an attack between two units with full validation.
        /// </summary>
        /// <param name="attacker">The attacking unit</param>
        /// <param name="target">The target unit</param>
        /// <param name="mapGen">Map generator for terrain lookups (LoS + cover). Pass null to skip both.</param>
        /// <returns>Combat result with damage dealt and outcome</returns>
        public static CombatResult ResolveAttack(Unit attacker, Unit target, MapGenerator mapGen)
        {
            // Null checks
            if (attacker == null || target == null)
                return CombatResult.Failed("Invalid attacker or target");

            // Friendly fire prevention
            if (attacker.definition.faction == target.definition.faction)
                return CombatResult.Failed("Cannot attack friendly units");

            // Check weapons
            if (attacker.definition.weapons == null || attacker.definition.weapons.Length == 0)
                return CombatResult.Failed("Attacker has no weapons");

            // Line of sight (skipped if mapGen is null)
            if (!LineOfSight.HasLineOfSight(attacker.coord, target.coord, mapGen))
                return CombatResult.Failed("No line of sight (terrain blocks)");

            // Calculate distance for range checks
            int distance = attacker.coord.Distance(target.coord);

            // Get tech attack bonus
            int attackBonus = attacker.GetAttackBonus();

            int totalDamage = 0;
            int totalHits = 0;

            foreach (var weapon in attacker.definition.weapons)
            {
                if (weapon == null) continue;

                // Range validation - weapon must be able to reach target
                if (distance < weapon.rangeMin || distance > weapon.rangeMax)
                {
                    Debug.Log($"{weapon.displayName} out of range (distance: {distance}, range: {weapon.rangeMin}-{weapon.rangeMax})");
                    continue; // Skip this weapon, try others
                }

                // Calculate accuracy with potential modifiers
                float hitChance = Mathf.Clamp01(weapon.accuracyBase);

                // Roll for each salvo
                int hits = 0;
                int salvos = Mathf.Max(1, weapon.salvoCount);
                for (int i = 0; i < salvos; i++)
                {
                    if (Random.value <= hitChance) hits++;
                }

                // Calculate damage with tech attack bonus
                int baseDamage = Mathf.Max(0, weapon.damage);
                int damageWithBonus = baseDamage + attackBonus;
                int weaponDamage = hits * damageWithBonus;

                totalDamage += weaponDamage;
                totalHits += hits;

                if (hits > 0)
                {
                    Debug.Log($"{weapon.displayName}: {hits}/{salvos} hits for {weaponDamage} damage (base: {baseDamage}, bonus: +{attackBonus})");
                }
            }

            // No weapons in range
            if (totalDamage == 0 && totalHits == 0)
            {
                // Check if ALL weapons were out of range
                bool anyInRange = false;
                foreach (var w in attacker.definition.weapons)
                {
                    if (w != null && distance >= w.rangeMin && distance <= w.rangeMax)
                    {
                        anyInRange = true;
                        break;
                    }
                }
                if (!anyInRange)
                    return CombatResult.Failed($"No weapons in range (distance: {distance})");
            }

            // Apply target terrain cover (flat damage reduction before armor in TakeDamage)
            int terrainDefense = 0;
            if (mapGen != null)
            {
                var targetTerrain = mapGen.GetTerrain(target.coord);
                if (targetTerrain != null) terrainDefense = targetTerrain.defenseBonus;
            }
            int damageAfterCover = Mathf.Max(0, totalDamage - terrainDefense);
            if (terrainDefense > 0 && totalDamage > 0)
            {
                Debug.Log($"Cover: target terrain defense -{terrainDefense} ({totalDamage} -> {damageAfterCover})");
            }

            // Apply damage
            int targetHPBefore = target.currentHP;
            target.TakeDamage(damageAfterCover);

            // Check if target was destroyed (currentHP <= 0 triggers Destroy)
            bool destroyed = target.currentHP <= 0;

            Debug.Log($"Combat: {attacker.definition.displayName} dealt {damageAfterCover} damage to {target.definition.displayName} ({targetHPBefore} -> {target.currentHP} HP){(destroyed ? " - DESTROYED" : "")}");

            return new CombatResult
            {
                Succeeded = true,
                TotalDamage = damageAfterCover,
                HitsLanded = totalHits,
                TargetDestroyed = destroyed
            };
        }

        /// <summary>
        /// Check if an attack is valid without resolving it.
        /// </summary>
        /// <param name="mapGen">Map generator for LoS check. Pass null to skip the LoS guard.</param>
        public static bool CanAttack(Unit attacker, Unit target, MapGenerator mapGen)
        {
            if (attacker == null || target == null) return false;
            if (attacker.definition.faction == target.definition.faction) return false;
            if (attacker.definition.weapons == null || attacker.definition.weapons.Length == 0) return false;

            int distance = attacker.coord.Distance(target.coord);

            // Check if any weapon can reach
            bool anyInRange = false;
            foreach (var weapon in attacker.definition.weapons)
            {
                if (weapon != null && distance >= weapon.rangeMin && distance <= weapon.rangeMax)
                {
                    anyInRange = true;
                    break;
                }
            }
            if (!anyInRange) return false;

            // LoS check
            return LineOfSight.HasLineOfSight(attacker.coord, target.coord, mapGen);
        }

        /// <summary>
        /// Get the maximum attack range for a unit based on its weapons.
        /// </summary>
        public static int GetMaxRange(Unit unit)
        {
            if (unit?.definition?.weapons == null) return 0;

            int maxRange = 0;
            foreach (var weapon in unit.definition.weapons)
            {
                if (weapon != null && weapon.rangeMax > maxRange)
                    maxRange = weapon.rangeMax;
            }
            return maxRange;
        }
    }
}
