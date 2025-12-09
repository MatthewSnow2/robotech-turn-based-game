using UnityEngine;
using Robotech.TBS.Hex;
using Robotech.TBS.Map;
using Robotech.TBS.Fog;
using Robotech.TBS.Systems;
using Robotech.TBS.Bootstrap;
using Robotech.TBS.Data;
using Robotech.TBS.Units;
using Robotech.TBS.Core;
using Robotech.TBS.Rendering;
using Robotech.TBS.AI;

namespace Robotech.TBS.Bootstrap
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Core Objects")]
        public HexGrid grid;
        public MapGenerator mapGen;
        public FogOfWarSystem fog;
        public ResourceManager resources;
        public TurnManager turnManager;
        public CityManager cityManager;
        public TechManager techManager;
        public UnitRegistry unitRegistry;
        public AIController aiController;

        [Header("Runtime Unit Defs (temp)")]
        public UnitDefinition rdfVF1A;
        public UnitDefinition zentTacticalPod;
        public UnitDefinition rdfSettler;
        public UnitDefinition zentSettler;
        public UnitDefinition rdfArmoredVeritech;
        public UnitDefinition rdfSuperVeritech;
        public UnitDefinition zentOfficerPod;

        void Awake()
        {
            // Ensure components exist
            if (grid == null) grid = gameObject.AddComponent<HexGrid>();
            if (mapGen == null) mapGen = gameObject.AddComponent<MapGenerator>();
            if (fog == null) fog = gameObject.AddComponent<FogOfWarSystem>();
            if (resources == null) resources = gameObject.AddComponent<ResourceManager>();
            if (turnManager == null) turnManager = gameObject.AddComponent<TurnManager>();
            if (cityManager == null) cityManager = gameObject.AddComponent<CityManager>();
            if (techManager == null) techManager = gameObject.AddComponent<TechManager>();
            if (unitRegistry == null) unitRegistry = gameObject.AddComponent<UnitRegistry>();
            if (aiController == null)
            {
                aiController = gameObject.AddComponent<AIController>();
                aiController.grid = grid;
                aiController.techManager = techManager;
                aiController.cityManager = cityManager;
                aiController.mapGen = mapGen;
            }
            // Auto-add debug renderer for quick visualization
            var debugRenderer = gameObject.AddComponent<HexDebugRenderer>();
            debugRenderer.grid = grid;
            debugRenderer.mapGen = mapGen;

            // Create minimal terrain ScriptableObjects for generator
            var plains   = DefinitionsFactory.CreateTerrain("plains","Plains",1,0);
            var forest   = DefinitionsFactory.CreateTerrain("forest","Forest",2,1);
            var hills    = DefinitionsFactory.CreateTerrain("hills","Hills",2,1,false,false,false,true);
            var mountains= DefinitionsFactory.CreateTerrain("mountains","Mountains",99,3,false,true,false,true);
            var desert   = DefinitionsFactory.CreateTerrain("desert","Desert",1,0);
            var tundra   = DefinitionsFactory.CreateTerrain("tundra","Tundra",1,0);
            var marsh    = DefinitionsFactory.CreateTerrain("marsh","Marsh",2,0);
            var urban    = DefinitionsFactory.CreateTerrain("urban","Urban/Ruins",1,1,false,false,true,false);
            var coast    = DefinitionsFactory.CreateTerrain("coast","Coast",1,0,true,false,false,false);
            var ocean    = DefinitionsFactory.CreateTerrain("ocean","Ocean",1,0,true,true,false,false);

            mapGen.grid = grid;
            mapGen.plains = plains; mapGen.forest=forest; mapGen.hills=hills; mapGen.mountains=mountains;
            mapGen.desert=desert; mapGen.tundra=tundra; mapGen.marsh=marsh; mapGen.urban=urban; mapGen.coast=coast; mapGen.ocean=ocean;
            mapGen.Generate();

            fog.grid = grid;

            // Create temporary weapon defs
            var gun = DefinitionsFactory.CreateWeapon("vf1a_gun","GU-11 Gun Pod","Kinetic",12,1,1,1,0.75f,false,false);
            var podGun = DefinitionsFactory.CreateWeapon("pod_gun","Battlepod Cannons","Kinetic",10,1,1,1,0.7f,false,false);
            var officerGun = DefinitionsFactory.CreateWeapon("officer_gun","Officer Pod Cannons","Kinetic",14,1,1,1,0.72f,false,false);

            // Create unit defs
            rdfVF1A = DefinitionsFactory.CreateUnit("vf1a","VF-1A Veritech", Faction.RDF, UnitLayer.Air, 100, 1, 4, 3, new[] { gun }, canTransform:true);
            zentTacticalPod = DefinitionsFactory.CreateUnit("tpod","Tactical Battlepod", Faction.Zentradi, UnitLayer.Ground, 110, 1, 3, 2, new[] { podGun });
            zentOfficerPod = DefinitionsFactory.CreateUnit("opod","Officer Battlepod", Faction.Zentradi, UnitLayer.Ground, 140, 2, 3, 2, new[] { officerGun });
            rdfSettler = DefinitionsFactory.CreateUnit("rdf_settler","RDF Engineer Corps", Faction.RDF, UnitLayer.Ground, 60, 0, 2, 2, new WeaponDefinition[] { }, false, false, false, true);
            zentSettler = DefinitionsFactory.CreateUnit("zent_colonizer","Zentradi Outpost Crew", Faction.Zentradi, UnitLayer.Ground, 60, 0, 2, 2, new WeaponDefinition[] { }, false, false, false, true);
            // Advanced RDF units unlocked by tech
            var heavyGun = DefinitionsFactory.CreateWeapon("arm_gun","Enhanced GU-11","Kinetic",16,1,1,1,0.75f,false,false);
            rdfArmoredVeritech = DefinitionsFactory.CreateUnit("vf1a_arm","Armored Veritech", Faction.RDF, UnitLayer.Air, 140, 2, 3, 3, new[] { heavyGun }, canTransform:true, ecm:false, jj:false);
            var superGun = DefinitionsFactory.CreateWeapon("super_gun","Super Veritech Cannons","Kinetic",18,1,1,1,0.78f,false,false);
            rdfSuperVeritech = DefinitionsFactory.CreateUnit("vf1a_sup","Super Veritech", Faction.RDF, UnitLayer.Air, 150, 2, 4, 3, new[] { superGun }, canTransform:true, ecm:true, jj:false);

            // Spawn two units
            var startA = new HexCoord(5, 5);
            var startB = new HexCoord(grid.width - 6, grid.height - 6);
            var uA = UnitFactory.SpawnUnit("RDF", rdfVF1A, startA, grid.hexSize);
            var uB = UnitFactory.SpawnUnit("ZENT", zentTacticalPod, startB, grid.hexSize);
            // Spawn settlers near starts if passable
            var settlerPosA = new HexCoord(startA.q + 1, startA.r);
            var settlerPosB = new HexCoord(startB.q - 1, startB.r);
            UnitFactory.SpawnUnit("RDF", rdfSettler, settlerPosA, grid.hexSize);
            UnitFactory.SpawnUnit("ZENT", zentSettler, settlerPosB, grid.hexSize);

            // Initial vision
            fog.ClearVisibility();
            fog.RevealFrom(startA, rdfVF1A.vision);

            // Hook turns
            TurnManager.OnTurnStarted += OnTurnStarted;

            // Initialize tech tree
            InitializeTechTree();
        }

        private void InitializeTechTree()
        {
            // Gen 0 Techs (8 total) - No prerequisites
            var jetPropulsion = DefinitionsFactory.CreateTech(
                "jet_propulsion", "Jet Propulsion", 10, TechGeneration.Gen0, TechCategory.Aerospace,
                "Advanced jet engine technology for high-speed flight.",
                isCriticalPath: true);

            var conventionalBallistics = DefinitionsFactory.CreateTech(
                "conventional_ballistics", "Conventional Ballistics", 15, TechGeneration.Gen0, TechCategory.Weapons,
                "Standard projectile weapons and gun pods.");

            var protocultureDiscovery = DefinitionsFactory.CreateTech(
                "protoculture_discovery", "Protoculture Discovery", 20, TechGeneration.Gen0, TechCategory.Special,
                "Unlocks the secrets of protoculture energy.",
                isCriticalPath: true, allowsEraTransition: true);

            var reactorMk1 = DefinitionsFactory.CreateTech(
                "reactor_mk1", "Energy Reactors Mk I", 15, TechGeneration.Gen0, TechCategory.Power,
                "Basic protoculture reactor systems.")
                .WithYieldBonus(protoculture: 10);

            var chassisI = DefinitionsFactory.CreateTech(
                "chassis_i", "Mecha Chassis I", 15, TechGeneration.Gen0, TechCategory.Mecha,
                "Foundational mecha framework and transformation mechanics.");

            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Defense,
                "Advanced alloys and armor plating.")
                .WithUnitBonuses(armor: 5);

            var missileGuidanceI = DefinitionsFactory.CreateTech(
                "missile_guidance_i", "Missile Guidance I", 13, TechGeneration.Gen0, TechCategory.Weapons,
                "Basic missile targeting systems.");

            var globalComms = DefinitionsFactory.CreateTech(
                "global_comms", "Global Communications Network", 18, TechGeneration.Gen0, TechCategory.Special,
                "Worldwide communication and data sharing infrastructure.")
                .WithYieldBonus(science: 5);

            // Gen 1 Techs (8 total) - With prerequisites
            var transformationI = DefinitionsFactory.CreateTech(
                "transformation_i", "Transformation Engineering I", 30, TechGeneration.Gen1, TechCategory.Mecha,
                "Enables VF-0 prototype production through advanced transformation systems.",
                isCriticalPath: true)
                .WithPrerequisites(chassisI);

            var sensorsI = DefinitionsFactory.CreateTech(
                "sensors_i", "Sensor Suite Integration I", 25, TechGeneration.Gen1, TechCategory.Aerospace,
                "Integrated sensor systems for improved targeting and reconnaissance.")
                .WithPrerequisites(jetPropulsion);

            var reactorMk2 = DefinitionsFactory.CreateTech(
                "reactor_mk2", "Reactor Mk II", 35, TechGeneration.Gen1, TechCategory.Power,
                "Enhanced protoculture reactor efficiency.")
                .WithPrerequisites(reactorMk1)
                .WithYieldBonus(protoculture: 15);

            var chassisII = DefinitionsFactory.CreateTech(
                "chassis_ii", "Mecha Chassis II", 32, TechGeneration.Gen1, TechCategory.Mecha,
                "Refined mecha designs with improved structural integrity.")
                .WithPrerequisites(chassisI);

            var missileControlII = DefinitionsFactory.CreateTech(
                "missile_control_ii", "Missile Control II", 28, TechGeneration.Gen1, TechCategory.Weapons,
                "Advanced missile guidance and tracking systems.")
                .WithPrerequisites(missileGuidanceI);

            var advancedMaterials = DefinitionsFactory.CreateTech(
                "advanced_materials", "Advanced Materials", 30, TechGeneration.Gen1, TechCategory.Defense,
                "Next-generation composite materials for superior armor.")
                .WithPrerequisites(metallurgyI)
                .WithUnitBonuses(armor: 8);

            var radarNetwork = DefinitionsFactory.CreateTech(
                "radar_network", "Radar Network", 25, TechGeneration.Gen1, TechCategory.Special,
                "Integrated radar systems for enhanced detection and tracking.")
                .WithPrerequisites(globalComms);

            var scoutArmor = DefinitionsFactory.CreateTech(
                "scout_armor", "Scout Armor Program", 22, TechGeneration.Gen1, TechCategory.Mecha,
                "Lightweight armor system for reconnaissance units.")
                .WithPrerequisites(chassisI)
                .WithUnitBonuses(armor: 3);

            // Add all techs to TechManager
            techManager.allTechs.Add(jetPropulsion);
            techManager.allTechs.Add(conventionalBallistics);
            techManager.allTechs.Add(protocultureDiscovery);
            techManager.allTechs.Add(reactorMk1);
            techManager.allTechs.Add(chassisI);
            techManager.allTechs.Add(metallurgyI);
            techManager.allTechs.Add(missileGuidanceI);
            techManager.allTechs.Add(globalComms);
            techManager.allTechs.Add(transformationI);
            techManager.allTechs.Add(sensorsI);
            techManager.allTechs.Add(reactorMk2);
            techManager.allTechs.Add(chassisII);
            techManager.allTechs.Add(missileControlII);
            techManager.allTechs.Add(advancedMaterials);
            techManager.allTechs.Add(radarNetwork);
            techManager.allTechs.Add(scoutArmor);

            // Update available techs (Gen 0 techs should be immediately available)
            techManager.UpdateAvailableTechs();

            // Legacy compatibility - add techs to old techTree list
            techManager.techTree.AddRange(techManager.allTechs);

            // Set up event handlers
            techManager.OnTechCompleted += t => Debug.Log($"Tech completed: {t.displayName}");
            techManager.OnEraTransition += era => Debug.Log($"Era transition: Advanced to {era}");

            Debug.Log($"Tech tree initialized with {techManager.allTechs.Count} Gen 0-1 techs");
        }

        private void OnDestroy()
        {
            TurnManager.OnTurnStarted -= OnTurnStarted;
        }

        private void OnTurnStarted(int turn)
        {
            // Reset unit moves and refresh visibility for the simple prototype
            if (UnitRegistry.Instance != null)
            {
                foreach (var unit in UnitRegistry.Instance.GetAllUnits())
                {
                    unit.NewTurn();
                }
            }

            // Recompute visibility from all RDF units for demo
            fog.ClearVisibility();
            if (UnitRegistry.Instance != null)
            {
                foreach (var unit in UnitRegistry.Instance.GetUnitsByFaction(Faction.RDF))
                {
                    fog.RevealFrom(unit.coord, unit.definition.vision);
                }
            }

            // City yields, border growth, and research progress
            if (cityManager != null && resources != null)
            {
                int sciBefore = resources.science;
                cityManager.GrowBorders(1);
                cityManager.ApplyCityYields(resources);
                int sciDelta = resources.science - sciBefore;
                if (techManager != null && sciDelta > 0) techManager.AddScience(sciDelta);
            }

            // Apply unit upkeep (protoculture)
            if (resources != null && UnitRegistry.Instance != null)
            {
                int totalUpkeep = 0;
                foreach (var unit in UnitRegistry.Instance.GetAllUnits())
                {
                    if (unit != null && unit.definition != null)
                        totalUpkeep += unit.definition.upkeepProtoculture;
                }
                if (totalUpkeep > 0)
                    resources.ApplyUpkeep(totalUpkeep);
            }
        }
    }
}
