using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Data;
using Robotech.TBS.Hex;
using Robotech.TBS.Map;
using Robotech.TBS.Units;
using Robotech.TBS.Systems;
using Robotech.TBS.Bootstrap;

namespace Robotech.TBS.Cities
{
    public class City : MonoBehaviour
    {
        public string cityName = "City";
        public HexCoord coord;
        public int population = 1;
        public List<DistrictDefinition> districts = new();
        public Faction faction;
        public float hexSize;

        public class ProductionItem
        {
            public UnitDefinition unit;
            public int progress;
            public int Cost => unit != null ? unit.buildCostMaterials : 0; // using materials as production points cost
        }

        public readonly Queue<ProductionItem> productionQueue = new();
        public ProductionItem CurrentItem => productionQueue.Count > 0 ? productionQueue.Peek() : null;

        public void Init(string cityLabel, HexCoord c, float hexSize, Faction faction)
        {
            cityName = cityLabel;
            coord = c;
            this.faction = faction;
            this.hexSize = hexSize;
            transform.position = coord.ToWorld(this.hexSize);
            gameObject.name = $"City_{cityName}_{coord.q}_{coord.r}";
        }

        public (int prod,int sci,int infl) GetYields()
        {
            int prod=0, sci=0, infl=0;
            foreach (var d in districts)
            {
                if (d == null) continue;
                prod += d.bonusProduction;
                sci += d.bonusScience;
                infl += d.bonusInfluence;
            }
            return (prod, sci, infl);
        }

        public int GetDefenseBonus()
        {
            int sum = 0; foreach (var d in districts) if (d != null) sum += d.defenseBonus; return sum;
        }

        public int GetVisionBonus()
        {
            int sum = 0; foreach (var d in districts) if (d != null) sum += d.visionBonus; return sum;
        }

        public void EnqueueUnit(UnitDefinition def)
        {
            if (def == null) return;
            productionQueue.Enqueue(new ProductionItem { unit = def, progress = 0 });
        }

        public void AdvanceProduction(int productionPoints, HexGrid grid, MapGenerator mapGen)
        {
            if (productionPoints <= 0) return;
            var item = CurrentItem;
            if (item == null) return;
            item.progress += productionPoints;
            if (item.progress >= item.Cost)
            {
                // Try to spawn the unit
                var spawn = FindSpawnHex(grid, mapGen, coord, item.unit);
                if (spawn != null)
                {
                    UnitFactory.SpawnUnit(faction == Faction.RDF ? "RDF" : "ZENT", item.unit, spawn.Value, grid.hexSize);
                }
                productionQueue.Dequeue();
            }
        }

        HexCoord? FindSpawnHex(HexGrid grid, MapGenerator mapGen, HexCoord center, UnitDefinition def)
        {
            if (IsPassable(mapGen, def, center) && IsTileFree(center)) return center;
            foreach (var n in grid.Neighbors(center))
            {
                if (IsPassable(mapGen, def, n) && IsTileFree(n)) return n;
            }
            return null;
        }

        bool IsPassable(MapGenerator mapGen, UnitDefinition def, HexCoord c)
        {
            var t = mapGen.GetTerrain(c);
            return MapRules.IsPassable(def, t);
        }

        bool IsTileFree(HexCoord c)
        {
            // Use UnitRegistry for O(1) lookup
            if (UnitRegistry.Instance != null)
            {
                return !UnitRegistry.Instance.IsOccupied(c);
            }
            return true;
        }
    }
}
