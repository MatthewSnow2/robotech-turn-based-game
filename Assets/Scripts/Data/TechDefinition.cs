using System.Collections.Generic;
using UnityEngine;

namespace Robotech.TBS.Data
{
    // Note: TechGeneration and TechCategory enums are defined in separate files:
    // - TechGeneration.cs (Gen0-Gen5)
    // - TechCategory.cs (Power, Mecha, Weapons, Defense, Aerospace, Special)

    [CreateAssetMenu(fileName = "TechDefinition", menuName = "Robotech/Data/Tech Definition", order = 10)]
    public class TechDefinition : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>
        /// Unique identifier for this technology.
        /// </summary>
        public string techId;

        /// <summary>
        /// Display name shown to the player.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Science cost required to research this technology.
        /// </summary>
        public int costScience = 50;

        /// <summary>
        /// Detailed description of the technology and its benefits.
        /// </summary>
        [TextArea]
        public string description;

        [Header("Classification")]
        /// <summary>
        /// The generation/era this technology belongs to.
        /// Determines tech tree progression and availability.
        /// </summary>
        public TechGeneration generation;

        /// <summary>
        /// The category or domain this technology falls under.
        /// Used for organizing the tech tree and thematic grouping.
        /// </summary>
        public TechCategory category;

        [Header("Prerequisites and Dependencies")]
        /// <summary>
        /// Technologies that must be researched before this one becomes available.
        /// All prerequisites must be completed to unlock this technology.
        /// </summary>
        public List<TechDefinition> prerequisites = new();

        [Header("Unlocks")]
        /// <summary>
        /// Unit types that become available when this technology is researched.
        /// </summary>
        public List<UnitDefinition> unlocksUnits = new();

        /// <summary>
        /// District types that become available when this technology is researched.
        /// </summary>
        public List<DistrictDefinition> unlocksDistricts = new();

        /// <summary>
        /// Abilities that become available when this technology is researched.
        /// </summary>
        public List<AbilityDefinition> unlocksAbilities = new();

        [Header("Yield Bonuses")]
        /// <summary>
        /// Passive protoculture yield bonus added per turn when this technology is researched.
        /// Applied globally or to specific cities depending on game design.
        /// </summary>
        public float protoculturePerTurn;

        /// <summary>
        /// Passive science yield bonus added per turn when this technology is researched.
        /// Can represent improved research efficiency or infrastructure.
        /// </summary>
        public float sciencePerTurn;

        /// <summary>
        /// Passive production yield bonus added per turn when this technology is researched.
        /// Represents improved manufacturing or construction capabilities.
        /// </summary>
        public float productionPerTurn;

        [Header("Unit Stat Bonuses")]
        /// <summary>
        /// Global HP bonus applied to units when this technology is researched.
        /// Can be flat bonus or percentage depending on implementation.
        /// </summary>
        public int hpBonus;

        /// <summary>
        /// Global armor bonus applied to units when this technology is researched.
        /// Improves damage reduction capabilities.
        /// </summary>
        public int armorBonus;

        /// <summary>
        /// Global movement bonus applied to units when this technology is researched.
        /// Increases tiles units can move per turn.
        /// </summary>
        public int movementBonus;

        /// <summary>
        /// Global attack bonus applied to units when this technology is researched.
        /// Increases damage output.
        /// </summary>
        public int attackBonus;

        [Header("Special Flags")]
        /// <summary>
        /// Marks this technology as part of the critical path for game progression.
        /// Critical path technologies may be required for victory conditions or era transitions.
        /// </summary>
        public bool isCriticalPath;

        /// <summary>
        /// Indicates whether completing this technology triggers an era transition.
        /// Era transitions may unlock new game mechanics or change the strategic landscape.
        /// </summary>
        public bool allowsEraTransition;

        [Header("Presentation")]
        /// <summary>
        /// Icon displayed in the tech tree UI and research panels.
        /// </summary>
        public Sprite icon;
    }
}
