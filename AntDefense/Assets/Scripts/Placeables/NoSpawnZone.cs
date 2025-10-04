using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class NoSpawnZone : BaseGhostable
{
    public static HashSet<NoSpawnZone> AllNoSpawnZones = new HashSet<NoSpawnZone>();
    private static readonly List<IntersectionPoint> _intersectionPoints = new List<IntersectionPoint>();

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

    public static Vector3? GetBestEdgePosition(Vector3 position, Vector3? previousGoodPosition = null, float leeway = 0.1f, float previousWeight = 0.5f, float maxJump = 20f)
    {
        var bestPoint = (Vector3?)null;
        var bestDistance = maxJump;

        if (previousGoodPosition != null && IsInAnyNoSpawnZone(previousGoodPosition.Value))
        {
            previousGoodPosition = null;    // the position isn't good anymore.
        }

        var allowedArea = GetAllowedArea();
        if (allowedArea.HasValue && !IsInRadius(position, allowedArea.Value))
        {
            // the position is outside the allowed area.
            var closestEdgePointInRange = GetClosestPointOnEdge(position, allowedArea.Value);
            if (!IsInAnyNoSpawnZone(closestEdgePointInRange))
            {
                // the closest point on the edge of the allowed area is not in a no spawn zone, so return that.
                return closestEdgePointInRange;
            }
            else
            {
                // this point is still in a no spawn zone, return null to say this can't find a good position.
                return null;
                // TODO: consider checking the intersections between this circle and all the no spawn zones.
                // Get list of all the intersections between this circle and the no spawn zones.
                // discard those that are in a no spawn zone.
                // add these to the list of points to check.
                // This might be too heavy to do every frame, so it might need caching if the allowed area doesn't change, or just not worth bothering with.
            }
        }

        var transgressedZones = AllNoSpawnZones.Where(z => z.IsInNoSpawnZone(position, leeway));
        if (!transgressedZones.Any() && (allowedArea == null || IsInRadius(position, allowedArea.Value)))
        {
            // Not in any no spawn zone, and inside the allowed area.
            // so nothing to do.
            return null;
        }

        Action<Vector3> keepIfBetter = (v) =>
        {
            var distance = (v - position).magnitude;
            var previousDistance = previousGoodPosition.HasValue
                ? (v - previousGoodPosition.Value).magnitude
                : 0;
            var weightedTotalDistance = distance + (previousDistance * previousWeight);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = v;
                Debug.DrawLine(v + (Vector3.up * distance), v - (Vector3.up * distance), Color.green, 2);
            }
        };

        var pointsToCheck = transgressedZones.Select(z => GetClosestPointOnEdge(position, z)).ToList();
        pointsToCheck.AddRange(_intersectionPoints.Where(p => p.IsOnEdge == true).Select(i => i.Point));


        foreach(var edgePoint in pointsToCheck)
        {
            if (IsInAnyNoSpawnZone(edgePoint))
            {
                // This edge point is still in a no spawn zone, so ignore it.
                continue;
            }
            keepIfBetter(edgePoint);
        }
        return bestPoint;
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

    private static Vector3 GetClosestPointOnEdge(Vector3 position, NoSpawnZone zone)
    {
        return GetClosestPointOnEdge(position, zone.transform.position, zone.Radius);
    }

    private static Vector3 GetClosestPointOnEdge(Vector3 position, (Vector3 center, float radius) area)
    {
        return GetClosestPointOnEdge(position, area.center, area.radius);
    }

    private static Vector3 GetClosestPointOnEdge(Vector3 position, Vector3 center, float radius)
    {
        var direction = position - center;
        direction = new Vector3(direction.x, 0, direction.z);    // move it down to the plane
        direction.Normalize();
        var newX = center.x + direction.x * radius;
        var newZ = center.z + direction.z * radius;
        var edgePoint = new Vector3(newX, 0, newZ);
        return edgePoint;
    }

    private void Update()
    {
        var previous = (Vector3?)null;
        for (int i = 0; i < 33; i++)
        {
            var target = this.transform.position + new Vector3(Mathf.Cos(i * Mathf.PI / 16), 0, Mathf.Sin(i * Mathf.PI / 16)) * this.Radius;
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
                var intersectionPoint = new IntersectionPoint
                {
                    Point = point,
                    ZoneA = this,
                    ZoneB = other
                };
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
        float d = Mathf.Sqrt(dx * dx + dz * dz);
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

    private class IntersectionPoint
    {
        public Vector3 Point;
        public NoSpawnZone ZoneA;
        public NoSpawnZone ZoneB;
        public bool? IsOnEdge;
    }
}
