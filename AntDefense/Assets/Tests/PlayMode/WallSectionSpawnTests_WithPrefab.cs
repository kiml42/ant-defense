using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runs the full spawn test suite using the real WallSection and WallStump prefabs.
/// SectionLength is read from the prefab, so these tests stay in sync with the asset
/// automatically — if the prefab's SectionLength changes, the expected counts and positions
/// update without any edits to the test code.
/// </summary>
public class WallSectionSpawnTests_WithPrefab : WallSectionSpawnTestsBase
{
    private const string WallSectionPrefabPath = "Assets/Prefabs/Placeables/WallSection.prefab";
    private const string WallStumpPrefabPath   = "Assets/Prefabs/Placeables/WallStump.prefab";

    private WallSection _sectionPrefab;
    private GameObject  _stumpPrefab;

    protected override void SetUpFixture()
    {
#if UNITY_EDITOR
        var sectionPrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(WallSectionPrefabPath);
        Assert.IsNotNull(sectionPrefabGO, $"Could not load WallSection prefab at {WallSectionPrefabPath}");
        _sectionPrefab = sectionPrefabGO.GetComponentInChildren<WallSection>(includeInactive: true);
        Assert.IsNotNull(_sectionPrefab, "WallSection prefab has no WallSection component");
        SectionLength = _sectionPrefab.SectionLength;

        _stumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallStumpPrefabPath);
        Assert.IsNotNull(_stumpPrefab, $"Could not load WallStump prefab at {WallStumpPrefabPath}");
#endif
    }

    protected override WallNode CreateWallSetup(Vector3 start, Vector3 end, bool withStumpPrefab = false)
    {
        var connectedGO = new GameObject("ConnectedNode");
        connectedGO.transform.position = end;
        var connectedNode = connectedGO.AddComponent<WallNode>();
        var connectedGhostGO = new GameObject("ConnectedWallGhost");
        connectedNode.WallGhost = connectedGhostGO.transform;
        ToDestroy.Add(connectedGO);
        ToDestroy.Add(connectedGhostGO);

        var ghostGO = new GameObject("WallGhost");
        ToDestroy.Add(ghostGO);

        var wallNodeGO = new GameObject("WallNode");
        wallNodeGO.transform.position = start;
        var wallNode = wallNodeGO.AddComponent<WallNode>();
        wallNode.SectionPrefab = _sectionPrefab;
        wallNode.ConnectedNode = connectedNode;
        wallNode.WallGhost = ghostGO.transform;
        ToDestroy.Add(wallNodeGO);

        if (withStumpPrefab)
            wallNode.StumpPrefab = _stumpPrefab;

        return wallNode;
    }

    [UnityTest]
    public IEnumerator SpawnSections_StumpsAnimateOnBuild()
    {
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance), withStumpPrefab: true);
        wallNode.OnBuildStart();

        // Wait past the stump's StartDelay so the animation makes progress
        yield return new WaitForSeconds(1f);

        var stumps = GetSpawnedStumps(wallNode);
        Assert.AreEqual(2, stumps.Count, "Expected 2 stumps");

        foreach (var stump in stumps)
        {
            var anim = stump.GetComponentInChildren<BaseBuildAnimation>(includeInactive: true);
            Assert.IsNotNull(anim, $"Stump '{stump.name}' has no BaseBuildAnimation component");
            Assert.IsTrue(anim.HasStartedAnimating, $"Stump '{stump.name}' animation has not started");
        }
    }
}
