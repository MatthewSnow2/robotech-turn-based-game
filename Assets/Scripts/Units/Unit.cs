using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Hex;
using Robotech.TBS.Data;
using Robotech.TBS.Systems;
using Robotech.TBS.Map;

namespace Robotech.TBS.Units
{
    [RequireComponent(typeof(Collider))]
    public class Unit : MonoBehaviour
    {
        public UnitDefinition definition;
        public HexCoord coord;
        public int currentHP;
        public int movesLeft;

        // Tech upgrade tracking
        private List<TechDefinition> appliedTechUpgrades = new();
        private int maxHPBonus = 0;
        private int armorBonus = 0;
        private int movementBonus = 0;
        private int attackBonus = 0;

        public void Init(UnitDefinition def, HexCoord spawn, float hexSize)
        {
            definition = def;
            coord = spawn;
            currentHP = definition.maxHP;
            movesLeft = definition.movement;
            transform.position = coord.ToWorld(hexSize) + Vector3.up * 0.5f;
            name = $"Unit_{definition.displayName}_{coord.q}_{coord.r}";

            // Register with UnitRegistry
            if (UnitRegistry.Instance != null)
            {
                UnitRegistry.Instance.Register(this);
            }
        }

        void OnDestroy()
        {
            // Unregister from UnitRegistry
            if (UnitRegistry.Instance != null)
            {
                UnitRegistry.Instance.Unregister(this);
            }
        }

        public void NewTurn()
        {
            movesLeft = definition.movement + movementBonus;
        }

        /// <summary>
        /// Check if the unit can move to an adjacent hex (legacy single-step movement).
        /// </summary>
        public bool CanMoveTo(HexCoord target, System.Func<HexCoord, bool> passable)
        {
            if (coord.Distance(target) != 1) return false; // simple adjacent step
            if (!passable(target)) return false;
            return movesLeft > 0;
        }

        /// <summary>
        /// Check if the unit can reach a target hex using pathfinding.
        /// </summary>
        /// <param name="target">Target hex coordinate</param>
        /// <param name="grid">The hex grid for bounds checking</param>
        /// <param name="mapGen">Map generator for terrain data</param>
        /// <returns>True if a valid path exists within remaining movement points</returns>
        public bool CanReach(HexCoord target, HexGrid grid, MapGenerator mapGen)
        {
            if (movesLeft <= 0) return false;
            var result = Pathfinder.FindPath(coord, target, grid, mapGen, definition, movesLeft);
            return result.Success;
        }

        /// <summary>
        /// Move to an adjacent hex (legacy single-step movement).
        /// </summary>
        public void MoveTo(HexCoord target, float hexSize)
        {
            var oldCoord = coord;
            coord = target;
            movesLeft = Mathf.Max(0, movesLeft - 1);
            transform.position = coord.ToWorld(hexSize) + Vector3.up * 0.5f;

            // Update UnitRegistry with new position
            if (UnitRegistry.Instance != null)
            {
                UnitRegistry.Instance.UpdatePosition(this, oldCoord, coord);
            }
        }

        /// <summary>
        /// Move the unit along a path, deducting movement costs based on terrain.
        /// </summary>
        /// <param name="path">List of hex coordinates to traverse (including start position)</param>
        /// <param name="hexSize">Size of hexes for world position calculation</param>
        /// <param name="mapGen">Map generator for terrain cost lookup</param>
        /// <returns>True if movement was successful, false if path was invalid</returns>
        public bool MoveAlongPath(List<HexCoord> path, float hexSize, MapGenerator mapGen)
        {
            if (path == null || path.Count < 2) return false;
            if (movesLeft <= 0) return false;

            // Verify path starts at current position
            if (path[0].q != coord.q || path[0].r != coord.r) return false;

            var oldCoord = coord;
            int totalCost = 0;

            // Calculate and deduct movement cost for each step
            for (int i = 1; i < path.Count; i++)
            {
                var terrain = mapGen.GetTerrain(path[i]);
                int stepCost = Pathfinder.GetMovementCost(terrain);

                if (totalCost + stepCost > movesLeft)
                {
                    // Can't afford this step, stop at previous position
                    if (i > 1)
                    {
                        coord = path[i - 1];
                        movesLeft -= totalCost;
                        transform.position = coord.ToWorld(hexSize) + Vector3.up * 0.5f;

                        if (UnitRegistry.Instance != null)
                        {
                            UnitRegistry.Instance.UpdatePosition(this, oldCoord, coord);
                        }
                        return true;
                    }
                    return false;
                }

                totalCost += stepCost;
            }

            // Successfully traversed entire path
            coord = path[path.Count - 1];
            movesLeft -= totalCost;
            transform.position = coord.ToWorld(hexSize) + Vector3.up * 0.5f;

            if (UnitRegistry.Instance != null)
            {
                UnitRegistry.Instance.UpdatePosition(this, oldCoord, coord);
            }
            return true;
        }

