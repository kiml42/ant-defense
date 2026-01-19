# Ant Defense - Copilot Instructions

## Project Overview
**Ant Defense** is a Unity strategy/tower-defense game where players manage an ant colony defending against threats. Ants autonomously forage for food, communicate via scent trails, and the player places defensive structures (turrets, walls).

**Environment:** Unity 6000.2.8f1 (2024), C#, Physics-based 3D gameplay

---

## Architecture Overview

### Core Components

#### 1. **Ant Intelligence System** (`AntStateMachine.cs`)
- **Central hub** for ant behavior, managing states: `SeekingFood`, `ReturningToFood`, `ReportingFood`, `CarryingFood`, `ReturningHome`
- Uses **state-driven behavior** with scent-based decision making
- Key properties:
  - `CurrentTarget`: Prioritized Smellable object (food or trail point)
  - `MaxTimeGoingForTrailPoint`: Timeout to abandon poor targets
  - `State`: Drives what scent trail the ant leaves and what targets it accepts
- **Why this pattern:** Ants simulate collective intelligence through simple pheromone-like mechanics; state machine enables clear behavior transitions

#### 2. **Scent System** (Smellable, FoodSmell, AntNestSmell, ScentDetector)
- **Scent types:** `Food` (food locations) and `Home` (nest location)
- `Smellable`: Abstract base for anything with a smell
  - `IsActual`: Distinguishes real objects from trail-point breadcrumbs
  - `GetPriority()`: Ranks targets based on value/distance (e.g., higher food value = higher priority)
- `ScentDetector`: Collision-based smell detection on ants; notifies `AntStateMachine.ProcessSmell()`
- **Why this pattern:** Smell-based navigation is computationally simpler than pathfinding; supports emergent behavior where ants follow pheromone trails

#### 3. **Ant Trail System** (`AntTrailController`, `TrailPointController`, `TrailPointManager`)
- Ants leave **temporary trail points** as breadcrumbs when carrying food or returning home
- Trail points decay over time; position-based clustering prevents trail spam (see `OverlapRadius = 2f`)
- Trail value (`TrailTargetValue`) encoded in trail metadata for priority decisions
- **Integration:** `AntStateMachine.TrailSmell` property determines what smell type the ant emits based on state

#### 4. **Ant Movement** (`AntMoveController`, `AntTargetPositionProvider`)
- **Physics-based:** Rigidbody forces/torques drive movement (not kinematic)
- Movement toward target via `AntTargetPositionProvider.DirectionToMove`
- Self-righting logic: ants apply upright torque (`this.transform.up` toward `Vector3.up`)
- **Why physics:** Enables emergent collision avoidance, obstacle bouncing, natural grouping behavior

#### 5. **Ant Spawning & Resource Management** (`AntNest`, `Digestion`)
- `AntNest` spawns ants via `AntPrefabs` list (different ant types with different food costs)
- `Digestion` manages colony food reserves
- Reserve system: keeps `ReserveFood` to prevent starvation; only spawns if `CurrentFood > (ReserveFood + SpawnCost)`
- **Why this pattern:** Resource scarcity creates strategic tension; different ant types allow future gameplay variety

#### 6. **Defensive Structures & Placement** (`ObjectPlacer`, `PlaceableObjectOrGhost`, `TurretController`, `FlipTrap`, `WallNode`)
- **Placement system:** QuickBar (numeric keys 1-9, 0) triggers ghost preview; drag + click to finalize
- **Ghost pattern:** Transparent preview (`PlaceableGhost`) validates placement before building
- Structure types: Walls (linked nodes), Turrets (targetable), Traps (flip-based)
- **Position validation:** `IPlaceablePositionValidator` interface for custom placement rules (e.g., `NoSpawnZone`)

---

## Key Patterns & Conventions

### **Use Unity Components for Composition**
- Each behavior is a separate MonoBehaviour (e.g., `ScentDetector`, `HealthController`, `TrailPointController`)
- Never monolithic scripts; split at ~200-300 lines (see TODO in `AntStateMachine.cs` line 9)
- Reference other components via `GetComponent<T>()` or public fields populated in editor

### **Enums Over Magic Strings**
- `AntState`, `Smell` enums drive major logic; case statements on enums are idiomatic
- Used in state machines and smell routing

