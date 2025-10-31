using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoSpawnZone : BaseGhostable
{
    public enum PointType
    {
        /// <summary>
        /// The original value is being used without any correction required
        /// </summary>
        Original,
        /// <summary>
        /// The position was invalid, and this is the closest valid poition
        /// </summary>
        Corrected,
        /// <summary>
        /// The closest position is an interactive point, so clicking now should interact with it rather than placing an object
        /// </summary>
        InteractionPoint,
        /// <summary>
        /// The original position is invalid, and no valid position was found
        /// </summary>
        Invalid
    }

    public class AdjustedPoint
    {
        public readonly Vector3 Point;
        public readonly PointType Type;

        public float SnapPriority
        {
            get
            {
                return this.Type switch
                {
                    PointType.InteractionPoint => 2,
                    _ => 0
                };
            }
        }

        public virtual void Activate()
        {
            Debug.Assert(this.Type != PointType.InteractionPoint, "Interactie points should always use their own class");
            if (this.Type == PointType.Invalid)
            {
                // nothing to do if the point is invalid.
                return;
            }
            ObjectPlacer.Instance.PlaceObject();
        }

        public AdjustedPoint(Vector3 point, PointType type)
        {
            this.Point = point;
            this.Type = type;
        }
    }

    public class InteractionPoint : AdjustedPoint
    {
        private readonly IInteractivePosition _pointObject;

        public InteractionPoint(IInteractivePosition point) : base(point.Position, PointType.InteractionPoint)
        {
            this._pointObject = point;
        }

        public override void Activate()
        {
            // Don't call base because we don't want the default behaviour
            this._pointObject.Interact();
        }
    }

    public static HashSet<NoSpawnZone> AllNoSpawnZones = new();

    private static readonly List<IntersectionPoint> _intersectionPoints = new();

    private static readonly List<InteractionPoint> InteractionPoints = new();

    public float Radius = 3;

    void Start()
    {
        this.Activate();
    }

    private void Activate()
    {
        this.enabled = true;
        var collider = this.GetComponent<SphereCollider>();
        if (collider != null)
        {
            this.Radius = collider.radius;
        }

        AllNoSpawnZones.Add(this);
        this.AddIntersectionPoints();
    }

    private void OnDestroy()
    {
        this.Deactivate();
    }

    private void Deactivate()
    {
        this.enabled = false;
        this.RemoveIntersectionPoints();
        AllNoSpawnZones.Remove(this);
    }

    public static AdjustedPoint GetBestEdgePosition(Vector3 position, Vector3? previousGoodPosition = null, float leeway = 0.1f, float previousWeight = 0.5f, float maxJump = 20f)
    {
        var currentPoint = new AdjustedPoint(position, PointType.Original);
        var bestDistance = maxJump;

        if (previousGoodPosition != null && IsInAnyNoSpawnZone(previousGoodPosition.Value))
        {
            previousGoodPosition = null;    // the position isn't good anymore.
        }

        var transgressedZones = AllNoSpawnZones.Where(z => z.IsInNoSpawnZone(position, leeway));
        if (!transgressedZones.Any())
        {
            // Not in any no spawn zone
            // move into the allowed area if there is one
            return GetClosestPositionInAllowedArea(currentPoint);
        }

        var bestPoint = new AdjustedPoint(position, PointType.Invalid);    // initilise to the original, but we now know it's invalid.

        void keepIfBetter(AdjustedPoint otherPoint)
        {
            var otherPoointPosition = otherPoint.Point;
            var distance = (otherPoointPosition - position).magnitude - otherPoint.SnapPriority;
            var previousDistance = previousGoodPosition.HasValue
                ? (otherPoointPosition - previousGoodPosition.Value).magnitude
                : 0;
            var weightedTotalDistance = distance + (previousDistance * previousWeight);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = otherPoint;
                Debug.DrawLine(otherPoointPosition + (Vector3.up * distance), otherPoointPosition - (Vector3.up * distance), Color.green, 2);
            }
        }

        var pointsToCheck = transgressedZones.Select(z => GetClosestPointOnEdge(position, z, PointType.Corrected)).ToList();
        pointsToCheck.AddRange(_intersectionPoints.Where(p => p.IsOnEdge == true));
        pointsToCheck.AddRange(InteractionPoints);

        foreach (var point in pointsToCheck)
        {
            if (point.Type != PointType.InteractionPoint && IsInAnyNoSpawnZone(point.Point))
            {
                // This edge point is still in a no spawn zone, so ignore it (unless it's an interaction point)
                continue;
            }
            keepIfBetter(point);
        }

        // move the best point into the allowed area if there is one, and it's otside it.
        return GetClosestPositionInAllowedArea(bestPoint);
    }

    /// <summary>
    /// Determines the closest valid position within the allowed area to the specified position.
    /// </summary>
    /// <remarks>If the specified position is outside the allowed area, the method attempts to find the
    /// closest point  on the edge of the allowed area that is not within a restricted "no spawn zone." If no such point
    /// exists,  the method returns <see langword="null"/>.</remarks>
    /// <param name="position">The position to evaluate. Can be <see langword="null"/>.</param>
    /// <returns>The closest valid position within the allowed area, or <see langword="null"/> if the input position is  <see langword="null"/> or no valid position can be determined, or the position doens't need to be updated.</returns>
    private static AdjustedPoint GetClosestPositionInAllowedArea(AdjustedPoint position)
    {
        Debug.Assert(position != null, "Position should not be null.");
        var allowedArea = GetAllowedArea();
        if (allowedArea.HasValue && !IsInRadius(position.Point, allowedArea.Value))
        {
            position = new AdjustedPoint(position.Point, PointType.Invalid);    // we now know the position is invalid.
            //Debug.Log("Position is outside the allowed area, moving it in." + position.Value);
            // the position is outside the allowed area.
            var closestEdgePointInRange = GetClosestPointOnEdge(position.Point, allowedArea.Value, PointType.Corrected);

            if (!IsInAnyNoSpawnZone(closestEdgePointInRange.Point))
            {
                //Debug.Log("The closest point on the edge of the allowed area is not in a no spawn zone, so using that." + closestEdgePointInRange);
                // the closest point on the edge of the allowed area is not in a no spawn zone, so return that.
                return closestEdgePointInRange;
            }
        }

        // return the original position, if it's invalid, it will have been marked so, otherwise it's valid.
        return position;
    }

    private static (Vector3 center, float radius)? GetAllowedArea()
    {
        var wallNadeBeingPlaced = ObjectPlacer.Instance != null ? ObjectPlacer.Instance.WallNodeBeingPlaced : null;

        if (wallNadeBeingPlaced != null && wallNadeBeingPlaced.ConnectedNode != null)
        {
            var center = wallNadeBeingPlaced.ConnectedNode.transform.position;
            var radius = wallNadeBeingPlaced.MaxLength;
            return (center, radius);
        }
        return null;
    }

    private static AdjustedPoint GetClosestPointOnEdge(Vector3 position, NoSpawnZone zone, PointType type)
    {
        return GetClosestPointOnEdge(position, zone.transform.position, zone.Radius, type);
    }

    private static AdjustedPoint GetClosestPointOnEdge(Vector3 position, (Vector3 center, float radius) area, PointType type)
    {
        return GetClosestPointOnEdge(position, area.center, area.radius, type);
    }

    private static AdjustedPoint GetClosestPointOnEdge(Vector3 position, Vector3 center, float radius, PointType type)
    {
        var direction = position - center;
        direction = new Vector3(direction.x, 0, direction.z);    // move it down to the plane
        direction.Normalize();
        var newX = center.x + (direction.x * radius);
        var newZ = center.z + (direction.z * radius);
        var edgePoint = new Vector3(newX, 0, newZ);
        return new AdjustedPoint(edgePoint, type);
    }

    private void Update()
    {
        var previous = (Vector3?)null;
        for (int i = 0; i < 33; i++)
        {
            var target = this.transform.position + (new Vector3(Mathf.Cos(i * Mathf.PI / 16), 0, Mathf.Sin(i * Mathf.PI / 16)) * this.Radius);
            target = new Vector3(target.x, 0, target.z);
            if (previous != null)
            {
                Debug.DrawLine(previous.Value, target, Color.aliceBlue);
            }
            previous = target;
        }
    }

    public void AddIntersectionPoints()
    {
        foreach (var other in AllNoSpawnZones)
        {
            if (other == this) continue;

            var intersectionPoints = this.CalculateIntersectionPoints(other);
            foreach (var point in intersectionPoints)
            {
                var intersectionPoint = new IntersectionPoint(point, this, other);
                _intersectionPoints.Add(intersectionPoint);
            }
        }
        foreach (var intersection in _intersectionPoints)
        {
            intersection.IsOnEdge = !IsInAnyNoSpawnZone(intersection.Point);
            //Debug.DrawLine(intersection.Point + Vector3.up, intersection.Point - Vector3.up, intersection.IsOnEdge.Value ? Color.cyan : Color.magenta, 300);
        }
    }

    public void RemoveIntersectionPoints()
    {
    }

    public bool IsInNoSpawnZone(Vector3 position, float leeway = 0.1f)
    {
        var center = this.transform.position;
        var radius = this.Radius;

        return IsInRadius(position, center, radius, leeway);
    }

    private static bool IsInRadius(Vector3 position, (Vector3 center, float radius) area, float leeway = 0.1f)
    {
        return IsInRadius(position, area.center, area.radius, leeway);
    }
    private static bool IsInRadius(Vector3 position, Vector3 center, float radius, float leeway = 0.1f)
    {
        var vector = position - center;
        vector = new Vector3(vector.x, 0, vector.z);    // move it down to the plane
        var isInside = (vector.magnitude + leeway) < radius;
        return isInside;
    }

    public static bool IsInAnyNoSpawnZone(Vector3 position)
    {
        foreach (var zone in AllNoSpawnZones)
        {
            if (zone.IsInNoSpawnZone(position))
            {
                return true;
            }
        }
        return false;
    }

    private List<Vector3> CalculateIntersectionPoints(NoSpawnZone other)
    {
        var points = new List<Vector3>();

        float dx = this.transform.position.x - other.transform.position.x;
        float dz = this.transform.position.z - other.transform.position.z; // Use z for 2D plane
        float d = Mathf.Sqrt((dx * dx) + (dz * dz));
        // No solution: circles are separate or one is contained within the other
        if (
            d > this.Radius + other.Radius // The circles are too far appart and don't touch
            || d < Mathf.Abs(this.Radius - other.Radius) // One circle is entirely within the other
            || d <= 0.001f  // the circles have the same center point (with a little fudge factor)
            )
        {
            return points; // No intersection points
        }

        // l is the distance from this to the point between the two intersection points
        float l = ((this.Radius * this.Radius) - (other.Radius * other.Radius) + (d * d)) / (2 * d);
        var vectorToL = (other.transform.position - this.transform.position) * l / d;
        // lPosition is the position of the point on the line between the centers directly between the two intersects
        var lPosition = new Vector3(this.transform.position.x + vectorToL.x, 0, this.transform.position.z + vectorToL.z);
        //Debug.DrawLine(this.transform.position, lPosition, Color.red, 300);

        // see https://math.stackexchange.com/questions/256100/how-can-i-find-the-points-at-which-two-circles-intersect
        float h = Mathf.Sqrt((this.Radius * this.Radius) - (l * l));    // the remaining side of the triangle between the center, L and the intersection point.
        // Find P2
        float x2 = lPosition.x;
        float z2 = lPosition.z; // Use z for 2D plane
        // Intersection points
        var intersection1 = new Vector3(
            x2 + (h * dz / d),
            0,
            z2 - (h * dx / d)
        );
        var intersection2 = new Vector3(
            x2 - (h * dz / d),
            0,
            z2 + (h * dx / d)
        );
        points.Add(intersection1);
        points.Add(intersection2);

        //Debug.DrawLine(this.transform.position, intersection1, Color.yellow, 300);
        //Debug.DrawLine(this.transform.position, intersection2, Color.green, 300);
        return points;
    }

    public override void Ghostify()
    {
        this.Deactivate();
    }

    public override void UnGhostify()
    {
        this.Activate();
    }

    internal static void Register(IInteractivePosition point)
    {
        InteractionPoints.Add(new InteractionPoint(point));
    }

    private class IntersectionPoint : AdjustedPoint
    {
        public readonly NoSpawnZone ZoneA;
        public readonly NoSpawnZone ZoneB;
        public bool? IsOnEdge;

        public IntersectionPoint(Vector3 point, NoSpawnZone noSpawnZone, NoSpawnZone other) : base(point, PointType.Corrected)
        {
            this.ZoneA = noSpawnZone;
            this.ZoneB = other;
        }
    }
}
