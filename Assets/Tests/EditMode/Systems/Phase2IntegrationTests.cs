using NUnit.Framework;
using UnityEngine;
using Robotech.TBS.Systems;
using Robotech.TBS.Data;
using Robotech.TBS.Bootstrap;
using Robotech.TBS.Units;
using Robotech.TBS.Hex;
using System.Collections.Generic;
using System.Linq;

namespace Robotech.TBS.Tests.EditMode.Systems
{
    [TestFixture]
    public class Phase2IntegrationTests
    {
        private GameObject gameObject;
        private TechManager techManager;
        private ResourceManager resourceManager;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestGameObject");
            techManager = gameObject.AddComponent<TechManager>();
            resourceManager = gameObject.AddComponent<ResourceManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        #region Resource Bonus Tests

        [Test]
        public void ResourceBonus_Applied_WhenTechCompletes()
        {
            // Arrange
            var reactorMk1 = DefinitionsFactory.CreateTech(
                "reactor_mk1", "Reactor Mk I", 15, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 10);

            techManager.allTechs.Add(reactorMk1);

            // Track initial protoculture
            int initialProtoculture = resourceManager.protoculture;

            // Subscribe to tech completion event to apply bonus
            techManager.OnTechCompleted += tech =>
            {
                resourceManager.protoculture += (int)tech.protoculturePerTurn;
            };

            // Act
            techManager.SetResearch(reactorMk1);
            techManager.AddScience(reactorMk1.costScience);

            // Assert
            Assert.AreEqual(initialProtoculture + 10, resourceManager.protoculture,
                "Protoculture should increase by +10 from Reactor Mk I");
            Assert.IsTrue(techManager.researchedTechs.Contains(reactorMk1),
                "Reactor Mk I should be in researched techs");
        }

        [Test]
        public void MultipleResourceBonuses_Stack_Correctly()
        {
            // Arrange
            var reactorMk1 = DefinitionsFactory.CreateTech(
                "reactor_mk1", "Reactor Mk I", 15, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 10);

            var reactorMk2 = DefinitionsFactory.CreateTech(
                "reactor_mk2", "Reactor Mk II", 35, TechGeneration.Gen1, TechCategory.Power, "Desc")
                .WithPrerequisites(reactorMk1)
                .WithYieldBonus(protoculture: 15);

            techManager.allTechs.Add(reactorMk1);
            techManager.allTechs.Add(reactorMk2);

            int initialProtoculture = resourceManager.protoculture;

            // Subscribe to apply bonuses
            techManager.OnTechCompleted += tech =>
            {
                resourceManager.protoculture += (int)tech.protoculturePerTurn;
            };

            // Act - Research Reactor Mk I
            techManager.SetResearch(reactorMk1);
            techManager.AddScience(reactorMk1.costScience);

            Assert.AreEqual(initialProtoculture + 10, resourceManager.protoculture,
                "After Mk I: +10 protoculture");

            // Research Reactor Mk II
            techManager.SetResearch(reactorMk2);
            techManager.AddScience(reactorMk2.costScience);

            // Assert - Bonuses should stack
            Assert.AreEqual(initialProtoculture + 10 + 15, resourceManager.protoculture,
                "After Mk I + Mk II: +25 total protoculture");
        }

