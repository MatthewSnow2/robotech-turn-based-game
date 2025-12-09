using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Hex;
using Robotech.TBS.Map;
using Robotech.TBS.Systems;
using Robotech.TBS.Inputs;

namespace Robotech.TBS.Rendering
{
    // Draws flat-colored hex tiles using Gizmos for quick prototype visualization
    public class HexDebugRenderer : MonoBehaviour
    {
        public HexGrid grid;
        public MapGenerator mapGen;
        public CityManager cityManager;

        [Header("Movement Visualization")]
        public Color reachableColor = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        public Color pathColor = new Color(1f, 1f, 0f, 0.6f);
        public Color attackableColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

        private SelectionController selectionController;

        private void OnDrawGizmos()
        {
            if (grid == null || mapGen == null) return;
            if (mapGen.Terrain == null) return;
            if (cityManager == null) cityManager = FindObjectOfType<CityManager>();
            if (selectionController == null) selectionController = FindObjectOfType<SelectionController>();

            foreach (var kv in mapGen.Terrain)
            {
                var c = kv.Key;
                var t = kv.Value;
                var center = grid.CoordToWorld(c);
                Gizmos.color = TerrainColor(t);
                DrawHex(center, grid.hexSize * 0.98f);

                // Ownership overlay and borders
                if (cityManager != null)
                {
                    var owner = cityManager.GetOwner(c);
                    if (owner != null)
                    {
                        Gizmos.color = OwnerColor(owner) * new Color(1,1,1,0.9f);
                        DrawHex(center, grid.hexSize * 0.85f);

                        // Border edges where neighbor owner differs
                        var verts = GetHexVerts(center, grid.hexSize * 1.0f);
                        for (int i = 0; i < 6; i++)
                        {
                            var dir = i; // neighbor index corresponds to edge between i and i+1
                            var n = grid.Neighbors(c)[dir];
                            var nOwner = grid.InBounds(n) ? cityManager.GetOwner(n) : null;
                            if (nOwner != owner)
                            {
                                Gizmos.color = new Color(1f, 1f, 0.2f, 1f);
                                Gizmos.DrawLine(verts[i], verts[(i + 1) % 6]);
                            }
                        }
                    }
                }
            }

            // Draw movement visualization overlays
            DrawMovementVisualization();
        }

        private void DrawMovementVisualization()
        {
            if (selectionController == null) return;
            if (selectionController.SelectedUnit == null) return;

            // Draw reachable hexes
            foreach (var hex in selectionController.ReachableHexes)
            {
                var center = grid.CoordToWorld(hex);
                Gizmos.color = reachableColor;
                DrawFilledHex(center, grid.hexSize * 0.9f);
            }

            // Draw attackable hexes
            foreach (var hex in selectionController.AttackableHexes)
            {
                var center = grid.CoordToWorld(hex);
                Gizmos.color = attackableColor;
                DrawFilledHex(center, grid.hexSize * 0.85f);
            }

            // Draw path preview
            if (selectionController.CurrentPath != null && selectionController.CurrentPath.Count > 1)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < selectionController.CurrentPath.Count - 1; i++)
                {
                    var from = grid.CoordToWorld(selectionController.CurrentPath[i]) + Vector3.up * 0.1f;
                    var to = grid.CoordToWorld(selectionController.CurrentPath[i + 1]) + Vector3.up * 0.1f;
                    Gizmos.DrawLine(from, to);
                }

                // Draw filled hex at destination
                var destCenter = grid.CoordToWorld(selectionController.CurrentPath[selectionController.CurrentPath.Count - 1]);
                DrawFilledHex(destCenter, grid.hexSize * 0.8f);
            }

            // Draw hover hex outline
            if (grid.InBounds(selectionController.HoverHex))
            {
                var hoverCenter = grid.CoordToWorld(selectionController.HoverHex);
                Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
                DrawHex(hoverCenter, grid.hexSize * 1.02f);
            }
        }

        private void DrawFilledHex(Vector3 center, float size)
        {
            var verts = GetHexVerts(center, size);
            // Draw as a wireframe with thicker lines (Gizmos doesn't support filled shapes natively)
            for (int i = 0; i < 6; i++)
            {
                Gizmos.DrawLine(verts[i], verts[(i + 1) % 6]);
                // Draw inner lines for visual fill effect
                Gizmos.DrawLine(center, verts[i]);
            }
        }

        private Color TerrainColor(Robotech.TBS.Data.TerrainType t)
        {
            if (t.isWater) return new Color(0.2f,0.4f,0.8f,0.5f);
            if (t.isUrban) return new Color(0.5f,0.5f,0.5f,0.6f);
            if (t.providesElevation) return new Color(0.4f,0.35f,0.25f,0.6f);
            if (t.displayName.ToLower().Contains("forest")) return new Color(0.1f,0.5f,0.2f,0.6f);
            if (t.displayName.ToLower().Contains("desert")) return new Color(0.8f,0.7f,0.3f,0.6f);
            if (t.displayName.ToLower().Contains("tundra")) return new Color(0.8f,0.9f,1f,0.6f);
            if (t.displayName.ToLower().Contains("marsh")) return new Color(0.2f,0.5f,0.3f,0.6f);
            return new Color(0.3f,0.7f,0.3f,0.6f);
        }

        private Color OwnerColor(Robotech.TBS.Cities.City city)
        {
            switch (city.faction)
            {
                case Robotech.TBS.Data.Faction.Zentradi:
                    return new Color(0.85f, 0.25f, 0.25f, 0.8f);
                case Robotech.TBS.Data.Faction.RDF:
                default:
                    return new Color(0.25f, 0.6f, 1f, 0.8f);
            }
        }

        private void DrawHex(Vector3 center, float size)
        {
            var verts = GetHexVerts(center, size);
            for (int i=0;i<6;i++)
            {
                Gizmos.DrawLine(verts[i], verts[(i+1)%6]);
            }
        }

        private Vector3[] GetHexVerts(Vector3 center, float size)
        {
            Vector3[] verts = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60 * i - 30); // pointy-top
                verts[i] = center + new Vector3(size * Mathf.Cos(angle), 0f, size * Mathf.Sin(angle));
            }
            return verts;
        }
    }
}
