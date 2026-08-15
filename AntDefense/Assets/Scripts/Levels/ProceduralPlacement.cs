using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure static helpers for procedural level generation.
/// No MonoBehaviour or scene dependencies — fully testable in EditMode.
/// </summary>
public static class ProceduralPlacement
{
    // ── Nest placement ────────────────────────────────────────────────────────

    /// <summary>
    /// Places <paramref name="count"/> nests inside <paramref name="area"/>, attempting to keep
    /// them at least <paramref name="minSeparation"/> apart and at least <paramref name="margin"/>
    /// from the edges. Best-effort: if the area is too crowded the closest valid candidate is used.
    /// </summary>
    public static List<Vector2> PlaceNests(
        int count, Rect area, float minSeparation, float margin, System.Random rng)
    {
        var inner = Shrink(area, margin);
        var positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            var best = Vector2.zero;
            float bestDist = -1f;
            int attempts = 100;

            for (int a = 0; a < attempts; a++)
            {
                var candidate = RandomPoint(inner, rng);
                float minDist = MinDistanceTo(candidate, positions);

                if (minDist < 0f) // first nest, no existing points
                {
                    best = candidate;
                    break;
                }

                if (minDist >= minSeparation)
                {
                    best = candidate;
                    break;
                }

                if (minDist > bestDist)
                {
                    bestDist = minDist;
                    best = candidate;
                }
            }

            positions.Add(best);
        }

