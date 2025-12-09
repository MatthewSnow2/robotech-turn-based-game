using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Data;
using Robotech.TBS.Map;
using Robotech.TBS.Units;
using Robotech.TBS.Systems;

namespace Robotech.TBS.Hex
{
    /// <summary>
    /// Result of a pathfinding operation.
    /// </summary>
    public class PathResult
    {
        /// <summary>
        /// Whether a valid path was found.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The path from start to goal (inclusive of both endpoints).
        /// Empty if no path found.
        /// </summary>
        public List<HexCoord> Path { get; set; } = new List<HexCoord>();

        /// <summary>
        /// Total movement cost of the path.
        /// </summary>
        public int TotalCost { get; set; }

        /// <summary>
        /// The furthest point reachable within the given movement budget.
        /// Useful when goal is unreachable in one turn.
        /// </summary>
        public HexCoord? FurthestReachable { get; set; }

        public static PathResult Failed() => new PathResult { Success = false };
    }

    /// <summary>
    /// Provides pathfinding capabilities using A* algorithm with terrain costs.
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// Find a path from start to goal using A* algorithm.
        /// </summary>
        /// <param name="start">Starting hex coordinate</param>
        /// <param name="goal">Target hex coordinate</param>
        /// <param name="grid">The hex grid for bounds checking</param>
        /// <param name="mapGen">Map generator for terrain data</param>
        /// <param name="unitDef">Unit definition for passability rules</param>
        /// <param name="maxCost">Maximum movement cost (use unit's movement points)</param>
        /// <returns>PathResult containing the path if found</returns>
        public static PathResult FindPath(
            HexCoord start,
            HexCoord goal,
            HexGrid grid,
            MapGenerator mapGen,
            UnitDefinition unitDef,
            int maxCost = int.MaxValue)
        {
            if (grid == null || mapGen == null || unitDef == null)
                return PathResult.Failed();

            if (!grid.InBounds(start) || !grid.InBounds(goal))
                return PathResult.Failed();

            // Check if goal is passable
            var goalTerrain = mapGen.GetTerrain(goal);
            if (!MapRules.IsPassable(unitDef, goalTerrain))
                return PathResult.Failed();

            // Check if goal is occupied by another unit
            if (UnitRegistry.Instance != null && UnitRegistry.Instance.IsOccupied(goal))
                return PathResult.Failed();

            // A* implementation
            var openSet = new PriorityQueue<HexCoord>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var gScore = new Dictionary<HexCoord, int> { [start] = 0 };
            var fScore = new Dictionary<HexCoord, int> { [start] = Heuristic(start, goal) };

            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current.q == goal.q && current.r == goal.r)
                {
                    // Reconstruct path
                    var path = ReconstructPath(cameFrom, current);
                    int totalCost = gScore[current];

                    return new PathResult
                    {
                        Success = true,
                        Path = path,
                        TotalCost = totalCost
                    };
                }

