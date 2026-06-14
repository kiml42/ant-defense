using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AntMovementTests
{
    private GameObject _ant;
    private GameObject _target;

    [SetUp]
    public void SetUp()
    {
        _ant = new GameObject();
        _ant.AddComponent<Rigidbody>();
        _ant.AddComponent<AntTargetPositionProvider>();
        _ant.AddComponent<AntMoveController>();

        _target = new GameObject();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_ant);
        Object.Destroy(_target);
    }

    [UnityTest]
    public IEnumerator AntMovesWhenNoTarget()
    {
        // Without a target the ant wanders — it should not stay perfectly still
        var startPosition = _ant.transform.position;

        yield return new WaitForSeconds(0.5f);

        Assert.AreNotEqual(startPosition, _ant.transform.position);
    }

    [UnityTest]
    public IEnumerator AntMovesTowardsTarget()
    {
        // Place target well ahead of the ant
        _target.transform.position = new Vector3(0, 0, 20f);

        var positionProvider = _ant.GetComponent<AntTargetPositionProvider>();

        // AntNestSmell is the simplest concrete Smellable with no required dependencies
        var smellable = _target.AddComponent<AntNestSmell>();
        smellable.TargetPoint = _target.transform;

        positionProvider.SetTarget(smellable);

        var startZ = _ant.transform.position.z;

        yield return new WaitForSeconds(0.5f);

        Assert.Greater(_ant.transform.position.z, startZ);
    }
}
