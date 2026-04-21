# Robotech TBS - Development Blueprint

## Project Status: Early Prototype

A Unity-based turn-based strategy game (Civ6-inspired) set in the Robotech Macross universe. Single-player skirmish mode with RDF vs Zentradi factions.

---

## Completed Systems

### Core Infrastructure
- [x] Hex grid with axial coordinates (pointy-top orientation)
- [x] Turn management (player/AI phases with events)
- [x] Event-driven architecture (static events for decoupled systems)
- [x] Data-driven design with ScriptableObjects
- [x] Factory pattern for runtime object creation

### Game Systems
- [x] Unit spawning and movement (simplified 1-hex steps)
- [x] City founding and territory expansion
- [x] Production queues with automatic unit spawning
- [x] Tech tree with prerequisites (16 Gen0-Gen1 techs)
- [x] Resource management (Protoculture, Materials, Credits, Science)
- [x] Basic combat (weapon-based damage, armor reduction)
- [x] Fog of war (dual-state: seen vs visible)
- [x] Debug UI shell (procedurally generated)

### Phase 3: Core Cleanup (Completed)
- [x] Fix enum duplication conflicts (TechCategory, TechGeneration)
- [x] Fix UIShell.cs corruption
- [x] Remove deprecated TechDefinition fields
- [x] Implement UnitRegistry for O(1) unit lookups
- [x] Replace FindObjectsOfType calls with UnitRegistry
- [x] Complete combat system (range validation, tech bonuses, friendly fire prevention)
- [x] Add missing factory methods (CreateDistrict, CreateAbility)

### Phase 4: AI Foundation (Completed)
- [x] Basic AI decision framework (AIController)
- [x] AI unit movement (move toward nearest enemy/objective)
- [x] AI combat decisions (prioritize weak/valuable targets)
- [x] AI city management (queue unit production)
- [x] AI tech research selection (prioritize critical path, bonuses)
- [x] Integrate AIController with TurnManager

### Phase 5: Movement & Pathfinding (Completed)
- [x] Multi-hex pathfinding (A* algorithm with terrain costs)
- [x] Apply terrain movement costs to pathfinding
- [x] Movement preview visualization (reachable hexes, path preview)
- [x] Unit movement with pathfinding (MoveAlongPath, MoveToTarget)
- [x] AI integration with pathfinding system
- [ ] Zone of control mechanics (deferred to future phase)

---

## Upcoming Phases

### Phase 6: Ranged Combat & Abilities
- [x] Line-of-sight calculations (HexMath.LineBetween + LineOfSight.HasLineOfSight; blocks on terrain.providesElevation)
- [x] Cover/terrain defense bonuses (target hex defenseBonus applied as flat damage reduction in CombatResolver before TakeDamage)
- [ ] Unit abilities implementation (overwatch, transform, etc.)
- [ ] Counter-attack mechanics

Notes for next pieces: fog of war is still pure-distance (does not respect LoS yet — deliberate, scope-limited). Forests and urban terrain do not block sight currently; only providesElevation (Hills, Mountains) does.

### Phase 7: City & Economy Depth
- [ ] District placement mechanics
- [ ] Tile yield extraction (worked tiles)
- [ ] Population/growth system
- [ ] Resource deficit consequences
- [ ] Trade routes (optional)

### Phase 8: Victory & Polish
- [ ] Victory conditions (conquest, tech, score)
- [ ] Defeat conditions
- [ ] Game over screen
- [ ] Basic animations (movement, combat)
- [ ] Sound effects
- [ ] Save/load system

---

## Future Considerations

- Multiplayer support
- Additional factions (Malcontent Zentradi, Southern Cross)
- Campaign mode
- Map editor
- Mod support

---

## Technical Debt & Known Issues

### Performance
- ~~FindObjectsOfType calls in hot paths~~ (Fixed: UnitRegistry)
- No object pooling for units/effects
- UI rebuilt every frame in some cases

### Incomplete Systems
- ~~Movement is 1-hex only~~ (Fixed: A* pathfinding with terrain costs)
- Districts can be added but no UI to place them
- Abilities defined but not implemented
- AI settler handling not implemented (founding cities)
- Zone of control mechanics not implemented

### Code Quality
- UIShell.cs is 600+ lines - needs refactoring
- Some magic numbers should be in configuration
- Legacy Input System (deprecated in Unity 2022.3)

---

## Architecture Notes

### Key Patterns
- **Event-Driven**: TurnManager.OnTurnStarted, OnPhaseChanged, etc.
- **Data-Driven**: All game balance in ScriptableObjects
- **Factory Pattern**: DefinitionsFactory, UnitFactory for runtime creation
- **Registry Pattern**: UnitRegistry for O(1) unit lookups

### Namespace Organization
```
Robotech.TBS
├── Bootstrap       # Initialization, factories
├── Core            # TurnManager
├── Hex             # HexCoord, HexGrid, HexMath, Pathfinder
├── Map             # MapGenerator, MapRules
├── Units           # Unit
├── Cities          # City
├── Systems         # ResourceManager, TechManager, CityManager, UnitRegistry
├── Combat          # CombatResolver
├── Inputs          # SelectionController
├── Fog             # FogOfWarSystem
├── Rendering       # HexDebugRenderer
├── Data            # Definition ScriptableObjects
├── UI              # UIShell
├── AI              # AIController
└── Debug           # DevHotkeys
```

---

## Testing

### Current Coverage
- Tech system (TechManager, TechDefinition) - comprehensive
- Resource management with tech bonuses
- Unit spawning with tech requirements
- Phase 2 integration tests

### Gaps
- Combat resolution tests
- HexMath edge cases
- WeaponDefinition validation
- TerrainType usage tests