                foreach (var neighborDir in HexCoord.Neighbors)
                {
                    var neighbor = current + neighborDir;

                    if (!grid.InBounds(neighbor)) continue;

                    var terrain = mapGen.GetTerrain(neighbor);
                    if (!MapRules.IsPassable(unitDef, terrain)) continue;

                    // Check if occupied (except goal which we already validated)
                    if (!(neighbor.q == goal.q && neighbor.r == goal.r))
                    {
                        if (UnitRegistry.Instance != null && UnitRegistry.Instance.IsOccupied(neighbor))
                            continue;
                    }

                    int moveCost = GetMovementCost(terrain);
                    int tentativeG = gScore[current] + moveCost;

                    // Skip if exceeds max cost
                    if (tentativeG > maxCost) continue;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }
            }

            return PathResult.Failed();
        }

        /// <summary>
        /// Find all hexes reachable within a movement budget.
        /// </summary>
        /// <param name="start">Starting hex coordinate</param>
        /// <param name="movementBudget">Maximum movement points to spend</param>
        /// <param name="grid">The hex grid for bounds checking</param>
        /// <param name="mapGen">Map generator for terrain data</param>
        /// <param name="unitDef">Unit definition for passability rules</param>
        /// <returns>Dictionary of reachable hexes with their costs</returns>
        public static Dictionary<HexCoord, int> GetReachableHexes(
            HexCoord start,
            int movementBudget,
            HexGrid grid,
            MapGenerator mapGen,
            UnitDefinition unitDef)
        {
            var reachable = new Dictionary<HexCoord, int>();
            if (grid == null || mapGen == null || unitDef == null)
                return reachable;

            if (!grid.InBounds(start))
                return reachable;

            // BFS with cost tracking
            var frontier = new PriorityQueue<HexCoord>();
            var costSoFar = new Dictionary<HexCoord, int> { [start] = 0 };

            frontier.Enqueue(start, 0);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int currentCost = costSoFar[current];

                foreach (var neighborDir in HexCoord.Neighbors)
                {
                    var neighbor = current + neighborDir;

                    if (!grid.InBounds(neighbor)) continue;

                    var terrain = mapGen.GetTerrain(neighbor);
                    if (!MapRules.IsPassable(unitDef, terrain)) continue;

                    // Check if occupied
                    if (UnitRegistry.Instance != null && UnitRegistry.Instance.IsOccupied(neighbor))
                        continue;

                    int moveCost = GetMovementCost(terrain);
                    int newCost = currentCost + moveCost;

                    if (newCost > movementBudget) continue;

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        frontier.Enqueue(neighbor, newCost);
                        reachable[neighbor] = newCost;
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// Get the path to move as far as possible toward a goal within a movement budget.
        /// </summary>
        public static PathResult FindPathWithBudget(
            HexCoord start,
            HexCoord goal,
            int movementBudget,
            HexGrid grid,
            MapGenerator mapGen,
            UnitDefinition unitDef)
        {
            // First try to find full path
            var fullPath = FindPath(start, goal, grid, mapGen, unitDef, int.MaxValue);

            if (!fullPath.Success)
                return PathResult.Failed();

            // If we can reach the goal within budget, return full path
            if (fullPath.TotalCost <= movementBudget)
                return fullPath;

            // Otherwise, find how far we can go along the path
            int costSoFar = 0;
            var partialPath = new List<HexCoord>();
            HexCoord? furthest = null;

            for (int i = 0; i < fullPath.Path.Count; i++)
            {
                var current = fullPath.Path[i];

                if (i == 0)
                {
                    partialPath.Add(current);
                    furthest = current;
                    continue;
                }

                var terrain = mapGen.GetTerrain(current);
                int moveCost = GetMovementCost(terrain);
                int newCost = costSoFar + moveCost;

                if (newCost > movementBudget)
                    break;

                costSoFar = newCost;
                partialPath.Add(current);
                furthest = current;
            }

            return new PathResult
            {
                Success = partialPath.Count > 1,
                Path = partialPath,
                TotalCost = costSoFar,
                FurthestReachable = furthest
            };
        }

        /// <summary>
        /// Get movement cost for a terrain type.
        /// </summary>
        public static int GetMovementCost(TerrainType terrain)
        {
            if (terrain == null) return 1;
            return Mathf.Max(1, terrain.movementCost);
        }

        /// <summary>
        /// Heuristic function for A* (hex distance).
        /// </summary>
        private static int Heuristic(HexCoord a, HexCoord b)
        {
            return a.Distance(b);
        }

        /// <summary>
        /// Reconstruct path from A* result.
        /// </summary>
        private static List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            var path = new List<HexCoord> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }
    }

    /// <summary>
    /// Simple priority queue implementation for pathfinding.
    /// </summary>
    public class PriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new List<(T, int)>();
        private HashSet<T> itemSet = new HashSet<T>();

        public int Count => elements.Count;

        public void Enqueue(T item, int priority)
        {
            elements.Add((item, priority));
            itemSet.Add(item);
            // Bubble up
            int i = elements.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (elements[parent].priority <= elements[i].priority) break;
                var temp = elements[parent];
                elements[parent] = elements[i];
                elements[i] = temp;
                i = parent;
            }
        }

        public T Dequeue()
        {
            var result = elements[0].item;
            itemSet.Remove(result);

            int lastIndex = elements.Count - 1;
            elements[0] = elements[lastIndex];
            elements.RemoveAt(lastIndex);

            if (elements.Count > 0)
            {
                // Bubble down
                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;
                    int smallest = i;

                    if (left < elements.Count && elements[left].priority < elements[smallest].priority)
                        smallest = left;
                    if (right < elements.Count && elements[right].priority < elements[smallest].priority)
                        smallest = right;

                    if (smallest == i) break;

                    var temp = elements[i];
                    elements[i] = elements[smallest];
                    elements[smallest] = temp;
                    i = smallest;
                }
            }

            return result;
        }

        public bool Contains(T item) => itemSet.Contains(item);
    }
}
