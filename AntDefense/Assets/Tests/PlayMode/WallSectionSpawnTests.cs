using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WallSectionSpawnTests
{
    private const float SectionLength = 2f;

    private List<GameObject> _toDestroy;

    [SetUp]
    public void SetUp()
    {
        _toDestroy = new List<GameObject>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _toDestroy)
            if (go != null) Object.Destroy(go);
    }

    // Creates a WallNode ready to spawn sections, with ConnectedNode placed at 'end'.
    private WallNode CreateWallSetup(Vector3 start, Vector3 end, bool withStumpPrefab = false)
    {
        // Section prefab — needs WallSection and a concrete PlaceableObjectOrGhost
        var sectionPrefabGO = new GameObject("SectionPrefab");
        var sectionComp = sectionPrefabGO.AddComponent<WallSection>();
        sectionComp.SectionLength = SectionLength;
        sectionPrefabGO.AddComponent<PlaceableRealObject>();
        _toDestroy.Add(sectionPrefabGO);

        // Connected node — also needs a WallGhost so its own Update() doesn't assert
        var connectedGO = new GameObject("ConnectedNode");
        connectedGO.transform.position = end;
        var connectedNode = connectedGO.AddComponent<WallNode>();
        var connectedGhostGO = new GameObject("ConnectedWallGhost");
        connectedNode.WallGhost = connectedGhostGO.transform;
        _toDestroy.Add(connectedGO);
        _toDestroy.Add(connectedGhostGO);

        // Wall ghost for the main node
        var ghostGO = new GameObject("WallGhost");
        _toDestroy.Add(ghostGO);

        // Main wall node
        var wallNodeGO = new GameObject("WallNode");
        wallNodeGO.transform.position = start;
        var wallNode = wallNodeGO.AddComponent<WallNode>();
        wallNode.SectionPrefab = sectionComp;
        wallNode.ConnectedNode = connectedNode;
        wallNode.WallGhost = ghostGO.transform;
        _toDestroy.Add(wallNodeGO);

        if (withStumpPrefab)
        {
            var stumpGO = new GameObject("StumpPrefab");
            wallNode.StumpPrefab = stumpGO;
            _toDestroy.Add(stumpGO);
        }

        return wallNode;
    }

    // Returns the spawned WallSection children, skipping the ghost if not yet destroyed.
    private List<WallSection> GetSpawnedSections(WallNode wallNode)
    {
        var sections = new List<WallSection>();
        foreach (Transform child in wallNode.transform)
        {
            var s = child.GetComponent<WallSection>();
            if (s != null) sections.Add(s);
        }
        return sections;
    }

    // --- Section count ---

    [UnityTest]
    public IEnumerator SpawnSections_CorrectCount_ExactMultiple()
    {
        // 6 / 2 = exactly 3 sections, no remainder
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 6f));
        wallNode.OnBuildStart();
        yield return null; // let Destroy(WallGhost) settle

        Assert.AreEqual(3, GetSpawnedSections(wallNode).Count);
    }

    [UnityTest]
    public IEnumerator SpawnSections_CorrectCount_WithRemainder()
    {
        // floor(7 / 2) = 3 sections, 1 unit remainder split as 0.5 gap at each end
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 7f));
        wallNode.OnBuildStart();
        yield return null;

        Assert.AreEqual(3, GetSpawnedSections(wallNode).Count);
    }

    [UnityTest]
    public IEnumerator SpawnSections_NoSections_WhenDistanceSmallerThanSectionLength()
    {
        // Distance 1 < SectionLength 2 — sectionCount == 0, should bail early
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 1f));
        wallNode.OnBuildStart();
        yield return null;

        Assert.AreEqual(0, GetSpawnedSections(wallNode).Count);
    }

    [UnityTest]
    public IEnumerator SpawnSections_NoSections_WhenNodesCoincide()
    {
        var wallNode = CreateWallSetup(Vector3.zero, Vector3.zero);
        wallNode.OnBuildStart();
        yield return null;

        Assert.AreEqual(0, GetSpawnedSections(wallNode).Count);
    }

    // --- Positions ---

    [UnityTest]
    public IEnumerator SpawnSections_SectionsPositionedAtCorrectIntervals()
    {
        // 6 units, 3 sections of length 2, halfGap = 0
        // Expected z-centres: 1, 3, 5
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 6f));
        wallNode.OnBuildStart();
        yield return null;

        var sections = GetSpawnedSections(wallNode);
        sections.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        Assert.AreEqual(1f, sections[0].transform.position.z, 0.01f);
        Assert.AreEqual(3f, sections[1].transform.position.z, 0.01f);
        Assert.AreEqual(5f, sections[2].transform.position.z, 0.01f);
    }

    [UnityTest]
    public IEnumerator SpawnSections_SectionsSymmetric_WhenRemainderExists()
    {
        // 7 units, 3 sections of length 2, halfGap = 0.5
        // Expected z-centres: 1.5, 3.5, 5.5
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 7f));
        wallNode.OnBuildStart();
        yield return null;

        var sections = GetSpawnedSections(wallNode);
        sections.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        var gapAtStart = sections[0].transform.position.z - SectionLength / 2f;
        var gapAtEnd = 7f - (sections[^1].transform.position.z + SectionLength / 2f);

        Assert.AreEqual(gapAtStart, gapAtEnd, 0.01f, "Gap at each end should be equal");
    }

    [UnityTest]
    public IEnumerator SpawnSections_SectionsDoNotExtendBeyondNodes()
    {
        // No section should start before nodeA or end after nodeB
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 7f));
        wallNode.OnBuildStart();
        yield return null;

        foreach (var section in GetSpawnedSections(wallNode))
        {
            var sectionStart = section.transform.position.z - SectionLength / 2f;
            var sectionEnd   = section.transform.position.z + SectionLength / 2f;

            Assert.GreaterOrEqual(sectionStart, 0f - 0.01f, "Section starts before nodeA");
            Assert.LessOrEqual(sectionEnd,      7f + 0.01f, "Section ends after nodeB");
        }
    }

    // --- Orientation ---

    [UnityTest]
    public IEnumerator SpawnSections_SectionsFaceFromNodeToNode()
    {
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 6f));
        wallNode.OnBuildStart();
        yield return null;

        foreach (var section in GetSpawnedSections(wallNode))
        {
            // Forward should point along +Z (from start to end)
            Assert.AreEqual(0f, section.transform.forward.x, 0.01f);
            Assert.AreEqual(1f, section.transform.forward.z, 0.01f);
        }
    }

    [UnityTest]
    public IEnumerator SpawnSections_SectionsFaceCorrectly_DiagonalWall()
    {
        // Wall going diagonally — sections should still face along the wall direction
        var end = new Vector3(6f, 0, 6f);
        var wallNode = CreateWallSetup(Vector3.zero, end);
        wallNode.OnBuildStart();
        yield return null;

        var expectedForward = (end - Vector3.zero).normalized;
        foreach (var section in GetSpawnedSections(wallNode))
        {
            Assert.AreEqual(expectedForward.x, section.transform.forward.x, 0.01f);
            Assert.AreEqual(expectedForward.z, section.transform.forward.z, 0.01f);
        }
    }

    // --- Stump ---

    [UnityTest]
    public IEnumerator SpawnSections_NoStump_WhenExactMultiple()
    {
        // halfGap == 0, no stump should be created even if StumpPrefab is assigned
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 6f), withStumpPrefab: true);
        wallNode.OnBuildStart();
        yield return null;

        // Only WallSection children — no extra stump child
        int sectionCount = GetSpawnedSections(wallNode).Count;
        Assert.AreEqual(sectionCount, wallNode.transform.childCount,
            "No stump child expected when distance is an exact multiple of SectionLength");
    }

    [UnityTest]
    public IEnumerator SpawnSections_SpawnsStump_WhenRemainderExistsAndPrefabSet()
    {
        // 7 units → halfGap = 0.5, one stump at each end (nodeA and nodeB sides)
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 7f), withStumpPrefab: true);
        wallNode.OnBuildStart();
        yield return null;

        int sectionCount = GetSpawnedSections(wallNode).Count;
        int totalChildren = wallNode.transform.childCount;

        // TODO: currently only one stump is spawned (at nodeA end); spawn one at nodeB end too
        Assert.AreEqual(sectionCount + 2, totalChildren, "Expected one stump at each end of the wall");
    }

    [UnityTest]
    public IEnumerator SpawnSections_NoStump_WhenPrefabNotSet()
    {
        // Remainder exists but StumpPrefab is null — no stump should appear
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, 7f), withStumpPrefab: false);
        wallNode.OnBuildStart();
        yield return null;

        int sectionCount = GetSpawnedSections(wallNode).Count;
        Assert.AreEqual(sectionCount, wallNode.transform.childCount,
            "No stump expected when StumpPrefab is null");
    }
}
