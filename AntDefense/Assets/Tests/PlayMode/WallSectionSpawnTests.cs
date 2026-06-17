using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the full spawn test suite using hand-built GameObjects with known values,
/// making the tests independent of any particular prefab configuration.
/// </summary>
public class WallSectionSpawnTests : WallSectionSpawnTestsBase
{
    protected override void SetUpFixture()
    {
        SectionLength = 2f;
    }

    protected override WallNode CreateWallSetup(Vector3 start, Vector3 end, bool withStumpPrefab = false)
    {
        var sectionPrefabGO = new GameObject("SectionPrefab");
        var sectionComp = sectionPrefabGO.AddComponent<WallSection>();
        sectionComp.SectionLength = SectionLength;
        sectionPrefabGO.AddComponent<PlaceableRealObject>();
        ToDestroy.Add(sectionPrefabGO);

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
        wallNode.SectionPrefab = sectionComp;
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