        [Test]
        public void ScienceBonus_Applied_WhenTechCompletes()
        {
            // Arrange
            var globalComms = DefinitionsFactory.CreateTech(
                "global_comms", "Global Communications", 18, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(science: 5);

            techManager.allTechs.Add(globalComms);

            int initialScience = resourceManager.science;

            techManager.OnTechCompleted += tech =>
            {
                resourceManager.science += (int)tech.sciencePerTurn;
            };

            // Act
            techManager.SetResearch(globalComms);
            techManager.AddScience(globalComms.costScience);

            // Assert
            Assert.AreEqual(initialScience + 5, resourceManager.science,
                "Science should increase by +5 from Global Comms");
        }

        [Test]
        public void ProductionBonus_Applied_WhenTechCompletes()
        {
            // Arrange
            var productionTech = DefinitionsFactory.CreateTech(
                "prod_tech", "Production Tech", 20, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(production: 12);

            techManager.allTechs.Add(productionTech);

            int initialMaterials = resourceManager.materials;

            techManager.OnTechCompleted += tech =>
            {
                resourceManager.materials += (int)tech.productionPerTurn;
            };

            // Act
            techManager.SetResearch(productionTech);
            techManager.AddScience(productionTech.costScience);

            // Assert
            Assert.AreEqual(initialMaterials + 12, resourceManager.materials,
                "Materials should increase by +12 from production tech");
        }

        [Test]
        public void Bonus_CalculationMethod_IncludesAllTechs()
        {
            // Arrange
            var reactor1 = DefinitionsFactory.CreateTech("r1", "R1", 10, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(protoculture: 10);
            var reactor2 = DefinitionsFactory.CreateTech("r2", "R2", 20, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(protoculture: 15);
            var reactor3 = DefinitionsFactory.CreateTech("r3", "R3", 30, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(protoculture: 8);

            techManager.allTechs.AddRange(new[] { reactor1, reactor2, reactor3 });

            // Simulate researching all three
            techManager.researchedTechs.Add(reactor1);
            techManager.researchedTechs.Add(reactor2);
            techManager.researchedTechs.Add(reactor3);

            // Act - Calculate total bonus from all researched techs
            float totalBonus = CalculateTotalProtoculturePerTurn(techManager);

            // Assert
            Assert.AreEqual(33, totalBonus, "Total protoculture bonus should be 10 + 15 + 8 = 33");
        }

        #endregion

        #region Unit Factory and Tech Requirement Tests

        [Test]
        public void UnitFactory_Rejects_Unit_Without_RequiredTech()
        {
            // Arrange
            var transformationI = DefinitionsFactory.CreateTech(
                "transformation_i", "Transformation Eng I", 30, TechGeneration.Gen1, TechCategory.Mecha, "Desc");

            var vf0 = DefinitionsFactory.CreateUnit(
                "vf0", "VF-0 Phoenix", Faction.RDF, UnitLayer.Air, 100, 1, 4, 3,
                new WeaponDefinition[] { });
            vf0.requiredTech = transformationI;

            techManager.allTechs.Add(transformationI);

            // Act
            bool canProduce = CanProduceUnit(vf0, techManager);

            // Assert
            Assert.IsFalse(canProduce, "VF-0 should be blocked without Transformation Engineering I");
        }

        [Test]
        public void UnitFactory_Allows_Unit_With_RequiredTech()
        {
            // Arrange
            var transformationI = DefinitionsFactory.CreateTech(
                "transformation_i", "Transformation Eng I", 30, TechGeneration.Gen1, TechCategory.Mecha, "Desc");

            var vf0 = DefinitionsFactory.CreateUnit(
                "vf0", "VF-0 Phoenix", Faction.RDF, UnitLayer.Air, 100, 1, 4, 3,
                new WeaponDefinition[] { });
            vf0.requiredTech = transformationI;

            techManager.allTechs.Add(transformationI);
            techManager.researchedTechs.Add(transformationI);

            // Act
            bool canProduce = CanProduceUnit(vf0, techManager);

            // Assert
            Assert.IsTrue(canProduce, "VF-0 should be allowed after researching Transformation Engineering I");
        }

        #endregion

        #region Unit Upgrade Tests

        [Test]
        public void UnitUpgrade_Applied_OnSpawn()
        {
            // Arrange
            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(armor: 5);

            var unitDef = DefinitionsFactory.CreateUnit(
                "test_unit", "Test Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });

            techManager.allTechs.Add(metallurgyI);
            techManager.researchedTechs.Add(metallurgyI); // Already researched

            // Act - Spawn unit after tech is researched
            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));
            ApplyTechUpgrades(unit, techManager);

            // Assert
            Assert.AreEqual(15, unit.definition.armor, "Unit armor should be 10 (base) + 5 (Metallurgy I) = 15");
        }

        [Test]
        public void UnitUpgrade_Applied_Retroactively()
        {
            // Arrange
            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(armor: 5);

            var unitDef = DefinitionsFactory.CreateUnit(
                "test_unit", "Test Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });

            techManager.allTechs.Add(metallurgyI);

            // Spawn unit BEFORE tech is researched
            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));
            Assert.AreEqual(10, unit.definition.armor, "Initial armor should be 10");

            // Act - Research tech
            techManager.SetResearch(metallurgyI);
            techManager.AddScience(metallurgyI.costScience);

            // Apply upgrade retroactively
            ApplyTechUpgrades(unit, techManager);

            // Assert
            Assert.AreEqual(15, unit.definition.armor, "Armor should be upgraded to 15 after research");
        }

        [Test]
        public void MultipleUnitUpgrades_Stack()
        {
            // Arrange
            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(armor: 5);

            var advancedMaterials = DefinitionsFactory.CreateTech(
                "advanced_materials", "Advanced Materials", 30, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(metallurgyI)
                .WithUnitBonuses(armor: 8);

            var unitDef = DefinitionsFactory.CreateUnit(
                "test_unit", "Test Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });

            techManager.allTechs.Add(metallurgyI);
            techManager.allTechs.Add(advancedMaterials);

            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));

            // Act - Research both techs
            techManager.researchedTechs.Add(metallurgyI);
            techManager.researchedTechs.Add(advancedMaterials);

            ApplyTechUpgrades(unit, techManager);

            // Assert
            Assert.AreEqual(23, unit.definition.armor,
                "Armor should be 10 (base) + 5 (Metallurgy I) + 8 (Advanced Materials) = 23");
        }

        [Test]
        public void TechBonus_Persists_InUnit()
        {
            // Arrange
            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(armor: 5);

            var unitDef = DefinitionsFactory.CreateUnit(
                "test_unit", "Test Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });

            techManager.allTechs.Add(metallurgyI);
            techManager.researchedTechs.Add(metallurgyI);

            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));
            ApplyTechUpgrades(unit, techManager);

            int armorAfterUpgrade = unit.definition.armor;

            // Act - Simulate some time passing (unit moves, takes damage, etc.)
            unit.MoveTo(new HexCoord(1, 0), 1.0f);
            unit.TakeDamage(10);

            // Assert - Armor value should remain the same
            Assert.AreEqual(armorAfterUpgrade, unit.definition.armor,
                "Tech bonuses should persist throughout the game");
        }

        [Test]
        public void HasTechUpgrade_PreventsDuplicates()
        {
            // Arrange
            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(armor: 5);

            var unitDef = DefinitionsFactory.CreateUnit(
                "test_unit", "Test Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });

            techManager.allTechs.Add(metallurgyI);
            techManager.researchedTechs.Add(metallurgyI);

            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));

            // Act - Apply upgrades twice
            ApplyTechUpgrades(unit, techManager);
            int armorAfterFirstApplication = unit.definition.armor;

            ApplyTechUpgrades(unit, techManager); // Apply again

            // Assert - Armor should not double-apply
            Assert.AreEqual(armorAfterFirstApplication, unit.definition.armor,
                "Tech upgrades should not be applied multiple times");
        }

        [Test]
        public void BypassTechCheck_AllowsDebugSpawning()
        {
            // Arrange
            var transformationI = DefinitionsFactory.CreateTech(
                "transformation_i", "Transformation Eng I", 30, TechGeneration.Gen1, TechCategory.Mecha, "Desc");

            var vf0 = DefinitionsFactory.CreateUnit(
                "vf0", "VF-0 Phoenix", Faction.RDF, UnitLayer.Air, 100, 1, 4, 3,
                new WeaponDefinition[] { });
            vf0.requiredTech = transformationI;

            techManager.allTechs.Add(transformationI);
            // Tech NOT researched

            // Act - Bypass tech check for debugging
            bool canProduceWithBypass = CanProduceUnit(vf0, techManager, bypassTechCheck: true);

            // Assert
            Assert.IsTrue(canProduceWithBypass, "Admin should be able to spawn locked units with bypass flag");
        }

        #endregion

        #region Complex Scenario Tests

        [Test]
        public void ComplexScenario_5Techs_Researched()
        {
            // Arrange - Create 5 different techs with various bonuses
            var tech1 = DefinitionsFactory.CreateTech("t1", "Tech 1", 10, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(protoculture: 10);
            var tech2 = DefinitionsFactory.CreateTech("t2", "Tech 2", 15, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(science: 5);
            var tech3 = DefinitionsFactory.CreateTech("t3", "Tech 3", 20, TechGeneration.Gen0, TechCategory.Mecha, "D")
                .WithUnitBonuses(armor: 5);
            var tech4 = DefinitionsFactory.CreateTech("t4", "Tech 4", 25, TechGeneration.Gen0, TechCategory.Power, "D")
                .WithYieldBonus(production: 8);
            var tech5 = DefinitionsFactory.CreateTech("t5", "Tech 5", 30, TechGeneration.Gen0, TechCategory.Mecha, "D")
                .WithUnitBonuses(hp: 20, attack: 3);

            techManager.allTechs.AddRange(new[] { tech1, tech2, tech3, tech4, tech5 });

            int initialProto = resourceManager.protoculture;
            int initialScience = resourceManager.science;
            int initialMaterials = resourceManager.materials;

            // Subscribe to apply all bonuses
            techManager.OnTechCompleted += tech =>
            {
                resourceManager.protoculture += (int)tech.protoculturePerTurn;
                resourceManager.science += (int)tech.sciencePerTurn;
                resourceManager.materials += (int)tech.productionPerTurn;
            };

            // Act - Research all 5 techs
            foreach (var tech in new[] { tech1, tech2, tech3, tech4, tech5 })
            {
                techManager.SetResearch(tech);
                techManager.AddScience(tech.costScience);
            }

            // Create a unit and apply upgrades
            var unitDef = DefinitionsFactory.CreateUnit("unit", "Unit", Faction.RDF, UnitLayer.Ground, 100, 10, 3, 3,
                new WeaponDefinition[] { });
            var unit = SpawnTestUnit(unitDef, new HexCoord(0, 0));
            ApplyTechUpgrades(unit, techManager);

            // Assert - Verify all bonuses applied
            Assert.AreEqual(initialProto + 10, resourceManager.protoculture, "Protoculture bonus");
            Assert.AreEqual(initialScience + 5, resourceManager.science, "Science bonus");
            Assert.AreEqual(initialMaterials + 8, resourceManager.materials, "Production bonus");
            Assert.AreEqual(120, unit.definition.maxHP, "HP should be 100 + 20");
            Assert.AreEqual(15, unit.definition.armor, "Armor should be 10 + 5");
            Assert.AreEqual(5, techManager.researchedTechs.Count, "All 5 techs researched");
        }

        [Test]
        public void EdgeCase_ZeroBonuses_WorkCorrectly()
        {
            // Arrange - Tech with zero bonuses
            var zeroTech = DefinitionsFactory.CreateTech(
                "zero_tech", "Zero Tech", 10, TechGeneration.Gen0, TechCategory.Special, "D")
                .WithYieldBonus(protoculture: 0, science: 0, production: 0)
                .WithUnitBonuses(hp: 0, armor: 0, attack: 0, movement: 0);

            techManager.allTechs.Add(zeroTech);

            int initialProto = resourceManager.protoculture;

            techManager.OnTechCompleted += tech =>
            {
                resourceManager.protoculture += (int)tech.protoculturePerTurn;
            };

            // Act
            techManager.SetResearch(zeroTech);
            techManager.AddScience(zeroTech.costScience);

            // Assert - Should complete without errors
            Assert.IsTrue(techManager.researchedTechs.Contains(zeroTech), "Tech should complete");
            Assert.AreEqual(initialProto, resourceManager.protoculture, "Protoculture unchanged (0 bonus)");
        }

        #endregion

        #region Helper Methods

        private float CalculateTotalProtoculturePerTurn(TechManager tm)
        {
            float total = 0;
            foreach (var tech in tm.researchedTechs)
            {
                total += tech.protoculturePerTurn;
            }
            return total;
        }

        private bool CanProduceUnit(UnitDefinition unit, TechManager tm, bool bypassTechCheck = false)
        {
            if (bypassTechCheck) return true;
            if (unit.requiredTech == null) return true;
            return tm.researchedTechs.Contains(unit.requiredTech);
        }

        private Unit SpawnTestUnit(UnitDefinition def, HexCoord coord)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var unit = go.AddComponent<Unit>();
            unit.Init(def, coord, 1.0f);
            return unit;
        }

        private void ApplyTechUpgrades(Unit unit, TechManager tm)
        {
            // Track which techs have been applied to prevent duplicates
            var appliedTechs = new HashSet<string>();

            foreach (var tech in tm.researchedTechs)
            {
                if (appliedTechs.Contains(tech.techId)) continue;

                unit.definition.maxHP += tech.hpBonus;
                unit.definition.armor += tech.armorBonus;
                unit.definition.movement += tech.movementBonus;
                // Attack bonus would apply to weapons, simplified here

                appliedTechs.Add(tech.techId);
            }
        }

        #endregion
    }
}
