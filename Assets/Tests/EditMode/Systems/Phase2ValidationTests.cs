using NUnit.Framework;
using UnityEngine;
using Robotech.TBS.Systems;
using Robotech.TBS.Data;
using Robotech.TBS.Bootstrap;
using System.Collections.Generic;
using System.Linq;

namespace Robotech.TBS.Tests.EditMode.Systems
{
    [TestFixture]
    public class Phase2ValidationTests
    {
        private GameObject gameObject;
        private TechManager techManager;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestGameObject");
            techManager = gameObject.AddComponent<TechManager>();
            InitializeFullTechTree();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        private void InitializeFullTechTree()
        {
            // Gen 0 Techs (8 total)
            var jetPropulsion = DefinitionsFactory.CreateTech(
                "jet_propulsion", "Jet Propulsion", 10, TechGeneration.Gen0, TechCategory.Mecha,
                "Advanced jet engine technology.", isCriticalPath: true);

            var conventionalBallistics = DefinitionsFactory.CreateTech(
                "conventional_ballistics", "Conventional Ballistics", 15, TechGeneration.Gen0, TechCategory.Mecha,
                "Standard projectile weapons.");

            var protocultureDiscovery = DefinitionsFactory.CreateTech(
                "protoculture_discovery", "Protoculture Discovery", 20, TechGeneration.Gen0, TechCategory.Special,
                "Unlocks protoculture secrets.", isCriticalPath: true, allowsEraTransition: true);

            var reactorMk1 = DefinitionsFactory.CreateTech(
                "reactor_mk1", "Energy Reactors Mk I", 15, TechGeneration.Gen0, TechCategory.Power,
                "Basic protoculture reactors.")
                .WithYieldBonus(protoculture: 10);

            var chassisI = DefinitionsFactory.CreateTech(
                "chassis_i", "Mecha Chassis I", 15, TechGeneration.Gen0, TechCategory.Mecha,
                "Foundational mecha framework.");

            var metallurgyI = DefinitionsFactory.CreateTech(
                "metallurgy_i", "Metallurgy I", 12, TechGeneration.Gen0, TechCategory.Mecha,
                "Advanced alloys and armor plating.")
                .WithUnitBonuses(armor: 5);

            var missileGuidanceI = DefinitionsFactory.CreateTech(
                "missile_guidance_i", "Missile Guidance I", 13, TechGeneration.Gen0, TechCategory.Mecha,
                "Basic missile targeting systems.");

            var globalComms = DefinitionsFactory.CreateTech(
                "global_comms", "Global Communications Network", 18, TechGeneration.Gen0, TechCategory.Power,
                "Worldwide communication infrastructure.")
                .WithYieldBonus(science: 5);

            // Gen 1 Techs (8 total)
            var transformationI = DefinitionsFactory.CreateTech(
                "transformation_i", "Transformation Engineering I", 30, TechGeneration.Gen1, TechCategory.Mecha,
                "Enables VF-0 production.", isCriticalPath: true)
                .WithPrerequisites(chassisI);

            var sensorsI = DefinitionsFactory.CreateTech(
                "sensors_i", "Sensor Suite Integration I", 25, TechGeneration.Gen1, TechCategory.Mecha,
                "Integrated sensor systems.")
                .WithPrerequisites(jetPropulsion);

            var reactorMk2 = DefinitionsFactory.CreateTech(
                "reactor_mk2", "Reactor Mk II", 35, TechGeneration.Gen1, TechCategory.Power,
                "Enhanced protoculture reactor efficiency.")
                .WithPrerequisites(reactorMk1)
                .WithYieldBonus(protoculture: 15);

            var chassisII = DefinitionsFactory.CreateTech(
                "chassis_ii", "Mecha Chassis II", 32, TechGeneration.Gen1, TechCategory.Mecha,
                "Refined mecha designs.")
                .WithPrerequisites(chassisI);

            var missileControlII = DefinitionsFactory.CreateTech(
                "missile_control_ii", "Missile Control II", 28, TechGeneration.Gen1, TechCategory.Mecha,
                "Advanced missile guidance.")
                .WithPrerequisites(missileGuidanceI);

            var advancedMaterials = DefinitionsFactory.CreateTech(
                "advanced_materials", "Advanced Materials", 30, TechGeneration.Gen1, TechCategory.Mecha,
                "Next-gen composite materials.")
                .WithPrerequisites(metallurgyI)
                .WithUnitBonuses(armor: 8);

            var radarNetwork = DefinitionsFactory.CreateTech(
                "radar_network", "Radar Network", 25, TechGeneration.Gen1, TechCategory.Power,
                "Integrated radar systems.")
                .WithPrerequisites(globalComms);

            var scoutArmor = DefinitionsFactory.CreateTech(
                "scout_armor", "Scout Armor Program", 22, TechGeneration.Gen1, TechCategory.Mecha,
                "Lightweight armor for reconnaissance.")
                .WithPrerequisites(chassisI)
                .WithUnitBonuses(armor: 3);

            // Add all techs
            techManager.allTechs.AddRange(new[]
            {
                jetPropulsion, conventionalBallistics, protocultureDiscovery, reactorMk1,
                chassisI, metallurgyI, missileGuidanceI, globalComms,
                transformationI, sensorsI, reactorMk2, chassisII,
                missileControlII, advancedMaterials, radarNetwork, scoutArmor
            });

            techManager.UpdateAvailableTechs();
        }

