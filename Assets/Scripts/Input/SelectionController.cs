using System;
using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Hex;
using Robotech.TBS.Units;
using Robotech.TBS.Map;
using Robotech.TBS.Systems;
using Robotech.TBS.Data;
using Robotech.TBS.Combat;
using Robotech.TBS.Core;

namespace Robotech.TBS.Inputs
{
    public class SelectionController : MonoBehaviour
    {
        public HexGrid grid;
        public MapGenerator mapGen;
        public CityManager cityManager;
        private TurnManager turnManager;

        public Unit SelectedUnit { get; private set; }
        public HexCoord HoverHex { get; private set; }
        public HashSet<HexCoord> ReachableHexes { get; private set; } = new();
        public Dictionary<HexCoord, int> ReachableHexCosts { get; private set; } = new();
        public HashSet<HexCoord> AttackableHexes { get; private set; } = new();
        public bool AttackMode { get; private set; } = false;

        /// <summary>
        /// The currently calculated path to the hover hex (for preview visualization).
        /// </summary>
        public List<HexCoord> CurrentPath { get; private set; } = new();

        public static event Action<Unit> OnUnitSelected;
        public static event Action OnSelectionCleared;

        Camera cam;
        Plane ground = new Plane(Vector3.up, Vector3.zero);

        void Awake()
        {
            cam = Camera.main;
            if (grid == null) grid = FindObjectOfType<HexGrid>();
            if (mapGen == null) mapGen = FindObjectOfType<MapGenerator>();
            if (cityManager == null) cityManager = FindObjectOfType<CityManager>();
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        }

        void Update()
        {
            UpdateHover();

            // Block all commands during AI phase
            if (turnManager != null && turnManager.CurrentPhase != TurnManager.TurnPhase.Player)
                return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                ClearSelection();
            }
            // Hotkey: Found City (B)
            if (UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                TryFoundCity();
            }
            // Hotkey: Overwatch (O)
            if (UnityEngine.Input.GetKeyDown(KeyCode.O))
            {
                TrySetOverwatch();
            }
        }

        void TrySetOverwatch()
        {
            if (SelectedUnit == null) return;
            if (SelectedUnit.definition == null || !SelectedUnit.definition.canOverwatch) return;
            // Player can only overwatch their own units (faction guard).
            if (SelectedUnit.definition.faction != Faction.RDF) return;
            if (SelectedUnit.SetOverwatch())
            {
                // Refresh derived selection state — no more reachable hexes for this unit this turn.
                ReachableHexes.Clear();
                CurrentPath.Clear();
            }
        }

        void UpdateHover()
        {
            var ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            float enter;
            if (ground.Raycast(ray, out enter))
            {
                var hit = ray.origin + ray.direction * enter;
                var newHover = HexMath.AxialFromWorld(hit, grid.hexSize);

                // Update path preview when hover hex changes
                if (newHover.q != HoverHex.q || newHover.r != HoverHex.r)
                {
                    HoverHex = newHover;
                    UpdatePathPreview();
                }
            }
        }

        void UpdatePathPreview()
        {
            CurrentPath.Clear();

            if (SelectedUnit == null || AttackMode) return;
            if (!ReachableHexes.Contains(HoverHex)) return;

            // Calculate path to hover hex for visualization
            var pathResult = Pathfinder.FindPathWithBudget(
                SelectedUnit.coord, HoverHex, SelectedUnit.movesLeft, grid, mapGen, SelectedUnit.definition);

            if (pathResult.Success)
            {
                CurrentPath = pathResult.Path;
            }
        }

        void HandleLeftClick()
        {
            // Try raycast against colliders to select a unit first
            var ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                if (unit != null)
                {
                    SelectUnit(unit);
                    return;
                }
            }

