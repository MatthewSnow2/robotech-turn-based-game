# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-based turn-based strategy game (Civ6-inspired) set in the Robotech Macross universe. Single-player skirmish mode with RDF vs Zentradi factions. Non-commercial fan project.

**Unity Version:** 2022.3.62f3 (LTS)
**Platform:** Windows, mouse/keyboard
**Status:** Mid-prototype. Core systems shipped (hex grid, turns, units, cities, fog, tech tree Gen 0-1, resources, basic + ranged combat, A* pathfinding, AI opponent, line-of-sight, terrain cover). Tech Tree UI and ability/counter-attack mechanics are the next major work. See `BLUEPRINT.md` for the active phase plan.

## Development Commands

### Running the Game
```bash
# Open in Unity Editor (via Unity Hub)
# Load scene: Assets/Scenes/Bootstrap.unity
# Press Play in Unity Editor
# Console should log: "Robotech TBS Bootstrap initialized."
```

### Testing
```bash
# EditMode tests only (no Play mode tests yet)
# Run via Unity Test Runner window: Window > General > Test Runner
# Or via command line (example):
unity-editor -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

### CI
GitHub Actions workflow runs EditMode tests on push/PR. Windows build job is scaffolded but requires `UNITY_LICENSE` secret to be configured.

## Architecture Overview

### Core Architectural Patterns

**Event-Driven Design**
- Static events for decoupled communication between systems
- Key events: `TurnManager.OnTurnStarted`, `SelectionController.OnUnitSelected`, `TurnManager.OnPhaseChanged`
- Subscribers in UI, fog of war, resource systems

**Data-Driven with ScriptableObjects**
- All game balance in ScriptableObjects (created at runtime via factories during prototyping)
- Data layer (definitions) separated from system layer (MonoBehaviours) and view layer (rendering/UI)
- Definition types: `UnitDefinition`, `WeaponDefinition`, `TerrainType`, `TechDefinition` (+ `TechCategory`, `TechGeneration` enums), `DistrictDefinition`, `AbilityDefinition` (placeholder, full impl pending)

**Registry Pattern**
- `UnitRegistry` (Assets/Scripts/Systems/UnitRegistry.cs): singleton, O(1) lookups by position and by faction. Replaces `FindObjectsOfType` calls in hot paths (UIShell, combat, AI).

**Factory Pattern**
- `DefinitionsFactory` (Assets/Scripts/Bootstrap/DefinitionsFactory.cs): Creates ScriptableObject definitions at runtime
- `UnitFactory` (Assets/Scripts/Bootstrap/UnitFactory.cs): Spawns unit GameObjects with proper initialization

**Hex Grid System**
- Axial coordinate system: `HexCoord(q, r)` where s = -q-r
- Pointy-top orientation
- Grid managed by `HexGrid` (Assets/Scripts/Hex/HexGrid.cs)
- Math utilities in `HexMath` for coordinate conversion, distance, neighbors, range calculations

### Initialization Flow

1. Bootstrap scene loads (`Assets/Scenes/Bootstrap.unity`)
2. `GameBootstrap.Awake()` (Assets/Scripts/Bootstrap/GameBootstrap.cs):
   - Ensures all core systems exist as components
   - Creates terrain definitions via `DefinitionsFactory`
   - Generates map via `MapGenerator.Generate()`
   - Creates weapon/unit definitions
   - Spawns starting units (VF-1A, Tactical Pod, settlers)
   - Initializes fog of war from unit vision
   - Sets up tech tree
3. `TurnManager.Start()` fires initial turn events and game loop begins

### Key Systems

**Turn Management** (Assets/Scripts/Core/TurnManager.cs)
- Turn cycle: Player phase → AI phase → increment turn counter → repeat
- Events fired at each phase transition (`OnTurnStarted`, `OnPhaseChanged`)
- AI phase runs `AIController.ExecuteAIPhase()` coroutine; configurable `aiThinkingDelay`
- Non-recursive `EndPhase`: coroutine-based phase transitions (no stack risk)

**Hex Grid** (Assets/Scripts/Hex/)
- `HexCoord`: Axial coordinate struct with distance, neighbors, range calculations
- `HexGrid`: Grid management, bounds checking
- `HexMath`: World position conversion, coordinate math, `LineBetween(a, b)` cube-lerp hex line
- `Pathfinder`: A* pathfinding with terrain movement costs, returns `PathResult` (path + total cost + reachable hexes for a budget). Honors impassable terrain and other units.
- Grid sizes: 40x24 (small), 60x36 (medium), 80x48 (large)

**Unit System** (Assets/Scripts/Units/Unit.cs)
- Tracks HP, armor, movement points, position, faction, definition reference
- Movement: multi-hex via A*. `MoveTo(target, hexSize)` single-step, `MoveAlongPath(path, hexSize, mapGen)` consumes movement points per hex using terrain cost.
- Events for unit state changes (HP, position, death)

**City/Settlement System** (Assets/Scripts/Cities/)
- `City.cs`: Territory ownership, production queue, district system, yield calculation
- `CityManager.cs`: Global city lifecycle, territory management
- Districts: Factory (production), Lab (science), Outpost (vision/defense)
- Production queue automatically spawns units when complete
- Initial territory: city center + 1 ring of hexes

**Resource Management** (Assets/Scripts/Systems/ResourceManager.cs)
- Tracked resources: Protoculture (upkeep), Materials (production), Credits, Science
- `ApplyIncome()` runs per-turn from city yields plus tech bonuses (protoculture/science/production)
- Unit upkeep deducted from Protoculture each turn, per faction

**Technology Tree** (Assets/Scripts/Systems/TechManager.cs)
- Linear research (one tech at a time), prerequisite-gated
- Tracks `allTechs` / `availableTechs` / `researchedTechs` and `currentGeneration`
- Era transitions on completing critical-path techs (Gen 0 → Gen 1 → ...)
- Tech effects: stat bonuses applied to ResourceManager + Unit on completion
- Unit production validated against `UnitDefinition.requiredTech`
- Gen 0-1 (16 techs) defined in `GameBootstrap`; Gen 2-4 + Tech Tree UI pending

**Combat System** (Assets/Scripts/Combat/)
- `CombatResolver`: static resolver — `ResolveAttack(attacker, target, mapGen)`, `CanAttack(attacker, target, mapGen)`, `GetMaxRange(unit)`
- Weapon-based damage with salvo accuracy rolls; armor reduces damage before HP loss
- Ranged combat: respects each weapon's max range and `GetMaxRange(unit)` across its loadout
- `LineOfSight.HasLineOfSight(a, b, mapGen)`: walks `HexMath.LineBetween`; intermediate hexes block if `terrain.providesElevation` (Hills, Mountains). Same-hex and adjacent always true. Predicate overload available for unit tests without scene wiring.
- Terrain cover: target hex `defenseBonus` applied as flat damage reduction before `Unit.TakeDamage`
- No friendly fire; tech bonuses apply to attacker damage

**Fog of War** (Assets/Scripts/Fog/FogOfWarSystem.cs)
- Dual-state visibility:
  - **Seen**: Revealed at least once (permanent, shows terrain)
  - **Visible**: Currently visible (updated each turn based on unit vision)
- `RevealFrom()` expands visibility by vision radius

**Map Generation** (Assets/Scripts/Map/MapGenerator.cs)
- Procedural terrain assignment with configurable probabilities
- Terrain types: Plains, Forest, Hills, Mountains, Desert, Tundra, Marsh, Urban, Coast, Ocean
- Terrain properties: movement cost, defense bonus, yields, flags (water, impassable, elevation, urban)

**Input/Selection** (Assets/Scripts/Input/SelectionController.cs)
- Mouse-based unit selection via raycast
- Calculates reachable hexes (A* with movement budget) and attackable hexes (range + LoS)
- Player-phase + faction guards prevent input during AI phase or on enemy units
- Hotkey 'B': Found city (if settler unit selected)

**AI Opponent** (Assets/Scripts/AI/AIController.cs)
- Runs during AI phase as a coroutine; orchestrates movement, combat, city production, tech research
- Targeting: prioritizes weak/valuable enemy units
- Movement: A* pathfinding toward nearest enemy or objective; respects terrain costs and LoS
- City management: queues unit production based on tech availability
- Tech research: selects critical-path or bonus techs
- Emits `OnAIPhaseComplete` event when done; `aiFaction` configurable (default Zentradi)
- Note: settler founding logic not implemented (deferred)

### Namespace Organization

```
Robotech.TBS
├── Bootstrap       # Initialization, factories (GameBootstrap, DefinitionsFactory, UnitFactory)
├── Core            # Core game logic (TurnManager)
├── Hex             # Hex math & grid (HexCoord, HexGrid, HexMath, Pathfinder)
├── Map             # Map generation (MapGenerator)
├── Units           # Unit entity system (Unit)
├── Cities          # City system (City)
├── Systems         # Game systems (ResourceManager, TechManager, CityManager, UnitRegistry, MapRules)
├── Combat          # Combat mechanics (CombatResolver, LineOfSight)
├── Inputs          # Input handling (SelectionController)
├── Fog             # Fog of war (FogOfWarSystem) — pure-distance; does not yet honor LoS
├── Rendering       # Visualization (HexDebugRenderer)
├── Data            # Data definitions (UnitDefinition, WeaponDefinition, TerrainType, TechDefinition,
│                   #   TechCategory, TechGeneration, DistrictDefinition, AbilityDefinition)
├── UI              # UI components (UIShell — ~700 lines of procedural debug HUD)
├── AI              # AI opponent (AIController)
└── Debug           # Development utilities (DevHotkeys)
```

### Assembly Definitions

Single runtime assembly: `Robotech.Runtime` (Assets/Scripts/Bootstrap/Runtime.asmdef)
- Excludes Editor code
- Auto-referenced
- No unsafe code

## Important Design Notes

**Pragmatic Prototyping:**
- Units rendered as capsules with faction colors (RDF=blue, Zentradi=red)
- No imported assets yet; focus on gameplay mechanics first
- Debug visualization via Gizmos in Scene view

**ScriptableObject Strategy:**
- Currently created at runtime via factories (no .asset files yet)
- Enables rapid iteration without reimporting
- Designed for migration to persistent assets later

**Combat Mechanics:**
- Based on Robotech Tactics (Palladium) adapted for TBS
- No friendly fire
- Stateless resolver (static utility class); requires `MapGenerator` for LoS + terrain cover lookups
- Salvo accuracy rolls; tech bonuses applied to attacker damage
- Line-of-sight blocked only by terrain with `providesElevation=true` (Hills, Mountains). Forests/urban do not block sight in current iteration.

**Hex Coordinate System:**
- Pointy-top orientation (not flat-top)
- Axial coordinates (q, r) with derived s = -q-r for cube coordinate math
- Distance calculation uses cube coordinates for efficiency
- Always validate coordinates with `HexGrid.IsInBounds()` before use

## Legal Context

Non-commercial fan project. MIT license for code, but all Robotech/Palladium IP remains property of respective owners. No commercial use permitted. See LICENSE file for details.