        return positions;
    }

    // ── BiscuitPlate placement ────────────────────────────────────────────────

    /// <summary>
    /// Returns the position (within <paramref name="area"/> minus <paramref name="margin"/>)
    /// that maximises the minimum Euclidean distance to any nest. Samples
    /// <paramref name="candidates"/> random points and picks the best.
    /// </summary>
    public static Vector2 PlacePlate(
        Rect area, float margin, IReadOnlyList<Vector2> nestPositions, System.Random rng,
        int candidates = 200, float minDistance = 0f)
    {
        var inner = Shrink(area, margin);
        var best = RandomPoint(inner, rng);
        float bestDist = MinDistanceTo(best, nestPositions);

        for (int i = 1; i < candidates; i++)
        {
            var candidate = RandomPoint(inner, rng);
            float d = MinDistanceTo(candidate, nestPositions);
            if (minDistance > 0f && d < minDistance) continue;
            if (d > bestDist)
            {
                bestDist = d;
                best = candidate;
            }
        }

        return best;
    }

    // ── Wall segment generation ───────────────────────────────────────────────

    // A gap in a wall line: centre along the wall axis and the half-width of the opening.
    private readonly struct GapSpec
    {
        public readonly float Centre;
        public readonly float HalfSize;
        public GapSpec(float centre, float halfSize) { Centre = centre; HalfSize = halfSize; }
    }

    public readonly struct WallSegment
    {
        public readonly Vector2 Centre;
        public readonly float AngleDegrees;
        public readonly float Length;

        public WallSegment(Vector2 centre, float angleDegrees, float length)
        {
            Centre = centre;
            AngleDegrees = angleDegrees;
            Length = length;
        }
    }

    /// <summary>
    /// Produces a wall spanning edge-to-edge (or to an existing wall) perpendicular to the
    /// nest→plate line. One strategic gap is placed offset from the midpoint; forced gaps are
    /// added wherever the wall passes near an avoid point; additional random gaps fill out the
    /// density set by <paramref name="gapsPerUnitLength"/>.
    /// </summary>
    public static List<WallSegment> BuildBlockingWall(
        Vector2 nestPos, Vector2 platePos,
        Rect area, float minGapOffset, float gapSize,
        float gapsPerUnitLength, float minAvoidDistance, System.Random rng,
        IReadOnlyList<WallSegment> existingWalls = null,
        IReadOnlyList<Vector2> avoidPositions = null)
    {
        var diff = platePos - nestPos;
        var wallDir = new Vector2(-diff.y, diff.x).normalized;
        var midpoint = (nestPos + platePos) * 0.5f;

        var (posExtent, negExtent) = WallExtents(midpoint, wallDir, area);
        ClipToExistingWalls(ref posExtent, ref negExtent, midpoint, wallDir, existingWalls);

        float wallStart  = -negExtent;
        float wallEnd    =  posExtent;
        float wallLength =  posExtent + negExtent;

        // Strategic gap: offset from midpoint so it isn't directly on the nest-plate line.
        bool posValid = posExtent - gapSize * 0.5f >= minGapOffset;
        bool negValid = negExtent - gapSize * 0.5f >= minGapOffset;

        float strategic;
        if (posValid && negValid)
            strategic = rng.NextDouble() > 0.5
                ?  RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f)
                : -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        else if (posValid)
            strategic = RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f);
        else if (negValid)
            strategic = -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        else
            strategic = 0f;

        int totalGaps = Mathf.Max(1, Mathf.RoundToInt(wallLength * gapsPerUnitLength));
        var gaps = new List<GapSpec> { new GapSpec(strategic, gapSize * 0.5f) };
        AddForcedGaps(gaps, midpoint, wallDir, wallStart, wallEnd, gapSize, minAvoidDistance, avoidPositions);
        AddRandomGaps(gaps, Mathf.Max(0, totalGaps - gaps.Count), wallStart, wallEnd, gapSize, rng);

        float wallAngle = Mathf.Atan2(wallDir.y, wallDir.x) * Mathf.Rad2Deg;
        return SegmentsFromGaps(midpoint, wallDir, wallAngle, wallStart, wallEnd, gaps);
    }

    /// <summary>
    /// Generates <paramref name="wallDensity"/> ambient walls without existing-wall context.
    /// Used primarily for testing; in-game use goes through BuildWalls.
    /// </summary>
    public static List<WallSegment> BuildAdditionalWalls(
        int wallDensity, Rect area, float gapSize, float gapsPerUnitLength, System.Random rng,
        float minAvoidDistance = 0f, IReadOnlyList<Vector2> avoidPositions = null)
    {
        var result = new List<WallSegment>(wallDensity * 4);
        for (int i = 0; i < wallDensity; i++)
            result.AddRange(BuildAmbientWall(area, gapSize, gapsPerUnitLength, minAvoidDistance, rng, null, avoidPositions));
        return result;
    }

    // Builds one full-width wall (clipped to existing walls) at a random angle.
    // Forced gaps clear nests/plate; random gaps fill out the density target.
    private static List<WallSegment> BuildAmbientWall(
        Rect area, float gapSize, float gapsPerUnitLength, float minAvoidDistance,
        System.Random rng,
        IReadOnlyList<WallSegment> existingWalls,
        IReadOnlyList<Vector2> avoidPositions)
    {
        float angleDeg = (float)(rng.NextDouble() * 180.0);
        float rad = angleDeg * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        var centre = RandomPoint(Shrink(area, 10f), rng);

        var (posExtent, negExtent) = WallExtents(centre, dir, area);
        ClipToExistingWalls(ref posExtent, ref negExtent, centre, dir, existingWalls);

        float wallStart  = -negExtent;
        float wallEnd    =  posExtent;
        float wallLength =  posExtent + negExtent;

        int targetGaps = Mathf.Max(1, Mathf.RoundToInt(wallLength * gapsPerUnitLength));
        var gaps = new List<GapSpec>(targetGaps);
        AddForcedGaps(gaps, centre, dir, wallStart, wallEnd, gapSize, minAvoidDistance, avoidPositions);
        AddRandomGaps(gaps, Mathf.Max(0, targetGaps - gaps.Count), wallStart, wallEnd, gapSize, rng);

        float wallAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return SegmentsFromGaps(centre, dir, wallAngle, wallStart, wallEnd, gaps);
    }

    // Clips posExtent / negExtent so the wall stops where it first hits an existing wall.
    // A small overlap is added so the walls visually join rather than just touching.
    private static void ClipToExistingWalls(
        ref float posExtent, ref float negExtent,
        Vector2 centre, Vector2 dir,
        IReadOnlyList<WallSegment> existingWalls)
    {
        if (existingWalls == null || existingWalls.Count == 0) return;
        const float overlap = 1f;

        foreach (var seg in existingWalls)
        {
            float segRad = seg.AngleDegrees * Mathf.Deg2Rad;
            var segDir = new Vector2(Mathf.Cos(segRad), Mathf.Sin(segRad));
            var delta = seg.Centre - centre;

            float cross = dir.x * segDir.y - dir.y * segDir.x;
            if (Mathf.Abs(cross) < 1e-6f) continue; // parallel — skip

            // t = distance along dir to intersection; s = position along seg
            float t = (delta.x * segDir.y - delta.y * segDir.x) / cross;
            float s = (dir.y  * delta.x   - dir.x  * delta.y)   / cross;

            if (Mathf.Abs(s) > seg.Length * 0.5f) continue; // outside segment bounds

            if (t > overlap)
                posExtent = Mathf.Min(posExtent, t + overlap);
            else if (t < -overlap)
                negExtent = Mathf.Min(negExtent, -t + overlap);
        }
    }

    // Adds a forced gap wherever the wall passes within minAvoidDistance of an avoid point.
    // The gap half-size is the exact geometric clearance needed to keep the wall edge at
    // minAvoidDistance from the point.
    private static void AddForcedGaps(
        List<GapSpec> gaps,
        Vector2 origin, Vector2 dir,
        float wallStart, float wallEnd,
        float gapSize, float minAvoidDistance,
        IReadOnlyList<Vector2> avoidPositions)
    {
        if (avoidPositions == null || minAvoidDistance <= 0f) return;
        float halfGap = gapSize * 0.5f;

        foreach (var pt in avoidPositions)
        {
            float t = Mathf.Clamp(Vector2.Dot(pt - origin, dir), wallStart, wallEnd);
            float dist = Vector2.Distance(pt, origin + dir * t);
            if (dist >= minAvoidDistance) continue;

            float halfRequired = Mathf.Sqrt(Mathf.Max(0f, minAvoidDistance * minAvoidDistance - dist * dist));
            float halfSize = Mathf.Max(halfGap, halfRequired);

            // Skip if an existing gap already covers this point.
            bool covered = false;
            foreach (var g in gaps)
                if (t >= g.Centre - g.HalfSize && t <= g.Centre + g.HalfSize) { covered = true; break; }

            if (!covered)
                gaps.Add(new GapSpec(t, halfSize));
        }
    }

    // Appends up to `count` non-overlapping random gaps; respects existing gap extents.
    private static void AddRandomGaps(List<GapSpec> gaps, int count, float wallStart, float wallEnd, float gapSize, System.Random rng)
    {
        float halfGap   = gapSize * 0.5f;
        float safeStart = wallStart + gapSize;
        float safeEnd   = wallEnd   - gapSize;
        if (safeEnd <= safeStart) return;

        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float t = RangeRng(rng, safeStart, safeEnd);
                bool overlaps = false;
                foreach (var g in gaps)
                    if (t < g.Centre + g.HalfSize + halfGap && t > g.Centre - g.HalfSize - halfGap)
                    { overlaps = true; break; }
                if (!overlaps) { gaps.Add(new GapSpec(t, halfGap)); break; }
            }
        }
    }

    // Converts a list of GapSpecs into solid WallSegments in the spaces between them.
    private static List<WallSegment> SegmentsFromGaps(
        Vector2 origin, Vector2 dir, float angleDeg,
        float wallStart, float wallEnd, List<GapSpec> gaps)
    {
        gaps.Sort((a, b) => a.Centre.CompareTo(b.Centre));

        var bounds = new List<float> { wallStart };
        foreach (var g in gaps)
        {
            bounds.Add(g.Centre - g.HalfSize);
            bounds.Add(g.Centre + g.HalfSize);
        }
        bounds.Add(wallEnd);

        var result = new List<WallSegment>(bounds.Count / 2);
        for (int i = 0; i < bounds.Count - 1; i += 2)
        {
            float start = bounds[i];
            float end   = bounds[i + 1];
            float len   = end - start;
            if (len > 0.5f)
                result.Add(new WallSegment(origin + dir * ((start + end) * 0.5f), angleDeg, len));
        }
        return result;
    }

    // ── Combined wall build ───────────────────────────────────────────────────

    /// <summary>
    /// Builds all walls for a level.
    /// Blocking walls fire automatically when dist(nest, plate) &lt; blockingWallThreshold.
    /// wallDensity controls ambient wall count. Both use gapsPerUnitLength for gap density.
    /// Each new wall is clipped against already-placed walls so it can end at an existing wall.
    /// </summary>
    public static List<WallSegment> BuildWalls(
        List<Vector2> nestPositions, Vector2 platePos2D,
        Rect area, int wallDensity,
        float minGapOffset, float gapSize,
        float gapsPerUnitLength, float minAvoidDistance,
        float connectivityCellSize, float blockingWallThreshold,
        System.Random rng)
    {
        var walls = new List<WallSegment>();
        var avoidPositions = new List<Vector2>(nestPositions) { platePos2D };

        // Blocking walls: automatic when a nest is close enough to the plate.
        foreach (var nest in nestPositions)
        {
            if (Vector2.Distance(nest, platePos2D) >= blockingWallThreshold) continue;

            List<WallSegment> candidate = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                candidate = BuildBlockingWall(
                    nest, platePos2D, area, minGapOffset, gapSize,
                    gapsPerUnitLength, minAvoidDistance, rng, walls, avoidPositions);

                var combined = new List<WallSegment>(walls);
                combined.AddRange(candidate);

                if (IsConnected(nestPositions, platePos2D, combined, area, connectivityCellSize))
                    break;

                candidate = null;
            }

            if (candidate != null)
                walls.AddRange(candidate);
        }

        // Ambient walls: built one at a time so each sees previously placed walls.
        for (int i = 0; i < wallDensity; i++)
        {
            var segs = BuildAmbientWall(area, gapSize, gapsPerUnitLength, minAvoidDistance, rng, walls, avoidPositions);
            var testList = new List<WallSegment>(walls);
            testList.AddRange(segs);
            if (IsConnected(nestPositions, platePos2D, testList, area, connectivityCellSize))
                walls.AddRange(segs);
        }

        return walls;
    }

    // ── Cluster placement ─────────────────────────────────────────────────────

    public readonly struct BushCluster
    {
        public readonly Vector2 Centre;
        public readonly int BushCount;

        public BushCluster(Vector2 centre, int bushCount)
        {
            Centre = centre;
            BushCount = bushCount;
        }
    }

    /// <summary>
    /// Places <paramref name="clusterCount"/> clusters inside <paramref name="area"/>.
    /// Cluster size is probabilistically larger the further the cluster centre is from
    /// any nest. <paramref name="clusterSize"/> is the baseline average size.
    /// <paramref name="avoidPositions"/> (nests + plate) are kept at least
    /// <paramref name="minAvoidDistance"/> away. Clusters are kept at least
    /// <paramref name="minClusterSeparation"/> apart to spread them evenly.
    /// </summary>
    public static List<BushCluster> PlaceClusters(
        int clusterCount, int clusterSize, Rect area, float margin,
        IReadOnlyList<Vector2> nestPositions, System.Random rng,
        IReadOnlyList<Vector2> avoidPositions = null,
        float minAvoidDistance = 0f,
        float minClusterSeparation = 0f)
    {
        var inner = Shrink(area, margin);
        float maxPossibleDist = Mathf.Sqrt(inner.width * inner.width + inner.height * inner.height);

        var clusters = new List<BushCluster>(clusterCount);
        var centres = new List<Vector2>(clusterCount);

        for (int i = 0; i < clusterCount; i++)
        {
            var best = RandomPoint(inner, rng);

            for (int a = 0; a < 50; a++)
            {
                var candidate = RandomPoint(inner, rng);

                if (avoidPositions != null && minAvoidDistance > 0f)
                {
                    float d = MinDistanceTo(candidate, avoidPositions);
                    if (d >= 0f && d < minAvoidDistance) continue;
                }

                if (minClusterSeparation > 0f && centres.Count > 0)
                {
                    if (MinDistanceTo(candidate, centres) < minClusterSeparation) continue;
                }

                best = candidate;
                break;
            }

            centres.Add(best);

            float distToNest = MinDistanceTo(best, nestPositions);
            if (distToNest < 0f) distToNest = maxPossibleDist;

            float normalizedDist = Mathf.Clamp01(distToNest / (maxPossibleDist * 0.5f));

            // Distance-weighted size: small (0.3–0.7×) near nests, large (0.5–2.5×) far away
            float minScale = Lerp(0.3f, 0.5f, normalizedDist);
            float maxScale = Lerp(0.7f, 2.5f, normalizedDist);
            float scale = Lerp(minScale, maxScale, (float)rng.NextDouble());

            int count = Mathf.Max(1, Mathf.RoundToInt(clusterSize * scale));
            clusters.Add(new BushCluster(best, count));
        }

        return clusters;
    }

    // ── Connectivity check ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if there is a traversable path (on a 2D grid) from every nest position
    /// to the plate position, given the wall segments that block passage.
    /// <paramref name="cellSize"/> is the grid resolution in world units.
    /// </summary>
    public static bool IsConnected(
        IReadOnlyList<Vector2> nestPositions, Vector2 platePosition,
        IReadOnlyList<WallSegment> walls, Rect area, float cellSize)
    {
        int cols = Mathf.CeilToInt(area.width / cellSize);
        int rows = Mathf.CeilToInt(area.height / cellSize);
        bool[,] blocked = new bool[cols, rows];

        foreach (var wall in walls)
            MarkWall(wall, area, cellSize, cols, rows, blocked);

        var plateCell = WorldToCell(platePosition, area, cellSize, cols, rows);

        foreach (var nest in nestPositions)
        {
            var nestCell = WorldToCell(nest, area, cellSize, cols, rows);
            if (!BfsConnected(nestCell, plateCell, blocked, cols, rows))
                return false;
        }

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Rect Shrink(Rect r, float margin) =>
        new Rect(r.x + margin, r.y + margin, r.width - margin * 2, r.height - margin * 2);

    private static Vector2 RandomPoint(Rect r, System.Random rng) =>
        new Vector2(
            r.x + (float)(rng.NextDouble() * r.width),
            r.y + (float)(rng.NextDouble() * r.height));

    private static float MinDistanceTo(Vector2 point, IReadOnlyList<Vector2> others)
    {
        if (others == null || others.Count == 0) return -1f;
        float min = float.MaxValue;
        foreach (var o in others)
            min = Mathf.Min(min, Vector2.Distance(point, o));
        return min;
    }

    private static float RangeRng(System.Random rng, float min, float max) =>
        (float)(rng.NextDouble() * (max - min) + min);

    // Extents from centre along +wallDir and -wallDir to the nearest area boundary.
    private static (float pos, float neg) WallExtents(Vector2 centre, Vector2 wallDir, Rect area)
    {
        float posExtent = float.MaxValue, negExtent = float.MaxValue;

        void Consider(float t)
        {
            if (t > 1e-4f) posExtent = Mathf.Min(posExtent, t);
            else if (t < -1e-4f) negExtent = Mathf.Min(negExtent, -t);
        }

        if (Mathf.Abs(wallDir.x) > 1e-6f)
        {
            Consider((area.xMin - centre.x) / wallDir.x);
            Consider((area.xMax - centre.x) / wallDir.x);
        }
        if (Mathf.Abs(wallDir.y) > 1e-6f)
        {
            Consider((area.yMin - centre.y) / wallDir.y);
            Consider((area.yMax - centre.y) / wallDir.y);
        }

        float pos = posExtent == float.MaxValue ? 50f : posExtent;
        float neg = negExtent == float.MaxValue ? 50f : negExtent;
        return (Mathf.Max(pos, 5f), Mathf.Max(neg, 5f));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // ── BFS helpers ───────────────────────────────────────────────────────────

    private static (int x, int y) WorldToCell(Vector2 world, Rect area, float cellSize, int cols, int rows)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((world.x - area.xMin) / cellSize), 0, cols - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((world.y - area.yMin) / cellSize), 0, rows - 1);
        return (x, y);
    }

    private static void MarkWall(WallSegment wall, Rect area, float cellSize, int cols, int rows, bool[,] blocked)
    {
        float rad = wall.AngleDegrees * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float halfLen = wall.Length * 0.5f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(wall.Length / (cellSize * 0.5f)));

        for (int s = 0; s <= steps; s++)
        {
            float t = -halfLen + s * (wall.Length / steps);
            var pt = wall.Centre + dir * t;
            var cell = WorldToCell(pt, area, cellSize, cols, rows);
            if (cell.x >= 0 && cell.x < cols && cell.y >= 0 && cell.y < rows)
                blocked[cell.x, cell.y] = true;
        }
    }

    private static bool BfsConnected((int x, int y) from, (int x, int y) to, bool[,] blocked, int cols, int rows)
    {
        if (blocked[from.x, from.y] || blocked[to.x, to.y]) return false;
        if (from == to) return true;

        var visited = new bool[cols, rows];
        var queue = new Queue<(int, int)>();
        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, 1, -1, -1 };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (cx == to.x && cy == to.y) return true;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + dx[d], ny = cy + dy[d];
                if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
                if (visited[nx, ny] || blocked[nx, ny]) continue;
                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return false;
    }
}
