using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runs the full spawn test suite using the real WallSection prefab as the section source.
/// SectionLength is read from the prefab, so these tests stay in sync with the asset
/// automatically — if the prefab's SectionLength changes, the expected counts and positions
/// update without any edits to the test code.
/// </summary>
public class WallSectionSpawnTests_WithPrefab : WallSectionSpawnTestsBase
{
    private const string WallSectionPrefabPath = "Assets/Prefabs/Placeables/WallSection.prefab";

    private WallSection _sectionPrefab;

    protected override void SetUpFixture()
    {
#if UNITY_EDITOR
        var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(WallSectionPrefabPath);
        Assert.IsNotNull(prefabGO, $"Could not load WallSection prefab at {WallSectionPrefabPath}");

        _sectionPrefab = prefabGO.GetComponentInChildren<WallSection>(includeInactive: true);
        Assert.IsNotNull(_sectionPrefab, "WallSection prefab has no WallSection component");

        SectionLength = _sectionPrefab.SectionLength;
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
        {
            var stumpGO = new GameObject("StumpPrefab");
            wallNode.StumpPrefab = stumpGO;
            ToDestroy.Add(stumpGO);
        }

        return wallNode;
    }
}
