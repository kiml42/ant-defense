# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Ant Defense** is a Unity tower-defense game where players defend a picnic from attacking ants. Ants autonomously forage for food via pheromone-like scent trails; players place defensive structures (turrets, walls, traps) to stop them.

**Environment:** Unity 6000.3.9f1, C#, physics-based 3D gameplay. No formal test suite — behavior is validated via play-testing.

## Building & Running

- Open `AntDefense/` as the Unity project folder in Unity Hub
- Unity version: `6000.3.9f1` (see `ProjectSettings/ProjectVersion.txt`)
- Main scenes: `Assets/Scenes/ProperMaze.unity`, `Assets/Scenes/SampleScene.unity`
- Build output goes to `/c/Projects/ant-defense/Builds/`

There is no CLI build command — use the Unity Editor or Unity Build Automation.

## Architecture

### Ant Intelligence (`AntStateMachine.cs`)
Central hub for ant behavior. States: `SeekingFood`, `ReturningToFood`, `ReportingFood`, `CarryingFood`, `ReturningHome`. State drives which scent trail the ant leaves and which targets it accepts. At 678 lines, it is marked for future refactoring.

### Scent System (`Smellable.cs`, `FoodSmell.cs`, `AntNestSmell.cs`, `ScentDetector.cs`)
Smell types: `Food` and `Home` (defined in `Smell` enum). `Smellable` is the abstract base for all targetable objects. `IsActual` distinguishes real objects from trail-point breadcrumbs. `ScentDetector` uses `OnTriggerEnter` collision events and calls `AntStateMachine.ProcessSmell()`.

### Trail System (`AntTrailController.cs`, `TrailPointController.cs`, `TrailPointManager.cs`)
Ants leave temporary trail points as breadcrumbs. Points decay over time; `OverlapRadius = 2f` prevents clustering. All trail points are parented to a single `TrailParent` GameObject to avoid hierarchy bloat.

### Ant Movement (`AntMoveController.cs`, `AntTargetPositionProvider.cs`)
Physics-based (Rigidbody forces, not kinematic). `StayUpright.cs` applies upright torque. Emergent collision avoidance comes from physics interactions.

### Placement System (`Assets/Scripts/Placeables/`)
- `ObjectPlacer` (singleton): manages placement state machine; triggered by QuickBar (keys 1–9, 0) or `ClickableButton`
- `PlaceableObjectOrGhost`: base for all structures; ghost preview validates before building
- `BaseGhostableMonobehaviour`: base for components that need ghost/real switching behavior; child objects are deselected when parent is deselected
- `IPlaceablePositionValidator`: interface for custom placement rules (e.g., `NoSpawnZone`)
- Ghost switching uses `MaterialGhostable`, `ColliderGhostable`, `RigidbodyGhostable` component composition

### Resource & Spawning (`AntNest.cs`, `Digestion.cs`)
`Digestion` tracks colony food reserves. `AntNest` spawns ants only when `CurrentFood > (ReserveFood + SpawnCost)`. Different ant prefabs in `AntNest.AntPrefabs` have different food costs.

### UI & Input (`Assets/Scripts/UI/`)
`GlobalKeyHandler` routes keyboard input. `QuickBarButton` maps number keys to placement. Score/money tracked via `ScoreTracker`, `MoneyTracker`, `ValueTracker`.

## Key Patterns

- **Component composition:** Each behavior is a separate MonoBehaviour. Target ~200–300 lines per script.
- **Enums over magic strings:** `AntState` and `Smell` enums drive all major logic branching.
- **Abstract base classes:** `Smellable` (targets), `Carryable` (food/items), `DeathActionBehaviour` (cleanup on death).
- **Public fields for tuning:** All behavior constants are public fields set in the Unity Inspector — don't hardcode values.
- **Singletons:** `ObjectPlacer.Instance`, `SingletonMonoBehaviour<T>` base class.
- **Nullable for optional state:** `float? _targetValue`, `Rigidbody _carrier = null`.

## Ant Decision-Making Loop

1. `ScentDetector.OnTriggerEnter()` → `AntStateMachine.ProcessSmell()`
2. `AntStateMachine` picks best `Smellable` via `GetPriority()`
3. State determines movement direction + trail scent type
4. `AntMoveController.FixedUpdate()` applies physics forces toward target
5. `AntTrailController` spawns trail points if conditions are met

## Common Extension Points

- **New ant type:** Add prefab to `AntNest.AntPrefabs`, set `FoodCost`, tune via `AntStateMachine` public fields
- **New structure:** Inherit `PlaceableObjectOrGhost`, add ghost visuals via `MaterialGhostable` or `PlaceableGhost`
- **New scent type:** Add to `Smell` enum, implement concrete `Smellable` subclass, update `AntStateMachine.TrailSmell` property
- **New game rule:** Hook into `Digestion` (resources), `HealthController` (damage), or `DeathActionBehaviour` (cleanup)

## Debugging Tips

- Use `Debug.Assert()` for invariant checks (see existing usage in `AntStateMachine`, `FoodSmell`)
- Physics bugs: check Rigidbody mass, drag, and constraints
- Scent detection issues: verify `ScentDetector` colliders have `IsTrigger = true`
- Trail density issues: adjust `AntTrailController.OverlapRadius`