        /// <summary>
        /// Move the unit to a target hex using pathfinding.
        /// </summary>
        /// <param name="target">Target hex coordinate</param>
        /// <param name="hexSize">Size of hexes for world position calculation</param>
        /// <param name="grid">The hex grid for bounds checking</param>
        /// <param name="mapGen">Map generator for terrain data</param>
        /// <returns>PathResult with success status and path taken</returns>
        public PathResult MoveToTarget(HexCoord target, float hexSize, HexGrid grid, MapGenerator mapGen)
        {
            if (movesLeft <= 0)
            {
                return PathResult.Failed();
            }

            // Find path within movement budget
            var result = Pathfinder.FindPathWithBudget(coord, target, movesLeft, grid, mapGen, definition);

            if (!result.Success || result.Path.Count < 2)
            {
                return PathResult.Failed();
            }

            // Execute the movement
            if (MoveAlongPath(result.Path, hexSize, mapGen))
            {
                return result;
            }

            return PathResult.Failed();
        }

        /// <summary>
        /// Get all hexes this unit can reach with current movement points.
        /// </summary>
        /// <param name="grid">The hex grid for bounds checking</param>
        /// <param name="mapGen">Map generator for terrain data</param>
        /// <returns>Dictionary of reachable hexes with their movement costs</returns>
        public Dictionary<HexCoord, int> GetReachableHexes(HexGrid grid, MapGenerator mapGen)
        {
            if (movesLeft <= 0)
            {
                return new Dictionary<HexCoord, int>();
            }

            return Pathfinder.GetReachableHexes(coord, movesLeft, grid, mapGen, definition);
        }

        public void TakeDamage(int amount)
        {
            int final = Mathf.Max(0, amount - (definition.armor + armorBonus));
            currentHP -= final;
            if (currentHP <= 0)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Apply a technology upgrade to this unit. Bonuses stack and are applied immediately.
        /// Tech upgrades are only applied once per tech to prevent duplicate bonuses.
        /// </summary>
        /// <param name="tech">The technology to apply</param>
        public void ApplyTechUpgrade(TechDefinition tech)
        {
            if (tech == null) return;

            // Check if tech has already been applied
            if (appliedTechUpgrades.Contains(tech))
            {
                return;
            }

            // Apply HP bonus
            if (tech.hpBonus > 0)
            {
                maxHPBonus += tech.hpBonus;
                currentHP += tech.hpBonus;
                Debug.Log($"Unit {definition.displayName} upgraded by {tech.displayName}: +{tech.hpBonus} HP (total bonus: +{maxHPBonus})");
            }

            // Apply armor bonus
            if (tech.armorBonus > 0)
            {
                armorBonus += tech.armorBonus;
                Debug.Log($"Unit {definition.displayName} upgraded by {tech.displayName}: +{tech.armorBonus} armor (total bonus: +{armorBonus})");
            }

            // Apply movement bonus
            if (tech.movementBonus > 0)
            {
                movementBonus += tech.movementBonus;
                movesLeft += tech.movementBonus; // Apply to current turn as well
                Debug.Log($"Unit {definition.displayName} upgraded by {tech.displayName}: +{tech.movementBonus} movement (total bonus: +{movementBonus})");
            }

            // Apply attack bonus
            if (tech.attackBonus > 0)
            {
                attackBonus += tech.attackBonus;
                Debug.Log($"Unit {definition.displayName} upgraded by {tech.displayName}: +{tech.attackBonus} attack (total bonus: +{attackBonus})");
            }

            // Add tech to applied upgrades list
            appliedTechUpgrades.Add(tech);
        }

        /// <summary>
        /// Check if a specific tech upgrade has already been applied to this unit.
        /// </summary>
        /// <param name="tech">The technology to check</param>
        /// <returns>True if the tech upgrade has been applied, false otherwise</returns>
        public bool HasTechUpgrade(TechDefinition tech)
        {
            return appliedTechUpgrades.Contains(tech);
        }

        /// <summary>
        /// Get the total maximum HP including base and all tech bonuses.
        /// </summary>
        public int GetMaxHP()
        {
            return definition.maxHP + maxHPBonus;
        }

        /// <summary>
        /// Get the total armor including base and all tech bonuses.
        /// </summary>
        public int GetArmor()
        {
            return definition.armor + armorBonus;
        }

        /// <summary>
        /// Get the total movement including base and all tech bonuses.
        /// </summary>
        public int GetMovement()
        {
            return definition.movement + movementBonus;
        }

        /// <summary>
        /// Get the attack bonus from tech upgrades.
        /// </summary>
        public int GetAttackBonus()
        {
            return attackBonus;
        }
    }
}
