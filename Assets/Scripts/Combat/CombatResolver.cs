using UnityEngine;
using Robotech.TBS.Units;
using Robotech.TBS.Data;

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
        /// <returns>Combat result with damage dealt and outcome</returns>
        public static CombatResult ResolveAttack(Unit attacker, Unit target)
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

            // Apply damage
            int targetHPBefore = target.currentHP;
            target.TakeDamage(totalDamage);

            // Check if target was destroyed (currentHP <= 0 triggers Destroy)
            bool destroyed = target.currentHP <= 0;

            Debug.Log($"Combat: {attacker.definition.displayName} dealt {totalDamage} damage to {target.definition.displayName} ({targetHPBefore} -> {target.currentHP} HP){(destroyed ? " - DESTROYED" : "")}");

            return new CombatResult
            {
                Succeeded = true,
                TotalDamage = totalDamage,
                HitsLanded = totalHits,
                TargetDestroyed = destroyed
            };
        }

        /// <summary>
        /// Check if an attack is valid without resolving it.
        /// </summary>
        public static bool CanAttack(Unit attacker, Unit target)
        {
            if (attacker == null || target == null) return false;
            if (attacker.definition.faction == target.definition.faction) return false;
            if (attacker.definition.weapons == null || attacker.definition.weapons.Length == 0) return false;

            int distance = attacker.coord.Distance(target.coord);

            // Check if any weapon can reach
            foreach (var weapon in attacker.definition.weapons)
            {
                if (weapon != null && distance >= weapon.rangeMin && distance <= weapon.rangeMax)
                    return true;
            }
            return false;
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
