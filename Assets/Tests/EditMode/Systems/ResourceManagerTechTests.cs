using NUnit.Framework;
using UnityEngine;
using Robotech.TBS.Systems;
using Robotech.TBS.Data;
using Robotech.TBS.Bootstrap;

namespace Robotech.TBS.Tests.EditMode.Systems
{
    [TestFixture]
    public class ResourceManagerTechTests
    {
        private GameObject resourceManagerObject;
        private ResourceManager resourceManager;

        [SetUp]
        public void SetUp()
        {
            resourceManagerObject = new GameObject("ResourceManager");
            resourceManager = resourceManagerObject.AddComponent<ResourceManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(resourceManagerObject);
        }

        #region Getter/Setter Tests

        [Test]
        public void SetProtocultureBonus_SetsCorrectValue()
        {
            // Arrange & Act
            resourceManager.SetProtocultureBonus(10f);

            // Assert
            Assert.AreEqual(10f, resourceManager.GetProtocultureBonus());
        }

        [Test]
        public void SetScienceBonus_SetsCorrectValue()
        {
            // Arrange & Act
            resourceManager.SetScienceBonus(15f);

            // Assert
            Assert.AreEqual(15f, resourceManager.GetScienceBonus());
        }

        [Test]
        public void SetProductionBonus_SetsCorrectValue()
        {
            // Arrange & Act
            resourceManager.SetProductionBonus(20f);

            // Assert
            Assert.AreEqual(20f, resourceManager.GetProductionBonus());
        }

