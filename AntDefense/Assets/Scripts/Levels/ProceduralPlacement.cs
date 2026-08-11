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
        Rect area, float margin, IReadOnlyList<Vector2> nestPositions, System.Random rng, int candidates = 200)
    {
        var inner = Shrink(area, margin);
        var best = RandomPoint(inner, rng);
        float bestDist = MinDistanceTo(best, nestPositions);

        for (int i = 1; i < candidates; i++)
        {
            var candidate = RandomPoint(inner, rng);
            float d = MinDistanceTo(candidate, nestPositions);
            if (d > bestDist)
            {
                bestDist = d;
                best = candidate;
            }
        }

        return best;
    }

    // ── Wall segment generation ───────────────────────────────────────────────

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
    /// Produces wall segments that block the direct line from <paramref name="nestPos"/> to
    /// <paramref name="platePos"/>. The gap in the wall is at least <paramref name="minGapOffset"/>
    /// units from the midpoint. Returns at most two segments (one either side of gap).
    /// </summary>
    public static List<WallSegment> BuildBlockingWall(
        Vector2 nestPos, Vector2 platePos,
        Rect area, float minGapOffset, float gapSize, System.Random rng)
    {
        var diff = platePos - nestPos;
        var wallDir = new Vector2(-diff.y, diff.x).normalized;
        var midpoint = (nestPos + platePos) * 0.5f;

        // Independent extents from midpoint to each boundary along wallDir
        var (posExtent, negExtent) = WallExtents(midpoint, wallDir, area);

        // Place the gap on one side, at least minGapOffset from midpoint
        bool posValid = posExtent - gapSize * 0.5f >= minGapOffset;
        bool negValid = negExtent - gapSize * 0.5f >= minGapOffset;

        float gapOffset;
        if (posValid && negValid)
        {
            if (rng.NextDouble() > 0.5)
                gapOffset = RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f);
            else
                gapOffset = -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        }
        else if (posValid)
            gapOffset = RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f);
        else if (negValid)
            gapOffset = -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        else
            gapOffset = 0f;

        float gapLeft = gapOffset - gapSize * 0.5f;
        float gapRight = gapOffset + gapSize * 0.5f;

        float wallAngle = Mathf.Atan2(wallDir.y, wallDir.x) * Mathf.Rad2Deg;
        var segments = new List<WallSegment>(2);

        // Segment from -negExtent to gapLeft
        float leftLen = gapLeft + negExtent;
        if (leftLen > 0.5f)
        {
            var leftCentre = midpoint + wallDir * ((gapLeft - negExtent) * 0.5f);
            segments.Add(new WallSegment(leftCentre, wallAngle, leftLen));
        }

        // Segment from gapRight to +posExtent
        float rightLen = posExtent - gapRight;
        if (rightLen > 0.5f)
        {
            var rightCentre = midpoint + wallDir * ((gapRight + posExtent) * 0.5f);
            segments.Add(new WallSegment(rightCentre, wallAngle, rightLen));
        }

        return segments;
    }

    /// <summary>
    /// Generates additional random wall segments based on <paramref name="wallDensity"/> (0–10).
    /// Even at maximum density the layout stays sparse.
    /// </summary>
    public static List<WallSegment> BuildAdditionalWalls(
        int wallDensity, Rect area, float gapSize, float minSegmentLength, float maxSegmentLength, System.Random rng)
    {
        // At density 10: 5 extra segments; at density 0: 0 segments.
        int count = Mathf.RoundToInt(wallDensity * 0.5f);
        var inner = Shrink(area, 10f);
        var result = new List<WallSegment>(count);

        for (int i = 0; i < count; i++)
        {
            var centre = RandomPoint(inner, rng);
            float angle = (float)(rng.NextDouble() * 360.0);
            float totalLen = Lerp(minSegmentLength, maxSegmentLength, (float)rng.NextDouble());

            // Split into two segments with a gap, like blocking walls
            float gapLen = Mathf.Max(gapSize, totalLen * 0.25f);
            float halfSide = (totalLen - gapLen) * 0.5f;
            var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            if (halfSide > 0.5f)
            {
                result.Add(new WallSegment(centre - dir * (halfSide * 0.5f + gapLen * 0.5f), angle, halfSide));
                result.Add(new WallSegment(centre + dir * (halfSide * 0.5f + gapLen * 0.5f), angle, halfSide));
            }
        }

        return result;
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
