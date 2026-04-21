using System;
using Robotech.TBS.Hex;
using Robotech.TBS.Data;
using Robotech.TBS.Map;

namespace Robotech.TBS.Combat
{
    /// <summary>
    /// Line-of-sight checks for ranged attacks and visibility.
    /// Walks the hex line between two coords; intermediate hexes (excluding endpoints)
    /// block sight if their terrain has providesElevation=true (Hills, Mountains).
    /// Adjacent or same-hex queries are always true (no intermediates to inspect).
    /// </summary>
    public static class LineOfSight
    {
        // Convenience overload: walks terrain via MapGenerator.
        public static bool HasLineOfSight(HexCoord from, HexCoord to, MapGenerator mapGen)
        {
            if (mapGen == null) return true;
            return HasLineOfSight(from, to, c =>
            {
                var t = mapGen.GetTerrain(c);
                return t != null && t.providesElevation;
            });
        }

        // Core overload: predicate lets EditMode tests inject a blocker set without Unity scene wiring.
        public static bool HasLineOfSight(HexCoord from, HexCoord to, Func<HexCoord, bool> blocksSight)
        {
            if (blocksSight == null) return true;
            int distance = from.Distance(to);
            if (distance <= 1) return true;

            var line = HexMath.LineBetween(from, to);
            // Skip endpoints: index 0 (attacker) and index distance (target).
            for (int i = 1; i < line.Count - 1; i++)
            {
                if (blocksSight(line[i])) return false;
            }
            return true;
        }
    }
}
