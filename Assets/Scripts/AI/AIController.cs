using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Core;
using Robotech.TBS.Units;
using Robotech.TBS.Data;
using Robotech.TBS.Hex;
using Robotech.TBS.Systems;
using Robotech.TBS.Combat;
using Robotech.TBS.Cities;

namespace Robotech.TBS.AI
{
    /// <summary>
    /// Main AI controller that orchestrates AI decision-making during the AI phase.
    /// Manages unit movement, combat, city production, and tech research for the AI faction.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Faction aiFaction = Faction.Zentradi;
        [SerializeField] private float actionDelay = 0.3f; // Delay between AI actions for visual feedback

        [Header("References")]
        public HexGrid grid;
        public TechManager techManager;
        public CityManager cityManager;
        public Map.MapGenerator mapGen;

        /// <summary>
        /// Event fired when the AI completes all actions for this phase.
        /// </summary>
        public static event System.Action OnAIPhaseComplete;

        private bool isProcessing = false;

        void Awake()
        {
            // Subscribe to phase changes
            TurnManager.OnPhaseChanged += OnPhaseChanged;
        }

        void OnDestroy()
        {
            TurnManager.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(TurnManager.TurnPhase phase)
        {
            if (phase == TurnManager.TurnPhase.AI && !isProcessing)
            {
                StartCoroutine(ExecuteAIPhase());
            }
        }

        /// <summary>
        /// Main AI phase execution coroutine.
        /// Processes all AI decisions in sequence with delays for visual feedback.
        /// </summary>
        private IEnumerator ExecuteAIPhase()
        {
            isProcessing = true;
            Debug.Log($"[AI] Starting AI phase for {aiFaction}");

            // 1. Process tech research decision
            yield return StartCoroutine(ProcessTechDecision());

            // 2. Process city production decisions
            yield return StartCoroutine(ProcessCityDecisions());

            // 3. Process unit actions (movement and combat)
            yield return StartCoroutine(ProcessUnitActions());

            Debug.Log($"[AI] AI phase complete");
            isProcessing = false;

            // Signal that AI is done
            OnAIPhaseComplete?.Invoke();
        }

        #region Tech Research

        private IEnumerator ProcessTechDecision()
        {
            if (techManager == null) yield break;

            // If already researching something, skip
            if (techManager.currentResearch != null) yield break;

            // Find available techs
            var availableTechs = techManager.availableTechs;
            if (availableTechs == null || availableTechs.Count == 0) yield break;

            // Select best tech based on AI priorities
            TechDefinition bestTech = SelectBestTech(availableTechs);

            if (bestTech != null)
            {
                techManager.SetResearch(bestTech);
                Debug.Log($"[AI] Started researching: {bestTech.displayName}");
            }

            yield return new WaitForSeconds(actionDelay);
        }

        private TechDefinition SelectBestTech(List<TechDefinition> techs)
        {
            TechDefinition best = null;
            float bestScore = float.MinValue;

            foreach (var tech in techs)
            {
                if (tech == null) continue;

                float score = EvaluateTech(tech);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = tech;
                }
            }

            return best;
        }

        private float EvaluateTech(TechDefinition tech)
        {
            float score = 0;

            // Prefer critical path techs
            if (tech.isCriticalPath) score += 50;

            // Prefer era transition techs
            if (tech.allowsEraTransition) score += 30;

            // Prefer techs with unit bonuses (military focus)
            score += tech.hpBonus * 2;
            score += tech.armorBonus * 3;
            score += tech.attackBonus * 4;

            // Prefer techs with yield bonuses
            score += tech.protoculturePerTurn * 2;
            score += tech.productionPerTurn * 1.5f;

            // Prefer cheaper techs (faster completion)
            score += 100f / Mathf.Max(1, tech.costScience);

            // Add some randomness to prevent predictability
            score += Random.Range(0f, 10f);

            return score;
        }

        #endregion

        #region City Management

        private IEnumerator ProcessCityDecisions()
        {
            if (cityManager == null) yield break;

            foreach (var city in cityManager.Cities)
            {
                if (city == null || city.faction != aiFaction) continue;

                // If city has no production, queue something
                if (city.CurrentItem == null)
                {
                    var unitToBuild = SelectUnitToProduce(city);
                    if (unitToBuild != null)
                    {
                        city.EnqueueUnit(unitToBuild);
                        Debug.Log($"[AI] {city.cityName} started building: {unitToBuild.displayName}");
                        yield return new WaitForSeconds(actionDelay);
                    }
                }
            }
        }

        private UnitDefinition SelectUnitToProduce(City city)
        {
            // Get available unit definitions from GameBootstrap or a registry
            // For now, we'll look for Zentradi units via UnitRegistry to find existing unit types
            var existingUnits = UnitRegistry.Instance?.GetUnitsByFaction(aiFaction);
            if (existingUnits == null || existingUnits.Count == 0) return null;

            // Get a reference unit definition to build more of the same type
            foreach (var unit in existingUnits)
            {
                if (unit?.definition != null && !unit.definition.canFoundCity)
                {
                    return unit.definition;
                }
            }

            return null;
        }

        #endregion

        #region Unit Actions

        private IEnumerator ProcessUnitActions()
        {
            if (UnitRegistry.Instance == null) yield break;

            var aiUnits = new List<Unit>(UnitRegistry.Instance.GetUnitsByFaction(aiFaction));

            foreach (var unit in aiUnits)
            {
                if (unit == null) continue;

                // Process this unit's actions
                yield return StartCoroutine(ProcessSingleUnit(unit));
            }
        }

