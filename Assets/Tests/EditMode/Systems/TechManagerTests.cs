using NUnit.Framework;
using UnityEngine;
using Robotech.TBS.Systems;
using Robotech.TBS.Data;
using Robotech.TBS.Bootstrap;
using System.Collections.Generic;

namespace Robotech.TBS.Tests.EditMode.Systems
{
    [TestFixture]
    public class TechManagerTests
    {
        private GameObject techManagerObject;
        private TechManager techManager;

        [SetUp]
        public void SetUp()
        {
            techManagerObject = new GameObject("TechManager");
            techManager = techManagerObject.AddComponent<TechManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(techManagerObject);
        }

        #region CreateTech Factory Tests

        [Test]
        public void CreateTech_WithRequiredParameters_CreatesValidTech()
        {
            // Arrange & Act
            var tech = DefinitionsFactory.CreateTech(
                "tech_basic",
                "Basic Tech",
                100,
                TechGeneration.Gen0,
                TechCategory.Mecha,
                "A basic technology"
            );

            // Assert
            Assert.IsNotNull(tech);
            Assert.AreEqual("tech_basic", tech.techId);
            Assert.AreEqual("Basic Tech", tech.displayName);
            Assert.AreEqual(100, tech.costScience);
            Assert.AreEqual(TechGeneration.Gen0, tech.generation);
            Assert.AreEqual(TechCategory.Mecha, tech.category);
            Assert.AreEqual("A basic technology", tech.description);
            Assert.IsFalse(tech.isCriticalPath);
            Assert.IsFalse(tech.allowsEraTransition);
        }

        [Test]
        public void CreateTech_WithOptionalParameters_SetsAllFields()
        {
            // Arrange & Act
            var tech = DefinitionsFactory.CreateTech(
                "tech_advanced",
                "Advanced Tech",
                200,
                TechGeneration.Gen1,
                TechCategory.Power,
                "An advanced technology",
                icon: null,
                isCriticalPath: true,
                allowsEraTransition: true
            );

            // Assert
            Assert.IsNotNull(tech);
            Assert.IsTrue(tech.isCriticalPath);
            Assert.IsTrue(tech.allowsEraTransition);
        }

        [Test]
        public void CreateTech_InitializesCollections()
        {
            // Arrange & Act
            var tech = DefinitionsFactory.CreateTech(
                "tech_test",
                "Test Tech",
                50,
                TechGeneration.Gen0,
                TechCategory.Power,
                "Test description"
            );

            // Assert
            Assert.IsNotNull(tech.prerequisites);
            Assert.IsNotNull(tech.unlocksUnits);
            Assert.IsNotNull(tech.unlocksDistricts);
            Assert.IsNotNull(tech.unlocksAbilities);
            Assert.AreEqual(0, tech.prerequisites.Count);
            Assert.AreEqual(0, tech.unlocksUnits.Count);
            Assert.AreEqual(0, tech.unlocksDistricts.Count);
            Assert.AreEqual(0, tech.unlocksAbilities.Count);
        }

