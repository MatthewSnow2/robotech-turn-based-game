using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Units;
using Robotech.TBS.Hex;
using Robotech.TBS.Data;

namespace Robotech.TBS.Systems
{
    /// <summary>
    /// Singleton registry for all units in the game. Provides O(1) lookups by position
    /// and efficient queries by faction. Replaces expensive FindObjectsOfType calls.
    /// </summary>
    public class UnitRegistry : MonoBehaviour
    {
        public static UnitRegistry Instance { get; private set; }

        /// <summary>
        /// Fired when a unit is registered (spawned).
        /// </summary>
        public static event System.Action<Unit> OnUnitRegistered;

        /// <summary>
        /// Fired when a unit is unregistered (destroyed).
        /// </summary>
        public static event System.Action<Unit> OnUnitUnregistered;

        // Position-based lookup for O(1) access
        private Dictionary<HexCoord, Unit> unitsByPosition = new();

        // Faction-based lookup for efficient faction queries
        private Dictionary<Faction, HashSet<Unit>> unitsByFaction = new();

        // All units for iteration
        private HashSet<Unit> allUnits = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize faction dictionaries
            unitsByFaction[Faction.RDF] = new HashSet<Unit>();
            unitsByFaction[Faction.Zentradi] = new HashSet<Unit>();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Register a unit with the registry. Called when a unit is spawned.
        /// </summary>
        public void Register(Unit unit)
        {
            if (unit == null) return;

            allUnits.Add(unit);
            unitsByPosition[unit.coord] = unit;

            if (unitsByFaction.TryGetValue(unit.definition.faction, out var factionSet))
            {
                factionSet.Add(unit);
            }

            OnUnitRegistered?.Invoke(unit);
        }

        /// <summary>
        /// Unregister a unit from the registry. Called when a unit is destroyed.
        /// </summary>
        public void Unregister(Unit unit)
        {
            if (unit == null) return;

            allUnits.Remove(unit);

            // Remove from position lookup
            if (unitsByPosition.TryGetValue(unit.coord, out var existing) && existing == unit)
            {
                unitsByPosition.Remove(unit.coord);
            }

            // Remove from faction lookup
            if (unitsByFaction.TryGetValue(unit.definition.faction, out var factionSet))
            {
                factionSet.Remove(unit);
            }

            OnUnitUnregistered?.Invoke(unit);
        }

        /// <summary>
        /// Update a unit's position in the registry. Must be called when a unit moves.
        /// </summary>
        public void UpdatePosition(Unit unit, HexCoord oldCoord, HexCoord newCoord)
        {
            if (unit == null) return;

            // Remove from old position
            if (unitsByPosition.TryGetValue(oldCoord, out var existing) && existing == unit)
            {
                unitsByPosition.Remove(oldCoord);
            }

            // Add to new position
            unitsByPosition[newCoord] = unit;
        }

        /// <summary>
        /// Get the unit at a specific position. Returns null if no unit is there.
        /// O(1) lookup time.
        /// </summary>
        public Unit GetUnitAt(HexCoord coord)
        {
            unitsByPosition.TryGetValue(coord, out var unit);
            return unit;
        }

        /// <summary>
        /// Check if a hex is occupied by any unit.
        /// O(1) lookup time.
        /// </summary>
        public bool IsOccupied(HexCoord coord)
        {
            return unitsByPosition.ContainsKey(coord);
        }

        /// <summary>
        /// Get all units. Use sparingly - prefer faction-specific queries when possible.
        /// </summary>
        public IReadOnlyCollection<Unit> GetAllUnits()
        {
            return allUnits;
        }

        /// <summary>
        /// Get all units belonging to a specific faction.
        /// </summary>
        public IReadOnlyCollection<Unit> GetUnitsByFaction(Faction faction)
        {
            if (unitsByFaction.TryGetValue(faction, out var factionSet))
            {
                return factionSet;
            }
            return System.Array.Empty<Unit>();
        }

        /// <summary>
        /// Get all units of the enemy faction.
        /// </summary>
        public IReadOnlyCollection<Unit> GetEnemyUnits(Faction myFaction)
        {
            var enemyFaction = myFaction == Faction.RDF ? Faction.Zentradi : Faction.RDF;
            return GetUnitsByFaction(enemyFaction);
        }

        /// <summary>
        /// Get all units within a specified range of a position.
        /// </summary>
        public List<Unit> GetUnitsInRange(HexCoord center, int range)
        {
            var results = new List<Unit>();
            foreach (var unit in allUnits)
            {
                if (center.Distance(unit.coord) <= range)
                {
                    results.Add(unit);
                }
            }
            return results;
        }

        /// <summary>
        /// Get enemy units within attack range of a position.
        /// </summary>
        public List<Unit> GetEnemiesInRange(HexCoord center, int range, Faction myFaction)
        {
            var results = new List<Unit>();
            var enemyFaction = myFaction == Faction.RDF ? Faction.Zentradi : Faction.RDF;

            if (unitsByFaction.TryGetValue(enemyFaction, out var enemies))
            {
                foreach (var enemy in enemies)
                {
                    if (center.Distance(enemy.coord) <= range)
                    {
                        results.Add(enemy);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Get the total count of all units.
        /// </summary>
        public int Count => allUnits.Count;

        /// <summary>
        /// Get the count of units for a specific faction.
        /// </summary>
        public int GetFactionCount(Faction faction)
        {
            if (unitsByFaction.TryGetValue(faction, out var factionSet))
            {
                return factionSet.Count;
            }
            return 0;
        }

        /// <summary>
        /// Clear all units from the registry. Used when resetting the game.
        /// </summary>
        public void Clear()
        {
            allUnits.Clear();
            unitsByPosition.Clear();
            foreach (var factionSet in unitsByFaction.Values)
            {
                factionSet.Clear();
            }
        }
    }
}
