using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Robotech.TBS.Data;

namespace Robotech.TBS.Systems
{
    public class TechManager : MonoBehaviour
    {
        public List<TechDefinition> techTree = new();
        public TechDefinition currentResearch;
        public int scienceProgress;

        // New fields for Phase 1
        public List<TechDefinition> allTechs = new();
        public List<TechDefinition> availableTechs = new();
        public List<TechDefinition> researchedTechs = new();
        public TechGeneration currentGeneration = TechGeneration.Gen0;

        public System.Action<TechDefinition> OnTechCompleted;
        public System.Action<TechGeneration> OnEraTransition;

        // Data-driven unlock queries (replaces deprecated boolean flags)

        /// <summary>
        /// Check if a specific unit has been unlocked by any researched technology.
        /// </summary>
        public bool IsUnitUnlocked(UnitDefinition unitDef)
        {
            if (unitDef == null) return false;
            foreach (var tech in researchedTechs)
            {
                if (tech.unlocksUnits != null && tech.unlocksUnits.Contains(unitDef))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a specific ability has been unlocked by any researched technology.
        /// </summary>
        public bool IsAbilityUnlocked(AbilityDefinition abilityDef)
        {
            if (abilityDef == null) return false;
            foreach (var tech in researchedTechs)
            {
                if (tech.unlocksAbilities != null && tech.unlocksAbilities.Contains(abilityDef))
                    return true;
            }
            return false;
        }

        public void AddScience(int amount)
        {
            if (currentResearch == null) return;
            scienceProgress += amount;
            if (scienceProgress >= currentResearch.costScience)
            {
                CompleteCurrentTech();
            }
        }

        public void SetResearch(TechDefinition tech)
        {
            currentResearch = tech;
            scienceProgress = 0;
        }

        void CompleteCurrentTech()
        {
            if (currentResearch == null) return;

            // Add to researched techs
            if (!researchedTechs.Contains(currentResearch))
            {
                researchedTechs.Add(currentResearch);
            }

            // Apply retroactive unit upgrades if tech has unit stat bonuses
            if (currentResearch.hpBonus > 0 || currentResearch.armorBonus > 0 ||
                currentResearch.movementBonus > 0 || currentResearch.attackBonus > 0)
            {
                // Use UnitRegistry for efficient unit lookup
                if (UnitRegistry.Instance != null)
                {
                    int upgradeCount = 0;
                    foreach (var unit in UnitRegistry.Instance.GetAllUnits())
                    {
                        if (!unit.HasTechUpgrade(currentResearch))
                        {
                            unit.ApplyTechUpgrade(currentResearch);
                            upgradeCount++;
                        }
                    }

                    if (upgradeCount > 0)
                    {
                        Debug.Log($"All {upgradeCount} units upgraded by {currentResearch.displayName}");
                    }
                }
            }

            // Handle era transition
            if (currentResearch.allowsEraTransition && currentResearch.generation == currentGeneration)
            {
                currentGeneration = (TechGeneration)((int)currentGeneration + 1);
                OnEraTransition?.Invoke(currentGeneration);
            }

            OnTechCompleted?.Invoke(currentResearch);

            // Remove from tree and available techs, clear current research
            techTree.Remove(currentResearch);
            availableTechs.Remove(currentResearch);
            currentResearch = null;
            scienceProgress = 0;

            // Update available techs after completion
            UpdateAvailableTechs();
        }

        public bool IsTechAvailable(TechDefinition tech)
        {
            if (tech == null) return false;
            if (researchedTechs.Contains(tech)) return false;

            // Check if all prerequisites are researched
            if (tech.prerequisites != null && tech.prerequisites.Count > 0)
            {
                foreach (var prereq in tech.prerequisites)
                {
                    if (prereq != null && !researchedTechs.Contains(prereq))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<TechDefinition> GetTechsByGeneration(TechGeneration gen)
        {
            return allTechs.Where(t => t.generation == gen).ToList();
        }

        public List<TechDefinition> GetTechsByCategory(TechCategory cat)
        {
            return allTechs.Where(t => t.category == cat).ToList();
        }

        public void UpdateAvailableTechs()
        {
            availableTechs.Clear();
            foreach (var tech in allTechs)
            {
                if (IsTechAvailable(tech))
                {
                    availableTechs.Add(tech);
                }
            }
        }
    }
}