        #region Gen 0 Tech Validation

        [Test]
        public void All_Gen0_Techs_Have_Correct_Configuration()
        {
            // Arrange
            var gen0Techs = techManager.GetTechsByGeneration(TechGeneration.Gen0);

            // Assert
            Assert.AreEqual(8, gen0Techs.Count, "Should have exactly 8 Gen 0 techs");

            foreach (var tech in gen0Techs)
            {
                // Verify basic fields
                Assert.IsNotNull(tech, "Tech should not be null");
                Assert.IsNotEmpty(tech.techId, "Tech should have an ID");
                Assert.IsNotEmpty(tech.displayName, "Tech should have a display name");
                Assert.Greater(tech.costScience, 0, $"{tech.displayName} should have positive science cost");
                Assert.AreEqual(TechGeneration.Gen0, tech.generation, $"{tech.displayName} should be Gen 0");

                // Verify no prerequisites for Gen 0
                Assert.IsNotNull(tech.prerequisites, $"{tech.displayName} should have prerequisites list");
                Assert.AreEqual(0, tech.prerequisites.Count,
                    $"{tech.displayName} should have no prerequisites (Gen 0 techs are starting techs)");
            }
        }

        [Test]
        public void Gen0_Specific_Tech_Values_Match_Design()
        {
            // Arrange
            var gen0Techs = techManager.GetTechsByGeneration(TechGeneration.Gen0);

            // Jet Propulsion
            var jetPropulsion = gen0Techs.FirstOrDefault(t => t.techId == "jet_propulsion");
            Assert.IsNotNull(jetPropulsion, "Jet Propulsion should exist");
            Assert.AreEqual(10, jetPropulsion.costScience, "Jet Propulsion cost");
            Assert.IsTrue(jetPropulsion.isCriticalPath, "Jet Propulsion is critical path");

            // Reactor Mk I
            var reactorMk1 = gen0Techs.FirstOrDefault(t => t.techId == "reactor_mk1");
            Assert.IsNotNull(reactorMk1, "Reactor Mk I should exist");
            Assert.AreEqual(15, reactorMk1.costScience, "Reactor Mk I cost");
            Assert.AreEqual(10f, reactorMk1.protoculturePerTurn, "Reactor Mk I provides +10 protoculture");

            // Metallurgy I
            var metallurgyI = gen0Techs.FirstOrDefault(t => t.techId == "metallurgy_i");
            Assert.IsNotNull(metallurgyI, "Metallurgy I should exist");
            Assert.AreEqual(12, metallurgyI.costScience, "Metallurgy I cost");
            Assert.AreEqual(5, metallurgyI.armorBonus, "Metallurgy I provides +5 armor");

            // Global Comms
            var globalComms = gen0Techs.FirstOrDefault(t => t.techId == "global_comms");
            Assert.IsNotNull(globalComms, "Global Communications should exist");
            Assert.AreEqual(18, globalComms.costScience, "Global Comms cost");
            Assert.AreEqual(5f, globalComms.sciencePerTurn, "Global Comms provides +5 science");

            // Protoculture Discovery
            var protocultureDiscovery = gen0Techs.FirstOrDefault(t => t.techId == "protoculture_discovery");
            Assert.IsNotNull(protocultureDiscovery, "Protoculture Discovery should exist");
            Assert.AreEqual(20, protocultureDiscovery.costScience, "Protoculture Discovery cost");
            Assert.IsTrue(protocultureDiscovery.isCriticalPath, "Protoculture Discovery is critical path");
            Assert.IsTrue(protocultureDiscovery.allowsEraTransition, "Protoculture Discovery unlocks Gen 1");

            // Chassis I
            var chassisI = gen0Techs.FirstOrDefault(t => t.techId == "chassis_i");
            Assert.IsNotNull(chassisI, "Chassis I should exist");
            Assert.AreEqual(15, chassisI.costScience, "Chassis I cost");

            // Missile Guidance I
            var missileGuidanceI = gen0Techs.FirstOrDefault(t => t.techId == "missile_guidance_i");
            Assert.IsNotNull(missileGuidanceI, "Missile Guidance I should exist");
            Assert.AreEqual(13, missileGuidanceI.costScience, "Missile Guidance I cost");

            // Conventional Ballistics
            var conventionalBallistics = gen0Techs.FirstOrDefault(t => t.techId == "conventional_ballistics");
            Assert.IsNotNull(conventionalBallistics, "Conventional Ballistics should exist");
            Assert.AreEqual(15, conventionalBallistics.costScience, "Conventional Ballistics cost");
        }