            if (SelectedUnit != null)
            {
                if (AttackMode)
                {
                    // Attack if target hex has enemy and is attackable
                    if (AttackableHexes.Contains(HoverHex))
                    {
                        var target = FindUnitAt(HoverHex);
                        if (target != null && target.definition.faction != SelectedUnit.definition.faction)
                        {
                            CombatResolver.ResolveAttack(SelectedUnit, target, mapGen);
                            // After attack, recompute ranges
                            RecomputeRanges();
                        }
                    }
                }
                else
                {
                    // Move if reachable using pathfinding
                    if (ReachableHexes.Contains(HoverHex))
                    {
                        // Use pathfinding for multi-hex movement
                        var pathResult = SelectedUnit.MoveToTarget(HoverHex, grid.hexSize, grid, mapGen);
                        if (pathResult.Success)
                        {
                            RecomputeRanges();
                        }
                    }
                }
            }
        }

        bool IsPassable(UnitDefinition def, HexCoord c)
        {
            var t = mapGen.GetTerrain(c);
            return MapRules.IsPassable(def, t);
        }

        public void SelectUnit(Unit u)
        {
            // Only allow selecting player-faction (RDF) units
            if (u != null && u.definition != null && u.definition.faction != Faction.RDF)
                return;

            SelectedUnit = u;
            AttackMode = false;
            RecomputeRanges();
            OnUnitSelected?.Invoke(u);
        }

        public void ClearSelection()
        {
            SelectedUnit = null;
            ReachableHexes.Clear();
            AttackableHexes.Clear();
            AttackMode = false;
            OnSelectionCleared?.Invoke();
        }

        public void RecomputeRanges()
        {
            ReachableHexes.Clear();
            ReachableHexCosts.Clear();
            AttackableHexes.Clear();
            CurrentPath.Clear();
            if (SelectedUnit == null) return;

            // Use pathfinding to compute all reachable hexes with terrain costs
            ReachableHexCosts = SelectedUnit.GetReachableHexes(grid, mapGen);
            foreach (var kvp in ReachableHexCosts)
            {
                ReachableHexes.Add(kvp.Key);
            }

            // Attack range based on weapon ranges - use UnitRegistry and CombatResolver
            if (UnitRegistry.Instance != null)
            {
                var enemies = UnitRegistry.Instance.GetEnemyUnits(SelectedUnit.definition.faction);
                foreach (var enemy in enemies)
                {
                    // Use CombatResolver.CanAttack for proper range validation (includes LoS)
                    if (CombatResolver.CanAttack(SelectedUnit, enemy, mapGen))
                    {
                        AttackableHexes.Add(enemy.coord);
                    }
                }
            }
        }

        public void SetAttackMode(bool enabled)
        {
            AttackMode = enabled;
        }

        private Unit FindUnitAt(HexCoord c)
        {
            // Use UnitRegistry for O(1) lookup
            if (UnitRegistry.Instance != null)
            {
                return UnitRegistry.Instance.GetUnitAt(c);
            }
            return null;
        }

        public bool CanFoundCityHere()
        {
            if (SelectedUnit == null || !SelectedUnit.definition.canFoundCity) return false;
            if (!grid.InBounds(SelectedUnit.coord)) return false;
            if (cityManager == null) return false;
            if (cityManager.IsOwned(SelectedUnit.coord)) return false;
            if (cityManager.IsTooCloseToAnyCity(SelectedUnit.coord, 3)) return false;
            var t = mapGen.GetTerrain(SelectedUnit.coord);
            // disallow on water/impassable
            if (t.isWater || t.isImpassable) return false;
            return true;
        }

        public bool TryFoundCity()
        {
            if (!CanFoundCityHere()) return false;
            var count = cityManager.Cities.Count + 1;
            var city = cityManager.FoundCity($"City {count}", SelectedUnit.coord, grid.hexSize, SelectedUnit.definition.faction);
            if (city != null)
            {
                Destroy(SelectedUnit.gameObject);
                ClearSelection();
                return true;
            }
            return false;
        }
    }
}
