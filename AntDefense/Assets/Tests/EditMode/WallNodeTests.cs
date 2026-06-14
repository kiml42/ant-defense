using NUnit.Framework;
using UnityEngine;

public class WallNodeTests
{
    private GameObject _goA;
    private GameObject _goB;
    private WallNode _nodeA;
    private WallNode _nodeB;

    [SetUp]
    public void SetUp()
    {
        _goA = new GameObject();
        _goB = new GameObject();
        _nodeA = _goA.AddComponent<WallNode>();
        _nodeB = _goB.AddComponent<WallNode>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_goA);
        Object.DestroyImmediate(_goB);
    }

    // --- AdditionalCost ---

    [Test]
    public void AdditionalCost_IsZero_WhenNoConnectedNode()
    {
        Assert.AreEqual(0f, _nodeA.AdditionalCost);
    }

    [Test]
    public void AdditionalCost_ScalesWithDistance()
    {
        _nodeA.CostPerMeter = 1f;
        _goB.transform.position = new Vector3(0, 0, 8f);
        _nodeA.ConnectedNode = _nodeB;

        Assert.AreEqual(8f, _nodeA.AdditionalCost, 0.001f);
    }

    [Test]
    public void AdditionalCost_ScalesWithCostPerMeter()
    {
        _nodeA.CostPerMeter = 3f;
        _goB.transform.position = new Vector3(0, 0, 5f);
        _nodeA.ConnectedNode = _nodeB;

        Assert.AreEqual(15f, _nodeA.AdditionalCost, 0.001f);
    }

    [Test]
    public void AdditionalCost_IsSymmetric()
    {
        _nodeA.CostPerMeter = 2f;
        _nodeB.CostPerMeter = 2f;
        _goB.transform.position = new Vector3(0, 0, 6f);
        _nodeA.ConnectedNode = _nodeB;
        _nodeB.ConnectedNode = _nodeA;

        Assert.AreEqual(_nodeA.AdditionalCost, _nodeB.AdditionalCost, 0.001f);
    }

    // --- PositionIsValid ---

    [Test]
    public void PositionIsValid_ReturnsTrue_WhenNoConnectedNode()
    {
        // Without a connected node any position is valid
        Assert.IsTrue(_nodeA.PositionIsValid(new Vector3(1000, 0, 1000)));
    }

    [Test]
    public void PositionIsValid_ReturnsTrue_WhenWithinMaxLength()
    {
        _nodeA.MaxLength = 10f;
        _goB.transform.position = new Vector3(0, 0, 5f);
        _nodeA.ConnectedNode = _nodeB;

        Assert.IsTrue(_nodeA.PositionIsValid(new Vector3(0, 0, 2f)));
    }

    [Test]
    public void PositionIsValid_ReturnsFalse_WhenBeyondMaxLength()
    {
        _nodeA.MaxLength = 5f;
        _goB.transform.position = new Vector3(0, 0, 3f);
        _nodeA.ConnectedNode = _nodeB;

        // Position 20 units from connected node — well over the 5-unit limit
        Assert.IsFalse(_nodeA.PositionIsValid(new Vector3(0, 0, 20f)));
    }

    [Test]
    public void PositionIsValid_ReturnsFalse_JustBeyondMaxLength()
    {
        _nodeA.MaxLength = 5f;
        _goB.transform.position = Vector3.zero;
        _nodeA.ConnectedNode = _nodeB;

        // MaxLength + 0.1 is the boundary; just beyond it should fail
        Assert.IsFalse(_nodeA.PositionIsValid(new Vector3(0, 0, 5.2f)));
    }

    // --- WallSection.PositionIsValid ---

    [Test]
    public void WallSection_PositionIsValid_AlwaysReturnsTrue()
    {
        var go = new GameObject();
        var section = go.AddComponent<WallSection>();

        Assert.IsTrue(section.PositionIsValid(Vector3.zero));
        Assert.IsTrue(section.PositionIsValid(new Vector3(1000, 1000, 1000)));

        Object.DestroyImmediate(go);
    }
}