        [Test]
        public void CreateTech_WithPrerequisites_AddsPrerequisitesCorrectly()
        {
            // Arrange
            var prereq1 = DefinitionsFactory.CreateTech("prereq1", "Prerequisite 1", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var prereq2 = DefinitionsFactory.CreateTech("prereq2", "Prerequisite 2", 50, TechGeneration.Gen0, TechCategory.Power, "Desc");

            // Act
            var tech = DefinitionsFactory.CreateTech("tech_test", "Test Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq1, prereq2);

            // Assert
            Assert.AreEqual(2, tech.prerequisites.Count);
            Assert.Contains(prereq1, tech.prerequisites);
            Assert.Contains(prereq2, tech.prerequisites);
        }

        [Test]
        public void CreateTech_WithYieldBonus_SetsYieldValues()
        {
            // Arrange & Act
            var tech = DefinitionsFactory.CreateTech("tech_test", "Test Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);

            // Assert
            Assert.AreEqual(5, tech.protoculturePerTurn);
            Assert.AreEqual(10, tech.sciencePerTurn);
            Assert.AreEqual(15, tech.productionPerTurn);
        }

        [Test]
        public void CreateTech_WithUnitBonuses_SetsBonusValues()
        {
            // Arrange & Act
            var tech = DefinitionsFactory.CreateTech("tech_test", "Test Tech", 100, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithUnitBonuses(hp: 10, armor: 2, movement: 1, attack: 5);

            // Assert
            Assert.AreEqual(10, tech.hpBonus);
            Assert.AreEqual(2, tech.armorBonus);
            Assert.AreEqual(1, tech.movementBonus);
            Assert.AreEqual(5, tech.attackBonus);
        }

        #endregion

        #region IsTechAvailable Tests

        [Test]
        public void IsTechAvailable_WithNoPrerequisites_ReturnsTrue()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_basic", "Basic Tech", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);

            // Act
            var result = techManager.IsTechAvailable(tech);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsTechAvailable_WithUnmetPrerequisites_ReturnsFalse()
        {
            // Arrange
            var prereq = DefinitionsFactory.CreateTech("prereq", "Prerequisite", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var tech = DefinitionsFactory.CreateTech("tech_advanced", "Advanced Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq);

            techManager.allTechs.Add(prereq);
            techManager.allTechs.Add(tech);

            // Act
            var result = techManager.IsTechAvailable(tech);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTechAvailable_WithMetPrerequisites_ReturnsTrue()
        {
            // Arrange
            var prereq = DefinitionsFactory.CreateTech("prereq", "Prerequisite", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var tech = DefinitionsFactory.CreateTech("tech_advanced", "Advanced Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq);

            techManager.allTechs.Add(prereq);
            techManager.allTechs.Add(tech);
            techManager.researchedTechs.Add(prereq);

            // Act
            var result = techManager.IsTechAvailable(tech);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsTechAvailable_WithMultiplePrerequisites_RequiresAllMet()
        {
            // Arrange
            var prereq1 = DefinitionsFactory.CreateTech("prereq1", "Prerequisite 1", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var prereq2 = DefinitionsFactory.CreateTech("prereq2", "Prerequisite 2", 50, TechGeneration.Gen0, TechCategory.Power, "Desc");
            var tech = DefinitionsFactory.CreateTech("tech_advanced", "Advanced Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq1, prereq2);

            techManager.allTechs.Add(prereq1);
            techManager.allTechs.Add(prereq2);
            techManager.allTechs.Add(tech);
            techManager.researchedTechs.Add(prereq1); // Only one prerequisite met

            // Act
            var result = techManager.IsTechAvailable(tech);

            // Assert - Should be false because not all prerequisites are met
            Assert.IsFalse(result);

            // Now add the second prerequisite
            techManager.researchedTechs.Add(prereq2);
            result = techManager.IsTechAvailable(tech);

            // Assert - Should be true now
            Assert.IsTrue(result);
        }

        [Test]
        public void IsTechAvailable_WithAlreadyResearched_ReturnsFalse()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_basic", "Basic Tech", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);
            techManager.researchedTechs.Add(tech);

            // Act
            var result = techManager.IsTechAvailable(tech);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTechAvailable_WithNullTech_ReturnsFalse()
        {
            // Act
            var result = techManager.IsTechAvailable(null);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Era Transition Tests

        [Test]
        public void CompleteCurrentTech_WithEraTransitionFlag_AdvancesGeneration()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_era", "Era Tech", 100, TechGeneration.Gen0, TechCategory.Special, "Desc", allowsEraTransition: true);
            techManager.currentGeneration = TechGeneration.Gen0;
            techManager.currentResearch = tech;
            techManager.scienceProgress = tech.costScience;

            bool eraTransitioned = false;
            TechGeneration newGeneration = TechGeneration.Gen0;
            techManager.OnEraTransition += (gen) => { eraTransitioned = true; newGeneration = gen; };

            // Act
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert
            Assert.IsTrue(eraTransitioned);
            Assert.AreEqual(TechGeneration.Gen1, newGeneration);
            Assert.AreEqual(TechGeneration.Gen1, techManager.currentGeneration);
        }

        [Test]
        public void CompleteCurrentTech_WithoutEraTransitionFlag_DoesNotAdvanceGeneration()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_normal", "Normal Tech", 100, TechGeneration.Gen0, TechCategory.Mecha, "Desc", allowsEraTransition: false);
            techManager.currentGeneration = TechGeneration.Gen0;
            techManager.currentResearch = tech;
            techManager.scienceProgress = tech.costScience;

            // Act
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert
            Assert.AreEqual(TechGeneration.Gen0, techManager.currentGeneration);
        }

        [Test]
        public void CompleteCurrentTech_WithWrongGenerationTech_DoesNotAdvanceGeneration()
        {
            // Arrange - Tech is Gen1 but current generation is Gen0
            var tech = DefinitionsFactory.CreateTech("tech_wrong_gen", "Wrong Gen Tech", 100, TechGeneration.Gen1, TechCategory.Special, "Desc", allowsEraTransition: true);
            techManager.currentGeneration = TechGeneration.Gen0;
            techManager.currentResearch = tech;
            techManager.scienceProgress = tech.costScience;

            // Act
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert - Should not advance because tech generation doesn't match current generation
            Assert.AreEqual(TechGeneration.Gen0, techManager.currentGeneration);
        }

        #endregion

        #region GetTechsByGeneration Tests

        [Test]
        public void GetTechsByGeneration_ReturnsOnlyMatchingGeneration()
        {
            // Arrange
            var gen0Tech1 = DefinitionsFactory.CreateTech("tech_gen0_1", "Gen0 Tech 1", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var gen0Tech2 = DefinitionsFactory.CreateTech("tech_gen0_2", "Gen0 Tech 2", 50, TechGeneration.Gen0, TechCategory.Power, "Desc");
            var gen1Tech = DefinitionsFactory.CreateTech("tech_gen1", "Gen1 Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc");
            var gen2Tech = DefinitionsFactory.CreateTech("tech_gen2", "Gen2 Tech", 150, TechGeneration.Gen2, TechCategory.Power, "Desc");

            techManager.allTechs.Add(gen0Tech1);
            techManager.allTechs.Add(gen0Tech2);
            techManager.allTechs.Add(gen1Tech);
            techManager.allTechs.Add(gen2Tech);

            // Act
            var gen0Techs = techManager.GetTechsByGeneration(TechGeneration.Gen0);
            var gen1Techs = techManager.GetTechsByGeneration(TechGeneration.Gen1);

            // Assert
            Assert.AreEqual(2, gen0Techs.Count);
            Assert.Contains(gen0Tech1, gen0Techs);
            Assert.Contains(gen0Tech2, gen0Techs);
            Assert.AreEqual(1, gen1Techs.Count);
            Assert.Contains(gen1Tech, gen1Techs);
        }

        [Test]
        public void GetTechsByGeneration_WithNoMatchingTechs_ReturnsEmptyList()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_gen0", "Gen0 Tech", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);

            // Act
            var gen3Techs = techManager.GetTechsByGeneration(TechGeneration.Gen3);

            // Assert
            Assert.IsNotNull(gen3Techs);
            Assert.AreEqual(0, gen3Techs.Count);
        }

        #endregion

        #region GetTechsByCategory Tests

        [Test]
        public void GetTechsByCategory_ReturnsOnlyMatchingCategory()
        {
            // Arrange
            var militaryTech1 = DefinitionsFactory.CreateTech("tech_mil1", "Military Tech 1", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var militaryTech2 = DefinitionsFactory.CreateTech("tech_mil2", "Military Tech 2", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc");
            var scienceTech = DefinitionsFactory.CreateTech("tech_sci", "Science Tech", 50, TechGeneration.Gen0, TechCategory.Power, "Desc");
            var infraTech = DefinitionsFactory.CreateTech("tech_infra", "Infrastructure Tech", 75, TechGeneration.Gen0, TechCategory.Power, "Desc");

            techManager.allTechs.Add(militaryTech1);
            techManager.allTechs.Add(militaryTech2);
            techManager.allTechs.Add(scienceTech);
            techManager.allTechs.Add(infraTech);

            // Act
            var militaryTechs = techManager.GetTechsByCategory(TechCategory.Mecha);
            var scienceTechs = techManager.GetTechsByCategory(TechCategory.Power);

            // Assert
            Assert.AreEqual(2, militaryTechs.Count);
            Assert.Contains(militaryTech1, militaryTechs);
            Assert.Contains(militaryTech2, militaryTechs);
            Assert.AreEqual(1, scienceTechs.Count);
            Assert.Contains(scienceTech, scienceTechs);
        }

        [Test]
        public void GetTechsByCategory_WithNoMatchingTechs_ReturnsEmptyList()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_mil", "Military Tech", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);

            // Act
            var specialTechs = techManager.GetTechsByCategory(TechCategory.Special);

            // Assert
            Assert.IsNotNull(specialTechs);
            Assert.AreEqual(0, specialTechs.Count);
        }

        #endregion

        #region UpdateAvailableTechs Tests

        [Test]
        public void UpdateAvailableTechs_AddsOnlyAvailableTechs()
        {
            // Arrange
            var tech1 = DefinitionsFactory.CreateTech("tech1", "Tech 1", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var prereq = DefinitionsFactory.CreateTech("prereq", "Prerequisite", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var tech2 = DefinitionsFactory.CreateTech("tech2", "Tech 2", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq);

            techManager.allTechs.Add(tech1);
            techManager.allTechs.Add(prereq);
            techManager.allTechs.Add(tech2);

            // Act
            techManager.UpdateAvailableTechs();

            // Assert - Only tech1 and prereq should be available (tech2 has unmet prerequisites)
            Assert.AreEqual(2, techManager.availableTechs.Count);
            Assert.Contains(tech1, techManager.availableTechs);
            Assert.Contains(prereq, techManager.availableTechs);
            Assert.IsFalse(techManager.availableTechs.Contains(tech2));
        }

        [Test]
        public void UpdateAvailableTechs_AfterResearch_UpdatesAvailability()
        {
            // Arrange
            var prereq = DefinitionsFactory.CreateTech("prereq", "Prerequisite", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq);

            techManager.allTechs.Add(prereq);
            techManager.allTechs.Add(tech);
            techManager.UpdateAvailableTechs();

            // Initially tech should not be available
            Assert.IsFalse(techManager.availableTechs.Contains(tech));

            // Research the prerequisite
            techManager.researchedTechs.Add(prereq);
            techManager.UpdateAvailableTechs();

            // Now tech should be available
            Assert.IsTrue(techManager.availableTechs.Contains(tech));
        }

        [Test]
        public void UpdateAvailableTechs_ExcludesAlreadyResearched()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);
            techManager.researchedTechs.Add(tech);

            // Act
            techManager.UpdateAvailableTechs();

            // Assert
            Assert.IsFalse(techManager.availableTechs.Contains(tech));
        }

        #endregion

        #region CompleteCurrentTech Integration Tests

        [Test]
        public void CompleteCurrentTech_AddsToResearchedTechs()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 100, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.currentResearch = tech;
            techManager.scienceProgress = tech.costScience;

            // Act
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert
            Assert.Contains(tech, techManager.researchedTechs);
        }

        [Test]
        public void CompleteCurrentTech_CallsUpdateAvailableTechs()
        {
            // Arrange
            var prereq = DefinitionsFactory.CreateTech("prereq", "Prerequisite", 50, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 100, TechGeneration.Gen1, TechCategory.Mecha, "Desc")
                .WithPrerequisites(prereq);

            techManager.allTechs.Add(prereq);
            techManager.allTechs.Add(tech);
            techManager.currentResearch = prereq;
            techManager.scienceProgress = prereq.costScience;

            // Initially tech should not be available
            techManager.UpdateAvailableTechs();
            Assert.IsFalse(techManager.availableTechs.Contains(tech));

            // Act - Complete the prerequisite
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert - Tech should now be available
            Assert.IsTrue(techManager.availableTechs.Contains(tech));
        }

        [Test]
        public void CompleteCurrentTech_RemovesFromAvailableTechs()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 100, TechGeneration.Gen0, TechCategory.Mecha, "Desc");
            techManager.allTechs.Add(tech);
            techManager.availableTechs.Add(tech);
            techManager.currentResearch = tech;
            techManager.scienceProgress = tech.costScience;

            // Act
            techManager.AddScience(0); // Triggers CompleteCurrentTech

            // Assert
            Assert.IsFalse(techManager.availableTechs.Contains(tech));
        }

        #endregion
    }
}
