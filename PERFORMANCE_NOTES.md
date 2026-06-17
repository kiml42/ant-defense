# Performance Optimisation Notes

Remove items from this list as they are completed.

All identified items have been implemented. File kept for reference.

---

## GC Allocations (cause stutters via garbage collection)

- [x] 1 **AntCam.cs:21-29** — `_directionKeys` property allocated 4 new collections + array every frame. Fixed: cached as a field initialised in `Awake()`.

- [x] 2 **TrailPointManager.cs:52,62** — LINQ `.Where()` in `Update()` allocates enumerators every frame across potentially hundreds of trail points. Fixed: manual `foreach` loops with inline guards.

- [x] 3 **TurretController.cs:171** — `CleanTargets()` ran `.Where().Distinct().ToList()` every `FixedUpdate`. Fixed: `RemoveAll()`. `.Distinct()` was also redundant since `RegisterTarget` already prevents duplicates.

- [x] 4 **TrailPointController.cs:59,67,80** — LINQ `.Any()`, `.Max()`, `.OrderBy().First()` in properties called in hot paths. Fixed: manual for-loops throughout; `UpdateTrailPoint` uses reverse-index `RemoveAt`.

- [x] 5 **UiPlane.cs:25** — LINQ filter + `ToArray()` on `ProtectMes` every frame. Fixed: `RemoveAll()`.

---

## Physics / Raycasts

- [x] 6 **AntTrailController.cs:111** — LINQ chain over `Physics.OverlapSphere` results every time a trail point is dropped. Fixed: single manual loop; distance comparison uses `sqrMagnitude`.

- [x] 7 **AntTargetSelector.cs:198** — LOS raycast every `FixedUpdate` per ant. Fixed: TTL-cached via `CachedCheckLineOfSight()` (default interval 0.1s); cache invalidated on target change.

- [x] 8 **TranslateHandle.cs:216,232,245** — 3 separate raycasts per frame for UI input. Fixed: single `PerformUiRaycast()` in `Update()`; result shared by all three handlers.

---

## Algorithmic

- [x] 9 **NoSpawnZone.cs:137-171** — LINQ `.Where()` / `.Select()` allocations in `GetBestEdgePosition()` on every mouse-drag frame. Fixed: manual for-loops; `transgressedZones` list lazily allocated only when needed; `pointsToCheck` pre-sized.
