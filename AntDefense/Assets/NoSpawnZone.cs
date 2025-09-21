using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoSpawnZone : MonoBehaviour
{
    public static HashSet<NoSpawnZone> AllNoSpawnZones = new HashSet<NoSpawnZone>();
    private static readonly List<IntersectionPoint> _intersectionPoints = new List<IntersectionPoint>();

    public float Radius = 3;

    void Start()
    {
        var collider = this.GetComponent<SphereCollider>();
        if (collider != null)
        {
            this.Radius = collider.radius;
        }

        AllNoSpawnZones.Add(this);
        AddIntersectionPoints();
    }

    private void OnDestroy()
    {
        RemoveIntersectionPoints();
        AllNoSpawnZones.Remove(this);
    }

    // TODO: Fix bug where a ghost separated from existing towers hops the no spawn zone over to existing towers.
    public static Vector3? GetBestEdgePosition(Vector3 position, Vector3? previousGoodPosition = null, float leeway = 0.1f, float previousWeight = 0.5f, float maxJump = 20f)
    {
        var bestPoint = (Vector3?)null;
        var bestDistance = maxJump;

        if (previousGoodPosition != null && NoSpawnZone.IsInAnyNoSpawnZone(previousGoodPosition.Value))
        {
            previousGoodPosition = null;    // the position isn't good anymore.
        }

        var transgressedZones = AllNoSpawnZones.Where(z => z.IsInNoSpawnZone(position, leeway));
        if (!transgressedZones.Any())
        {
            // Not in any no spawn zone, so nothing to do.
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

        foreach (var zone in transgressedZones)
        {
            var edgePoint = GetClosestPointOnEdge(position, zone);
            if (IsInAnyNoSpawnZone(edgePoint))
            {
                // This edge point is still in a no spawn zone, so ignore it.
                continue;
            }
            keepIfBetter(edgePoint);
        }
        foreach (var intersect in _intersectionPoints.Where(p => p.IsOnEdge == true))
        {
            keepIfBetter(intersect.Point);
        }
        return bestPoint;
    }

    private static Vector3 GetClosestPointOnEdge(Vector3 position, NoSpawnZone zone)
    {
        var direction = (position - zone.transform.position);
        direction = new Vector3(direction.x, 0, direction.z);    // move it down to the plane
        direction.Normalize();
        var edgePoint = zone.transform.position + direction * zone.Radius;
        return edgePoint;
    }

    private void Update()
    {
        var previous = (Vector3?)null;
        for (int i = 0; i < 33; i++)
        {
            var target = this.transform.position + new Vector3(Mathf.Cos(i * Mathf.PI / 16), 0, Mathf.Sin(i * Mathf.PI / 16)) * Radius;
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

            var intersectionPoints = CalculateIntersectionPoints(other);
            foreach (var point in intersectionPoints)
            {
                var intersectionPoint = new IntersectionPoint
                {
                    Point = point,
                    ZoneA = this,
                    ZoneB = other,
                    IsOnEdge = null // TODO: Determine if the point is on the edge
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
        var vector = position - this.transform.position;
        vector = new Vector3(vector.x, 0, vector.z);    // move it down to the plane
        var isInside = (vector.magnitude + leeway) < this.Radius;
        //if (isInside)
        //{
        //    Debug.Log(position + " is inside circle. distance = " + vector.magnitude + ", r = " + this.Radius + ", leeway = " + leeway);
        //}
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
        // TODO: check all the maths makes sense from here (it doesn't seem to work)
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



    private class IntersectionPoint
    {
        public Vector3 Point;
        public NoSpawnZone ZoneA;
        public NoSpawnZone ZoneB;
        public bool? IsOnEdge;
    }
}