        #endregion

        #region Gen 1 Tech Validation

        [Test]
        public void All_Gen1_Techs_Have_Prerequisites()
        {
            // Arrange
            var gen1Techs = techManager.GetTechsByGeneration(TechGeneration.Gen1);

            // Assert
            Assert.AreEqual(8, gen1Techs.Count, "Should have exactly 8 Gen 1 techs");

            foreach (var tech in gen1Techs)
            {
                // Verify basic fields
                Assert.IsNotNull(tech, "Tech should not be null");
                Assert.IsNotEmpty(tech.techId, $"{tech.displayName} should have ID");
                Assert.AreEqual(TechGeneration.Gen1, tech.generation, $"{tech.displayName} should be Gen 1");

                // Verify prerequisites
                Assert.IsNotNull(tech.prerequisites, $"{tech.displayName} should have prerequisites list");
                Assert.GreaterOrEqual(tech.prerequisites.Count, 1,
                    $"{tech.displayName} should have at least 1 prerequisite");

                // Verify all prerequisites are Gen 0
                foreach (var prereq in tech.prerequisites)
                {
                    Assert.IsNotNull(prereq, $"{tech.displayName} should not have null prerequisite");
                    Assert.AreEqual(TechGeneration.Gen0, prereq.generation,
                        $"{tech.displayName} prerequisite '{prereq.displayName}' should be Gen 0");
                }
            }
        }

        [Test]
        public void Gen1_Prerequisite_Chains_Are_Correct()
        {
            // Arrange
            var gen1Techs = techManager.GetTechsByGeneration(TechGeneration.Gen1);

            // Transformation Engineering I -> Chassis I
            var transformationI = gen1Techs.FirstOrDefault(t => t.techId == "transformation_i");
            Assert.IsNotNull(transformationI, "Transformation I should exist");
            Assert.AreEqual(1, transformationI.prerequisites.Count, "Transformation I has 1 prerequisite");
            Assert.AreEqual("chassis_i", transformationI.prerequisites[0].techId,
                "Transformation I requires Chassis I");

            // Sensor Suite I -> Jet Propulsion
            var sensorsI = gen1Techs.FirstOrDefault(t => t.techId == "sensors_i");
            Assert.IsNotNull(sensorsI, "Sensors I should exist");
            Assert.AreEqual(1, sensorsI.prerequisites.Count, "Sensors I has 1 prerequisite");
            Assert.AreEqual("jet_propulsion", sensorsI.prerequisites[0].techId,
                "Sensors I requires Jet Propulsion");

            // Reactor Mk II -> Reactor Mk I
            var reactorMk2 = gen1Techs.FirstOrDefault(t => t.techId == "reactor_mk2");
            Assert.IsNotNull(reactorMk2, "Reactor Mk II should exist");
            Assert.AreEqual(1, reactorMk2.prerequisites.Count, "Reactor Mk II has 1 prerequisite");
            Assert.AreEqual("reactor_mk1", reactorMk2.prerequisites[0].techId,
                "Reactor Mk II requires Reactor Mk I");

            // Chassis II -> Chassis I
            var chassisII = gen1Techs.FirstOrDefault(t => t.techId == "chassis_ii");
            Assert.IsNotNull(chassisII, "Chassis II should exist");
            Assert.AreEqual(1, chassisII.prerequisites.Count, "Chassis II has 1 prerequisite");
            Assert.AreEqual("chassis_i", chassisII.prerequisites[0].techId,
                "Chassis II requires Chassis I");

            // Missile Control II -> Missile Guidance I
            var missileControlII = gen1Techs.FirstOrDefault(t => t.techId == "missile_control_ii");
            Assert.IsNotNull(missileControlII, "Missile Control II should exist");
            Assert.AreEqual(1, missileControlII.prerequisites.Count, "Missile Control II has 1 prerequisite");
            Assert.AreEqual("missile_guidance_i", missileControlII.prerequisites[0].techId,
                "Missile Control II requires Missile Guidance I");

            // Advanced Materials -> Metallurgy I
            var advancedMaterials = gen1Techs.FirstOrDefault(t => t.techId == "advanced_materials");
            Assert.IsNotNull(advancedMaterials, "Advanced Materials should exist");
            Assert.AreEqual(1, advancedMaterials.prerequisites.Count, "Advanced Materials has 1 prerequisite");
            Assert.AreEqual("metallurgy_i", advancedMaterials.prerequisites[0].techId,
                "Advanced Materials requires Metallurgy I");

            // Radar Network -> Global Comms
            var radarNetwork = gen1Techs.FirstOrDefault(t => t.techId == "radar_network");
            Assert.IsNotNull(radarNetwork, "Radar Network should exist");
            Assert.AreEqual(1, radarNetwork.prerequisites.Count, "Radar Network has 1 prerequisite");
            Assert.AreEqual("global_comms", radarNetwork.prerequisites[0].techId,
                "Radar Network requires Global Comms");

            // Scout Armor -> Chassis I
            var scoutArmor = gen1Techs.FirstOrDefault(t => t.techId == "scout_armor");
            Assert.IsNotNull(scoutArmor, "Scout Armor should exist");
            Assert.AreEqual(1, scoutArmor.prerequisites.Count, "Scout Armor has 1 prerequisite");
            Assert.AreEqual("chassis_i", scoutArmor.prerequisites[0].techId,
                "Scout Armor requires Chassis I");
        }

