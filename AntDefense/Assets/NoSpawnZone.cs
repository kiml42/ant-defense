using System.Collections.Generic;
using UnityEngine;

public class NoSpawnZone : MonoBehaviour
{
    public static HashSet<NoSpawnZone> AllNoSpawnZones = new HashSet<NoSpawnZone>();
    private readonly List<IntersectionPoint> _intersectionPoints = new List<IntersectionPoint>();

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

    private void Update()
    {
        var previous = (Vector3?)null;
        for (int i = 0; i < 33; i++)
        {
            var target = this.transform.position + new Vector3(Mathf.Cos(i * Mathf.PI / 16), 0, Mathf.Sin(i * Mathf.PI / 16)) * Radius;
            target = new Vector3(target.x, 0, target.z);
            if(previous != null)
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
    }

    public void RemoveIntersectionPoints()
    {
    }

    private List<Vector3> CalculateIntersectionPoints(NoSpawnZone other)
    {
        var points = new List<Vector3>();
        float dx = this.transform.position.x - other.transform.position.x;
        float dz = this.transform.position.z - other.transform.position.z; // Use z for 2D plane
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        Debug.Log("dx = " + dx + ", dy = " + dz + ", d = " + d);
        // No solution: circles are separate or one is contained within the other
        if (d > this.Radius + other.Radius || d < Mathf.Abs(this.Radius - other.Radius) || d == 0f)
        {
            return points; // No intersection points
        }


        // l is the distance from this to the point between the two intersection points
        float l = ((this.Radius * this.Radius) - (other.Radius * other.Radius) + (d * d)) / (2 * d);
        Debug.Log("l = " + l);
        var vectorToA = (other.transform.position - this.transform.position) * l/d;
        var aPosition = new Vector3(this.transform.position.x + vectorToA.x, 0, this.transform.position.z + vectorToA.z);
        Debug.Log("aPosition = " + aPosition);
        Debug.DrawLine(this.transform.position, aPosition, Color.red, 300);

        // see https://math.stackexchange.com/questions/256100/how-can-i-find-the-points-at-which-two-circles-intersect
        // TODO: check all the maths makes sense from here (it doesn't seem to work)
        float h = Mathf.Sqrt((this.Radius * this.Radius) - (l * l));
        // Find P2
        float x2 = this.transform.position.x + (l * dx / d);
        float z2 = this.transform.position.z + (l * dz / d); // Use z for 2D plane
        var p2 = new Vector3(x2, 0, z2);
        Debug.Log("h = " + h + ", P2 = " + p2);
        // Intersection points
        var intersection1 = new Vector3(
            ((l/d)*dx) + ((h/d)*dz) + this.transform.position.x,
            0,
            ((l / d) * dz) - ((h / d) * dx) + this.transform.position.y
        );
        var intersection2 = new Vector3(
            ((l / d) * dx) - ((h / d) * dz) + this.transform.position.x,
            0,
            ((l / d) * dz) + ((h / d) * dx) + this.transform.position.y
        );
        points.Add(intersection1);
        points.Add(intersection2);

        Debug.DrawLine(this.transform.position, intersection1, Color.yellow, 300);
        Debug.DrawLine(this.transform.position, intersection2, Color.yellow, 300);
        return points;
    }



    private struct IntersectionPoint
    {
        public Vector3 Point;
        public NoSpawnZone ZoneA;
        public NoSpawnZone ZoneB;
        public bool? IsOnEdge;
    }
}