### **Abstract Base Classes for Shared Behavior**
- `Smellable`: All targetable objects inherit (food, nests, trail points)
- `Carryable`: Food and items that ants can carry
- `DeathActionBehaviour`: Base for objects that need cleanup on death (see `DeathActionBehaviour`, `SpawnObjectOnDeath`)

### **Numeric Tuning via Public Fields**
- Almost all behavior constants are public float/int fields (e.g., `MaxTimeGoingForTrailPoint`, `ForceMultiplier`, `SpawnRadius`)
- Game design adjusts these in the Inspector; don't hardcode values

### **Singleton Patterns for Global Systems**
- `ObjectPlacer.Instance`: Single placement manager
- `TrailParent` GameObject: Central parent for all trail points (avoids scene hierarchy bloat)

### **Nullable<T> for Optional State**
- Trail value tracking: `float? _targetValue`; collision detection: `Rigidbody _carrier = null`
- Simplifies null checks vs. sentinel values

---

## Data Flow & Communication

### **Ant Decision-Making Loop**
1. **Smell Detection** → `ScentDetector.OnTriggerEnter()` → `AntStateMachine.ProcessSmell()`
2. **Target Selection** → `AntStateMachine` picks best `Smellable` via `GetPriority()`
3. **State Update** → State determines movement direction + trail scent type
4. **Movement** → `AntMoveController.FixedUpdate()` applies physics forces toward target
5. **Trail Emission** → `AntTrailController` spawns trail points if conditions met

### **Ant-Nest Communication**
- Ants carry `Food` objects back to nest
- `Food.Attach(Rigidbody)` disables food smell when carried (hides from other ants until delivered)
- Nest `Digestion` accumulates food; triggers spawning when reserves exceed cost

### **Player-Structure Communication**
- `ObjectPlacer` manages placement state machine
- Structures inherit `PlaceableObjectOrGhost`; implement `IPlaceablePositionValidator` for custom placement rules
- `ClickableButton` / `GlobalKeyHandler` route input to placement or world interactions

---

## Development Workflows

### **Building & Running**
- Open `/workspaces/ant-defense/AntDefense` as the Unity project folder
- Unity 6000.2.8f1 required; set in `ProjectSettings/ProjectVersion.txt`
- Main scenes: `ProperMaze.unity`, `SampleScene.unity` (see `Assets/Scenes/`)

### **Debugging Tips**
- Use `Debug.Assert()` for invariant checks (see `AntStateMachine`, `FoodSmell`)
- Physics-based bugs: Check Rigidbody mass, drag, constraints; ants use default gravity
- Scent detection issues: Verify `ScentDetector` colliders are set to `IsTrigger = true`
- Trail point clustering: Adjust `AntTrailController.OverlapRadius` to tune density

### **Common Extensions**
- **New ant type:** Add prefab to `AntNest.AntPrefabs`, set `FoodCost`, and tune behavior via `AntStateMachine` public fields
- **New structure:** Inherit `PlaceableObjectOrGhost`, implement ghost visuals (`PlaceableGhost` or material swapping)
- **New scent type:** Add to `Smell` enum, implement concrete `Smellable` subclass, update `AntStateMachine.TrailSmell` property
- **New game rule:** Hook into `Digestion` (resources), `HealthController` (damage), or `DeathActionBehaviour` (cleanup)

---

## Important Caveats

- **No formal tests:** Behavior validated via play-testing; use `Debug.Assert()` liberally
- **Performance notes:** Scent detection is collision-based (O(n) ants × triggers); trail point cleanup via distance/time decay
- **Visual scripting:** Project includes Unity VisualScripting (see `Packages/manifest.json`); some gameplay may use node graphs—check prefab properties
- **TODO markers:** Large classes like `AntStateMachine` (678 lines) are marked for refactoring; defer splitting unless working in that area

---

## File Structure Cheat Sheet
- `Assets/Scripts/`: Core game logic (ants, food, structures, UI)
- `Assets/Scripts/Placeables/`: Placement, validation, structure behavior
- `Assets/Scripts/Helpers/`: Utility functions (e.g., `MouseHelper`)
- `Assets/Scripts/UI/`: Input handlers and UI logic
- `Assets/Prefabs/`: Reusable objects (ants, structures, food, UI widgets)
- `Assets/Materials/`: Shaders/materials for visual feedback (health bars, trails, ghosts)