        [Test]
        public void GetProtocultureBonus_InitiallyZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.GetProtocultureBonus());
        }

        [Test]
        public void GetScienceBonus_InitiallyZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.GetScienceBonus());
        }

        [Test]
        public void GetProductionBonus_InitiallyZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.GetProductionBonus());
        }

        #endregion

        #region ApplyTechBonus Tests

        [Test]
        public void ApplyTechBonus_WithProtoculturePerTurn_AddsCorrectAmount()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_proto", "Protoculture Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 0, production: 0);

            // Act
            resourceManager.ApplyTechBonus(tech);

            // Assert
            Assert.AreEqual(5f, resourceManager.GetProtocultureBonus());
        }

        [Test]
        public void ApplyTechBonus_WithSciencePerTurn_AddsCorrectAmount()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_sci", "Science Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 0, science: 10, production: 0);

            // Act
            resourceManager.ApplyTechBonus(tech);

            // Assert
            Assert.AreEqual(10f, resourceManager.GetScienceBonus());
        }

        [Test]
        public void ApplyTechBonus_WithProductionPerTurn_AddsCorrectAmount()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_prod", "Production Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 0, science: 0, production: 15);

            // Act
            resourceManager.ApplyTechBonus(tech);

            // Assert
            Assert.AreEqual(15f, resourceManager.GetProductionBonus());
        }

        [Test]
        public void ApplyTechBonus_WithMultipleBonuses_AddsAllCorrectly()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_multi", "Multi Bonus Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);

            // Act
            resourceManager.ApplyTechBonus(tech);

            // Assert
            Assert.AreEqual(5f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(10f, resourceManager.GetScienceBonus());
            Assert.AreEqual(15f, resourceManager.GetProductionBonus());
        }

        [Test]
        public void ApplyTechBonus_WithZeroBonuses_DoesNotChangeBonuses()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_zero", "Zero Bonus Tech", 100, TechGeneration.Gen0, TechCategory.Mecha, "Desc")
                .WithYieldBonus(protoculture: 0, science: 0, production: 0);

            // Act
            resourceManager.ApplyTechBonus(tech);

            // Assert
            Assert.AreEqual(0f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(0f, resourceManager.GetScienceBonus());
            Assert.AreEqual(0f, resourceManager.GetProductionBonus());
        }

        [Test]
        public void ApplyTechBonus_WithNullTech_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => resourceManager.ApplyTechBonus(null));
            Assert.AreEqual(0f, resourceManager.GetProtocultureBonus());
        }

        [Test]
        public void ApplyTechBonus_MultipleTimes_BonusesStack()
        {
            // Arrange
            var tech1 = DefinitionsFactory.CreateTech("tech1", "Tech 1", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);
            var tech2 = DefinitionsFactory.CreateTech("tech2", "Tech 2", 100, TechGeneration.Gen1, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 3, science: 7, production: 12);

            // Act
            resourceManager.ApplyTechBonus(tech1);
            resourceManager.ApplyTechBonus(tech2);

            // Assert
            Assert.AreEqual(8f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(17f, resourceManager.GetScienceBonus());
            Assert.AreEqual(27f, resourceManager.GetProductionBonus());
        }

        [Test]
        public void ApplyTechBonus_SameTechTwice_BonusesStackIncorrectly()
        {
            // Arrange
            // Note: This test documents current behavior - bonuses would stack if tech is researched twice
            // In practice, TechManager prevents researching same tech twice
            var tech = DefinitionsFactory.CreateTech("tech", "Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);

            // Act
            resourceManager.ApplyTechBonus(tech);
            resourceManager.ApplyTechBonus(tech);

            // Assert - Bonuses do stack (but TechManager should prevent this scenario)
            Assert.AreEqual(10f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(20f, resourceManager.GetScienceBonus());
            Assert.AreEqual(30f, resourceManager.GetProductionBonus());
        }

        #endregion

        #region Calculate Total Per Turn Tests

        [Test]
        public void CalculateTotalProtoculturePerTurn_WithNoBonuses_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.CalculateTotalProtoculturePerTurn());
        }

        [Test]
        public void CalculateTotalProtoculturePerTurn_WithBonus_ReturnsCorrectTotal()
        {
            // Arrange
            resourceManager.SetProtocultureBonus(10f);

            // Act
            var total = resourceManager.CalculateTotalProtoculturePerTurn();

            // Assert
            Assert.AreEqual(10f, total);
        }

        [Test]
        public void CalculateTotalSciencePerTurn_WithNoBonuses_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.CalculateTotalSciencePerTurn());
        }

        [Test]
        public void CalculateTotalSciencePerTurn_WithBonus_ReturnsCorrectTotal()
        {
            // Arrange
            resourceManager.SetScienceBonus(15f);

            // Act
            var total = resourceManager.CalculateTotalSciencePerTurn();

            // Assert
            Assert.AreEqual(15f, total);
        }

        [Test]
        public void CalculateTotalProductionPerTurn_WithNoBonuses_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0f, resourceManager.CalculateTotalProductionPerTurn());
        }

        [Test]
        public void CalculateTotalProductionPerTurn_WithBonus_ReturnsCorrectTotal()
        {
            // Arrange
            resourceManager.SetProductionBonus(20f);

            // Act
            var total = resourceManager.CalculateTotalProductionPerTurn();

            // Assert
            Assert.AreEqual(20f, total);
        }

        [Test]
        public void CalculateTotalPerTurn_WithMultipleTechBonuses_ReturnsSum()
        {
            // Arrange
            var tech1 = DefinitionsFactory.CreateTech("tech1", "Tech 1", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);
            var tech2 = DefinitionsFactory.CreateTech("tech2", "Tech 2", 100, TechGeneration.Gen1, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 3, science: 7, production: 12);

            resourceManager.ApplyTechBonus(tech1);
            resourceManager.ApplyTechBonus(tech2);

            // Act
            var protoTotal = resourceManager.CalculateTotalProtoculturePerTurn();
            var sciTotal = resourceManager.CalculateTotalSciencePerTurn();
            var prodTotal = resourceManager.CalculateTotalProductionPerTurn();

            // Assert
            Assert.AreEqual(8f, protoTotal);
            Assert.AreEqual(17f, sciTotal);
            Assert.AreEqual(27f, prodTotal);
        }

        #endregion

        #region ApplyIncome Tests

        [Test]
        public void ApplyIncome_WithNoBonuses_DoesNotChangeResources()
        {
            // Arrange
            int initialProtocolture = resourceManager.protoculture;
            int initialScience = resourceManager.science;
            int initialMaterials = resourceManager.materials;

            // Act
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(initialProtocolture, resourceManager.protoculture);
            Assert.AreEqual(initialScience, resourceManager.science);
            Assert.AreEqual(initialMaterials, resourceManager.materials);
        }

        [Test]
        public void ApplyIncome_WithProtocultureBonus_IncreasesProtocolture()
        {
            // Arrange
            int initialProtocolture = resourceManager.protoculture;
            resourceManager.SetProtocultureBonus(5f);

            // Act
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(initialProtocolture + 5, resourceManager.protoculture);
        }

        [Test]
        public void ApplyIncome_WithScienceBonus_IncreasesScience()
        {
            // Arrange
            int initialScience = resourceManager.science;
            resourceManager.SetScienceBonus(10f);

            // Act
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(initialScience + 10, resourceManager.science);
        }

        [Test]
        public void ApplyIncome_WithProductionBonus_IncreasesMaterials()
        {
            // Arrange
            int initialMaterials = resourceManager.materials;
            resourceManager.SetProductionBonus(15f);

            // Act
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(initialMaterials + 15, resourceManager.materials);
        }

        [Test]
        public void ApplyIncome_WithAllBonuses_IncreasesAllResources()
        {
            // Arrange
            int initialProtocolture = resourceManager.protoculture;
            int initialScience = resourceManager.science;
            int initialMaterials = resourceManager.materials;

            resourceManager.SetProtocultureBonus(5f);
            resourceManager.SetScienceBonus(10f);
            resourceManager.SetProductionBonus(15f);

            // Act
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(initialProtocolture + 5, resourceManager.protoculture);
            Assert.AreEqual(initialScience + 10, resourceManager.science);
            Assert.AreEqual(initialMaterials + 15, resourceManager.materials);
        }

        [Test]
        public void ApplyIncome_RoundsFloatBonuses()
        {
            // Arrange
            int initialProtocolture = resourceManager.protoculture;
            resourceManager.SetProtocultureBonus(5.7f);

            // Act
            resourceManager.ApplyIncome();

            // Assert - Should round to nearest int (6)
            Assert.AreEqual(initialProtocolture + 6, resourceManager.protoculture);
        }

        [Test]
        public void ApplyIncome_MultipleCalls_StacksBonuses()
        {
            // Arrange
            int initialProtocolture = resourceManager.protoculture;
            resourceManager.SetProtocultureBonus(5f);

            // Act
            resourceManager.ApplyIncome();
            resourceManager.ApplyIncome();

            // Assert - Bonuses should apply twice
            Assert.AreEqual(initialProtocolture + 10, resourceManager.protoculture);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_TechCompletion_AppliesBonusAndIncome()
        {
            // Arrange
            var tech = DefinitionsFactory.CreateTech("tech_integration", "Integration Tech", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);

            int initialProtocolture = resourceManager.protoculture;
            int initialScience = resourceManager.science;
            int initialMaterials = resourceManager.materials;

            // Act - Simulate tech completion
            resourceManager.ApplyTechBonus(tech);
            resourceManager.ApplyIncome();

            // Assert
            Assert.AreEqual(5f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(10f, resourceManager.GetScienceBonus());
            Assert.AreEqual(15f, resourceManager.GetProductionBonus());

            Assert.AreEqual(initialProtocolture + 5, resourceManager.protoculture);
            Assert.AreEqual(initialScience + 10, resourceManager.science);
            Assert.AreEqual(initialMaterials + 15, resourceManager.materials);
        }

        [Test]
        public void Integration_MultipleTechs_StackBonusesCorrectly()
        {
            // Arrange
            var tech1 = DefinitionsFactory.CreateTech("tech1", "Tech 1", 100, TechGeneration.Gen0, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 5, science: 10, production: 15);
            var tech2 = DefinitionsFactory.CreateTech("tech2", "Tech 2", 100, TechGeneration.Gen1, TechCategory.Power, "Desc")
                .WithYieldBonus(protoculture: 3, science: 7, production: 12);

            int initialProtocolture = resourceManager.protoculture;
            int initialScience = resourceManager.science;
            int initialMaterials = resourceManager.materials;

            // Act
            resourceManager.ApplyTechBonus(tech1);
            resourceManager.ApplyTechBonus(tech2);
            resourceManager.ApplyIncome();

            // Assert - Bonuses should stack
            Assert.AreEqual(8f, resourceManager.GetProtocultureBonus());
            Assert.AreEqual(17f, resourceManager.GetScienceBonus());
            Assert.AreEqual(27f, resourceManager.GetProductionBonus());

            Assert.AreEqual(initialProtocolture + 8, resourceManager.protoculture);
            Assert.AreEqual(initialScience + 17, resourceManager.science);
            Assert.AreEqual(initialMaterials + 27, resourceManager.materials);
        }

        #endregion
    }
}
