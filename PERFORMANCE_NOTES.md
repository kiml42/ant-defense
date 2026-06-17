# Performance Optimisation Notes

Remove items from this list as they are completed.

---

## GC Allocations (cause stutters via garbage collection)

- [x] **AntCam.cs:21-29** — `_directionKeys` property allocated 4 new collections + array every frame. Fixed: cached as a field initialised in `Awake()`.

- [ ] **TrailPointManager.cs:52,62** — LINQ `.Where()` in `Update()` allocates enumerators every frame across potentially hundreds of trail points. Replace with manual `for` loops.

- [ ] **TurretController.cs:171** — `CleanTargets()` runs `.Where().Distinct().ToList()` every `FixedUpdate`. `.Distinct()` is O(n²) worst-case and allocates. Use `RemoveAll()` instead.

- [ ] **TrailPointController.cs:59,67,80** — LINQ `.Any()`, `.Max()`, `.OrderBy().First()` in properties likely called in hot paths. Replace with manual loops.

- [ ] **UiPlane.cs:25** — LINQ filter on `ProtectMes` every frame to clean up destroyed objects. Switch to event/callback-based cleanup.

---

## Physics / Raycasts

- [ ] **AntTrailController.cs:111** — `Physics.OverlapSphere()` every `FixedUpdate` per ant. Add a frame-skip counter (every 2–3 frames) to halve/third the cost.

- [ ] **AntTargetSelector.cs:198** — LOS raycast every `FixedUpdate` per ant. Cache the result with a short TTL (e.g. 0.1s) and only recheck on expiry.

- [ ] **TranslateHandle.cs:216,232,245** — 3 separate raycasts per frame for input detection. Do one raycast and pass the result to all handlers.

---

## Algorithmic

- [ ] **NoSpawnZone.cs:137-171** — `GetBestEdgePosition()` has nested LINQ loops that re-check `IsInAnyNoSpawnZone()` for every candidate point on every mouse-move frame. Cache per-frame or dirty-flag the result.