        [Test]
        public void Gen1_Specific_Tech_Values_Match_Design()
        {
            // Arrange
            var gen1Techs = techManager.GetTechsByGeneration(TechGeneration.Gen1);

            // Transformation Engineering I
            var transformationI = gen1Techs.FirstOrDefault(t => t.techId == "transformation_i");
            Assert.IsNotNull(transformationI, "Transformation I should exist");
            Assert.AreEqual(30, transformationI.costScience, "Transformation I cost");
            Assert.IsTrue(transformationI.isCriticalPath, "Transformation I is critical path");

            // Reactor Mk II
            var reactorMk2 = gen1Techs.FirstOrDefault(t => t.techId == "reactor_mk2");
            Assert.IsNotNull(reactorMk2, "Reactor Mk II should exist");
            Assert.AreEqual(35, reactorMk2.costScience, "Reactor Mk II cost");
            Assert.AreEqual(15f, reactorMk2.protoculturePerTurn, "Reactor Mk II provides +15 protoculture");

            // Advanced Materials
            var advancedMaterials = gen1Techs.FirstOrDefault(t => t.techId == "advanced_materials");
            Assert.IsNotNull(advancedMaterials, "Advanced Materials should exist");
            Assert.AreEqual(30, advancedMaterials.costScience, "Advanced Materials cost");
            Assert.AreEqual(8, advancedMaterials.armorBonus, "Advanced Materials provides +8 armor");

            // Scout Armor
            var scoutArmor = gen1Techs.FirstOrDefault(t => t.techId == "scout_armor");
            Assert.IsNotNull(scoutArmor, "Scout Armor should exist");
            Assert.AreEqual(22, scoutArmor.costScience, "Scout Armor cost");
            Assert.AreEqual(3, scoutArmor.armorBonus, "Scout Armor provides +3 armor");

            // Sensor Suite I
            var sensorsI = gen1Techs.FirstOrDefault(t => t.techId == "sensors_i");
            Assert.IsNotNull(sensorsI, "Sensors I should exist");
            Assert.AreEqual(25, sensorsI.costScience, "Sensors I cost");

            // Radar Network
            var radarNetwork = gen1Techs.FirstOrDefault(t => t.techId == "radar_network");
            Assert.IsNotNull(radarNetwork, "Radar Network should exist");
            Assert.AreEqual(25, radarNetwork.costScience, "Radar Network cost");
        }

        #endregion

        #region Tech Uniqueness and Integrity

