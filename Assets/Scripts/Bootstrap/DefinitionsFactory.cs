using System.Collections.Generic;
using UnityEngine;
using Robotech.TBS.Data;

namespace Robotech.TBS.Bootstrap
{
    public static class DefinitionsFactory
    {
        public static TerrainType CreateTerrain(string id, string name, int move, int def, bool water=false, bool impass=false, bool urban=false, bool elev=false)
        {
            var t = ScriptableObject.CreateInstance<TerrainType>();
            t.terrainId = id; t.displayName = name;
            t.movementCost = move; t.defenseBonus = def;
            t.isWater = water; t.isImpassable = impass; t.isUrban = urban; t.providesElevation = elev;
            return t;
        }

        public static WeaponDefinition CreateWeapon(string id, string name, string wclass, int dmg, int salvo, int rmin, int rmax, float acc, bool aa=false, bool siege=false)
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.weaponId = id; w.displayName = name; w.weaponClass = wclass;
            w.damage = dmg; w.salvoCount = salvo; w.rangeMin = rmin; w.rangeMax = rmax; w.accuracyBase = acc;
            w.antiAir = aa; w.siege = siege;
            return w;
        }

        public static UnitDefinition CreateUnit(string id, string name, Faction faction, UnitLayer layer, int hp, int armor, int move, int vision, WeaponDefinition[] weapons, bool canTransform=false, bool ecm=false, bool jj=false, bool canFoundCity=false)
        {
            var u = ScriptableObject.CreateInstance<UnitDefinition>();
            u.unitId = id; u.displayName = name; u.faction = faction; u.layer = layer;
            u.maxHP = hp; u.armor = armor; u.movement = move; u.vision = vision;
            u.weapons = weapons; u.canTransform = canTransform; u.hasECM = ecm; u.hasJumpJets = jj; u.canFoundCity = canFoundCity;
            return u;
        }

        public static DistrictDefinition CreateDistrict(
            string id,
            string name,
            DistrictType type,
            int production = 0,
            int science = 0,
            int influence = 0,
            int defense = 0,
            int vision = 0,
            int upkeep = 0,
            string description = "")
        {
            var d = ScriptableObject.CreateInstance<DistrictDefinition>();
            d.districtId = id;
            d.displayName = name;
            d.type = type;
            d.bonusProduction = production;
            d.bonusScience = science;
            d.bonusInfluence = influence;
            d.defenseBonus = defense;
            d.visionBonus = vision;
            d.protocultureUpkeep = upkeep;
            d.description = description;
            return d;
        }

        public static AbilityDefinition CreateAbility(
            string id,
            string name,
            string description = "")
        {
            var a = ScriptableObject.CreateInstance<AbilityDefinition>();
            a.abilityId = id;
            a.displayName = name;
            a.description = description;
            return a;
        }

        public static TechDefinition CreateTech(
            string techId,
            string displayName,
            int costScience,
            TechGeneration generation,
            TechCategory category,
            string description,
            Sprite icon = null,
            bool isCriticalPath = false,
            bool allowsEraTransition = false)
        {
            var tech = ScriptableObject.CreateInstance<TechDefinition>();
            tech.techId = techId;
            tech.displayName = displayName;
            tech.costScience = costScience;
            tech.generation = generation;
            tech.category = category;
            tech.description = description;
            tech.icon = icon;
            tech.isCriticalPath = isCriticalPath;
            tech.allowsEraTransition = allowsEraTransition;

            // Initialize collections
            tech.prerequisites = new List<TechDefinition>();
            tech.unlocksUnits = new List<UnitDefinition>();
            tech.unlocksDistricts = new List<DistrictDefinition>();
            tech.unlocksAbilities = new List<AbilityDefinition>();

            return tech;
        }

        // Helper method to add prerequisites to a tech
        public static TechDefinition WithPrerequisites(this TechDefinition tech, params TechDefinition[] prerequisites)
        {
            if (tech.prerequisites == null)
                tech.prerequisites = new List<TechDefinition>();
            tech.prerequisites.AddRange(prerequisites);
            return tech;
        }

        // Helper method to add unit unlocks
        public static TechDefinition WithUnitUnlocks(this TechDefinition tech, params UnitDefinition[] units)
        {
            if (tech.unlocksUnits == null)
                tech.unlocksUnits = new List<UnitDefinition>();
            tech.unlocksUnits.AddRange(units);
            return tech;
        }

        // Helper method to add district unlocks
        public static TechDefinition WithDistrictUnlocks(this TechDefinition tech, params DistrictDefinition[] districts)
        {
            if (tech.unlocksDistricts == null)
                tech.unlocksDistricts = new List<DistrictDefinition>();
            tech.unlocksDistricts.AddRange(districts);
            return tech;
        }

        // Helper method to add ability unlocks
        public static TechDefinition WithAbilityUnlocks(this TechDefinition tech, params AbilityDefinition[] abilities)
        {
            if (tech.unlocksAbilities == null)
                tech.unlocksAbilities = new List<AbilityDefinition>();
            tech.unlocksAbilities.AddRange(abilities);
            return tech;
        }

        // Helper method to set yield bonuses
        public static TechDefinition WithYieldBonus(this TechDefinition tech, float protoculture = 0, float science = 0, float production = 0)
        {
            tech.protoculturePerTurn = protoculture;
            tech.sciencePerTurn = science;
            tech.productionPerTurn = production;
            return tech;
        }

        // Helper method to set unit stat bonuses
        public static TechDefinition WithUnitBonuses(this TechDefinition tech, int hp = 0, int armor = 0, int movement = 0, int attack = 0)
        {
            tech.hpBonus = hp;
            tech.armorBonus = armor;
            tech.movementBonus = movement;
            tech.attackBonus = attack;
            return tech;
        }
    }
}
