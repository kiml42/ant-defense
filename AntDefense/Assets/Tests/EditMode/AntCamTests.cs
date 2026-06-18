using NUnit.Framework;
using UnityEngine;

public class AntCamTests
{
    [Test]
    public void GroundPointUnderRay_RayStraightDown_ReturnsOriginOnGround()
    {
        var ray = new Ray(new Vector3(5f, 20f, 3f), Vector3.down);
        var result = AntCam.GroundPointUnderRay(ray);
        Assert.AreEqual(new Vector3(5f, 0f, 3f), result);
    }

    [Test]
    public void GroundPointUnderRay_DiagonalRay_ReturnsCorrectGroundPoint()
    {
        // Ray from (0, 10, 0) pointing toward (1, -1, 0) normalised
        var direction = new Vector3(1f, -1f, 0f).normalized;
        var ray = new Ray(new Vector3(0f, 10f, 0f), direction);
        var result = AntCam.GroundPointUnderRay(ray);

        // t such that 10 + t * direction.y = 0  =>  t = 10 / |direction.y|
        // Since direction = (1,-1,0)/sqrt(2), direction.y = -1/sqrt(2)
        // t = 10 / (1/sqrt(2)) = 10*sqrt(2)
        // x = 0 + 10*sqrt(2) * (1/sqrt(2)) = 10
        Assert.That(result.y, Is.EqualTo(0f).Within(1e-4f));
        Assert.That(result.x, Is.EqualTo(10f).Within(1e-4f));
        Assert.That(result.z, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void GroundPointUnderRay_RayParallelToGround_ReturnsRayOrigin()
    {
        var ray = new Ray(new Vector3(1f, 5f, 2f), Vector3.forward);
        var result = AntCam.GroundPointUnderRay(ray);
        Assert.AreEqual(ray.origin, result);
    }

    [Test]
    public void GroundPointUnderRay_ResultAlwaysAtYZero()
    {
        // Various origins and downward-ish directions should all hit y=0
        var cases = new[]
        {
            new Ray(new Vector3(0f,  50f, 0f),  new Vector3( 0.3f, -1f,  0.1f).normalized),
            new Ray(new Vector3(10f, 100f, -5f), new Vector3(-0.5f, -1f,  0.5f).normalized),
            new Ray(new Vector3(-3f, 15f, 7f),   new Vector3( 0f,   -1f,  0f  ).normalized),
        };

        foreach (var ray in cases)
        {
            var result = AntCam.GroundPointUnderRay(ray);
            Assert.That(result.y, Is.EqualTo(0f).Within(1e-4f), $"Expected y=0 for ray {ray}");
        }
    }

    [Test]
    public void GroundPointUnderRay_DragAnchorStaysFixed()
    {
        // Simulates the camera pan logic: moving camera by -delta should keep anchor under cursor.
        // Anchor was recorded at world point A from camera position C0.
        // Camera moves to C1 = C0 + (A - currentGroundPoint(C1)).
        // We verify that after the move the anchor is correctly under the cursor.

        var cameraHeight = 30f;
        var anchorWorld = new Vector3(5f, 0f, 8f);

        // Initial camera at origin (xz), height 30, pointing straight down
        var initialCamPos = new Vector3(0f, cameraHeight, 0f);
        var direction = Vector3.down;

        // Mouse moves: ray now shoots from offset camera toward a different screen point,
        // but we model it simply — camera hasn't moved yet, ray origin shifts.
        // New mouse screen ray from same camera but angled slightly
        var newDirection = new Vector3(0.2f, -1f, 0.1f).normalized;
        var rayFromCurrentCam = new Ray(initialCamPos, newDirection);
        var currentGroundPoint = AntCam.GroundPointUnderRay(rayFromCurrentCam);

        // Camera should move by (anchorWorld - currentGroundPoint) in XZ
        var delta = anchorWorld - currentGroundPoint;
        var newCamPos = initialCamPos + new Vector3(delta.x, 0f, delta.z);

        // With the camera at newCamPos, the same ray direction should now hit anchorWorld
        var verifyRay = new Ray(newCamPos, newDirection);
        var verifyPoint = AntCam.GroundPointUnderRay(verifyRay);

        Assert.That(verifyPoint.x, Is.EqualTo(anchorWorld.x).Within(1e-4f));
        Assert.That(verifyPoint.z, Is.EqualTo(anchorWorld.z).Within(1e-4f));
        Assert.That(verifyPoint.y, Is.EqualTo(0f).Within(1e-4f));
    }
}