        private IEnumerator ProcessSingleUnit(Unit unit)
        {
            // Skip settlers for now (city founding AI not implemented)
            if (unit.definition.canFoundCity)
            {
                yield break;
            }

            // 1. Try to attack if enemies in range
            bool attacked = TryAttackEnemy(unit);
            if (attacked)
            {
                yield return new WaitForSeconds(actionDelay);
            }

            // 2. If unit still has moves, try to move toward enemies or objectives
            while (unit != null && unit.movesLeft > 0)
            {
                bool moved = TryMoveTowardObjective(unit);
                if (!moved) break;

                yield return new WaitForSeconds(actionDelay);

                // After moving, try to attack again if possible
                if (unit != null && TryAttackEnemy(unit))
                {
                    yield return new WaitForSeconds(actionDelay);
                }
            }
        }

        private bool TryAttackEnemy(Unit unit)
        {
            if (unit == null || unit.definition.weapons == null || unit.definition.weapons.Length == 0)
                return false;

            // Find best target in range
            var target = FindBestTarget(unit);
            if (target == null) return false;

            // Execute attack
            var result = CombatResolver.ResolveAttack(unit, target);
            if (result.Succeeded)
            {
                Debug.Log($"[AI] {unit.definition.displayName} attacked {target.definition.displayName} for {result.TotalDamage} damage");
                return true;
            }

            return false;
        }

        private Unit FindBestTarget(Unit attacker)
        {
            if (UnitRegistry.Instance == null) return null;

            var enemies = UnitRegistry.Instance.GetEnemyUnits(attacker.definition.faction);
            Unit bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!CombatResolver.CanAttack(attacker, enemy)) continue;

                float score = EvaluateTarget(attacker, enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private float EvaluateTarget(Unit attacker, Unit target)
        {
            float score = 0;

            // Prefer low HP targets (easy kills)
            score += 100f / Mathf.Max(1, target.currentHP);

            // Prefer high-value targets (units with weapons)
            if (target.definition.weapons != null && target.definition.weapons.Length > 0)
                score += 20;

            // Prefer settlers (strategic targets)
            if (target.definition.canFoundCity)
                score += 50;

            // Prefer closer targets
            int distance = attacker.coord.Distance(target.coord);
            score += 10f / Mathf.Max(1, distance);

            return score;
        }

        private bool TryMoveTowardObjective(Unit unit)
        {
            if (unit == null || unit.movesLeft <= 0) return false;
            if (grid == null || mapGen == null) return false;

            // Find objective (nearest enemy or enemy city)
            HexCoord? objectivePos = FindNearestObjective(unit);
            if (!objectivePos.HasValue) return false;

            // Use pathfinding to find the best path toward the objective
            var pathResult = Pathfinder.FindPathWithBudget(
                unit.coord, objectivePos.Value, unit.movesLeft, grid, mapGen, unit.definition);

            if (pathResult.Success && pathResult.Path.Count > 1)
            {
                // Move along the calculated path
                if (unit.MoveAlongPath(pathResult.Path, grid.hexSize, mapGen))
                {
                    var destination = pathResult.Path[pathResult.Path.Count - 1];
                    Debug.Log($"[AI] {unit.definition.displayName} moved to {destination} (path length: {pathResult.Path.Count})");
                    return true;
                }
            }

            // Fallback: try simple adjacent movement if pathfinding fails
            HexCoord? bestMove = FindBestMoveToward(unit, objectivePos.Value);
            if (!bestMove.HasValue) return false;

            // Execute move
            unit.MoveTo(bestMove.Value, grid.hexSize);
            Debug.Log($"[AI] {unit.definition.displayName} moved to {bestMove.Value} (fallback)");
            return true;
        }

        private HexCoord? FindNearestObjective(Unit unit)
        {
            if (UnitRegistry.Instance == null) return null;

            // Priority 1: Nearest enemy unit
            var enemies = UnitRegistry.Instance.GetEnemyUnits(unit.definition.faction);
            HexCoord? nearest = null;
            int nearestDist = int.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                int dist = unit.coord.Distance(enemy.coord);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy.coord;
                }
            }

            // Priority 2: Enemy cities (if no enemy units nearby)
            if (nearest == null && cityManager != null)
            {
                var enemyFaction = unit.definition.faction == Faction.RDF ? Faction.Zentradi : Faction.RDF;
                foreach (var city in cityManager.Cities)
                {
                    if (city != null && city.faction == enemyFaction)
                    {
                        int dist = unit.coord.Distance(city.coord);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearest = city.coord;
                        }
                    }
                }
            }

            return nearest;
        }

        private HexCoord? FindBestMoveToward(Unit unit, HexCoord objective)
        {
            var neighbors = grid.Neighbors(unit.coord);
            HexCoord? best = null;
            int bestDist = unit.coord.Distance(objective);

            foreach (var neighbor in neighbors)
            {
                // Check if passable
                if (!IsPassable(unit, neighbor)) continue;

                // Check if occupied
                if (UnitRegistry.Instance != null && UnitRegistry.Instance.IsOccupied(neighbor)) continue;

                // Check if this moves us closer
                int dist = neighbor.Distance(objective);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = neighbor;
                }
            }

            return best;
        }

        private bool IsPassable(Unit unit, HexCoord coord)
        {
            if (mapGen == null) return false;

            var terrain = mapGen.GetTerrain(coord);
            return MapRules.IsPassable(unit.definition, terrain);
        }

        #endregion
    }
}