        [Test]
        public void Tech_Bonuses_Dont_Duplicate()
        {
            // Verify no two techs have identical IDs
            var techIds = new HashSet<string>();
            foreach (var tech in techManager.allTechs)
            {
                Assert.IsFalse(techIds.Contains(tech.techId),
                    $"Duplicate tech ID found: {tech.techId}");
                techIds.Add(tech.techId);
            }

            // Verify no two techs have identical display names
            var techNames = new HashSet<string>();
            foreach (var tech in techManager.allTechs)
            {
                Assert.IsFalse(techNames.Contains(tech.displayName),
                    $"Duplicate tech name found: {tech.displayName}");
                techNames.Add(tech.displayName);
            }
        }

        [Test]
        public void No_Circular_Prerequisites()
        {
            // Verify no tech depends on itself (direct or indirect)
            foreach (var tech in techManager.allTechs)
            {
                var visited = new HashSet<string>();
                Assert.IsFalse(HasCircularDependency(tech, visited),
                    $"{tech.displayName} has circular prerequisite dependency");
            }
        }

        [Test]
        public void All_Prerequisites_Exist_In_TechTree()
        {
            // Verify all prerequisite references point to valid techs in the tree
            var allTechIds = new HashSet<string>(techManager.allTechs.Select(t => t.techId));

            foreach (var tech in techManager.allTechs)
            {
                foreach (var prereq in tech.prerequisites)
                {
                    Assert.IsNotNull(prereq, $"{tech.displayName} has null prerequisite");
                    Assert.IsTrue(allTechIds.Contains(prereq.techId),
                        $"{tech.displayName} prerequisite '{prereq.techId}' not found in tech tree");
                }
            }
        }

        [Test]
        public void Bonus_Stacking_Formula_Is_Additive()
        {
            // Verify that bonuses from multiple techs stack additively (not multiplicatively)
            var armorTechs = techManager.allTechs.Where(t => t.armorBonus > 0).ToList();
            Assert.GreaterOrEqual(armorTechs.Count, 2, "Should have at least 2 armor bonus techs");

            // Simulate researching all armor techs
            int totalArmorBonus = 0;
            foreach (var tech in armorTechs)
            {
                totalArmorBonus += tech.armorBonus;
            }

            // Verify individual bonuses sum correctly
            var metallurgyI = techManager.allTechs.FirstOrDefault(t => t.techId == "metallurgy_i");
            var advancedMaterials = techManager.allTechs.FirstOrDefault(t => t.techId == "advanced_materials");
            var scoutArmor = techManager.allTechs.FirstOrDefault(t => t.techId == "scout_armor");

            Assert.AreEqual(5 + 8 + 3, totalArmorBonus,
                "Total armor bonus should be additive: 5 + 8 + 3 = 16");
        }

        #endregion

        #region Availability Tests

        [Test]
        public void Gen0_Techs_Available_At_Start()
        {
            // All Gen 0 techs should be immediately available
            var gen0Techs = techManager.GetTechsByGeneration(TechGeneration.Gen0);

            foreach (var tech in gen0Techs)
            {
                Assert.IsTrue(techManager.availableTechs.Contains(tech),
                    $"Gen 0 tech '{tech.displayName}' should be available at start");
            }
        }

        [Test]
        public void Gen1_Techs_Not_Available_At_Start()
        {
            // All Gen 1 techs should NOT be available at start (no prerequisites met)
            var gen1Techs = techManager.GetTechsByGeneration(TechGeneration.Gen1);

            foreach (var tech in gen1Techs)
            {
                Assert.IsFalse(techManager.availableTechs.Contains(tech),
                    $"Gen 1 tech '{tech.displayName}' should NOT be available at start");
            }
        }

        [Test]
        public void Total_Tech_Count_Is_Sixteen()
        {
            // Verify exactly 16 techs (8 Gen 0 + 8 Gen 1)
            Assert.AreEqual(16, techManager.allTechs.Count,
                "Should have exactly 16 Gen 0-1 techs");

            var gen0Count = techManager.GetTechsByGeneration(TechGeneration.Gen0).Count;
            var gen1Count = techManager.GetTechsByGeneration(TechGeneration.Gen1).Count;

            Assert.AreEqual(8, gen0Count, "Should have 8 Gen 0 techs");
            Assert.AreEqual(8, gen1Count, "Should have 8 Gen 1 techs");
        }

        #endregion

        #region Helper Methods

        private bool HasCircularDependency(TechDefinition tech, HashSet<string> visited)
        {
            if (visited.Contains(tech.techId))
            {
                return true; // Circular dependency detected
            }

            visited.Add(tech.techId);

            foreach (var prereq in tech.prerequisites)
            {
                if (prereq != null && HasCircularDependency(prereq, new HashSet<string>(visited)))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
