using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Abstract base that runs the full wall-section spawn test suite.
/// Subclasses supply a SectionLength and a CreateWallSetup factory so that
/// exactly the same assertions run against both hand-built and prefab-based setups.
/// </summary>
public abstract class WallSectionSpawnTestsBase
{
    // The section length used throughout the suite — set by each subclass.
    protected float SectionLength;

    protected List<GameObject> ToDestroy;

    [SetUp]
    public void SetUp()
    {
        ToDestroy = new List<GameObject>();
        SetUpFixture();
    }

    /// <summary>Subclasses override to load prefabs / set SectionLength before each test.</summary>
    protected virtual void SetUpFixture() { }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in ToDestroy)
            if (go != null) Object.Destroy(go);
    }

    /// <summary>
    /// Creates a WallNode at <paramref name="start"/> connected to a node at <paramref name="end"/>,
    /// ready for OnBuildStart() to be called.
    /// </summary>
    protected abstract WallNode CreateWallSetup(Vector3 start, Vector3 end, bool withStumpPrefab = false);

    protected List<WallSection> GetSpawnedSections(WallNode wallNode)
    {
        var sections = new List<WallSection>();
        foreach (Transform child in wallNode.transform)
        {
            var s = child.GetComponent<WallSection>();
            if (s != null) sections.Add(s);
        }
        return sections;
    }

    // ── Section count ────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpawnSections_CorrectCount_ExactMultiple()
    {
        float distance = SectionLength * 3;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        Assert.AreEqual(3, GetSpawnedSections(wallNode).Count);
    }

    [UnityTest]
    public IEnumerator SpawnSections_CorrectCount_WithRemainder()
    {
        // Add half a section-length so floor division still gives 3
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        Assert.AreEqual(3, GetSpawnedSections(wallNode).Count);
    }

    [UnityTest]
    public IEnumerator SpawnSections_NoSections_WhenDistanceSmallerThanSectionLength()
    {
        float distance = SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
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

    // ── Positions ────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpawnSections_SectionsPositionedAtCorrectIntervals()
    {
        // Exact multiple so halfGap = 0; centres are at 0.5, 1.5, 2.5 × SectionLength
        float distance = SectionLength * 3;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        var sections = GetSpawnedSections(wallNode);
        sections.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        for (int i = 0; i < sections.Count; i++)
        {
            float expectedZ = (i + 0.5f) * SectionLength;
            Assert.AreEqual(expectedZ, sections[i].transform.position.z, 0.01f,
                $"Section {i} z-position incorrect");
        }
    }

    [UnityTest]
    public IEnumerator SpawnSections_GapAtEachEndIsEqual_WhenRemainderExists()
    {
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        var sections = GetSpawnedSections(wallNode);
        sections.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        float gapAtStart = sections[0].transform.position.z - SectionLength / 2f;
        float gapAtEnd   = distance - (sections[^1].transform.position.z + SectionLength / 2f);

        Assert.AreEqual(gapAtStart, gapAtEnd, 0.01f, "Gap at each end of the wall should be equal");
    }

    [UnityTest]
    public IEnumerator SpawnSections_SectionsDoNotExtendBeyondNodes()
    {
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        foreach (var section in GetSpawnedSections(wallNode))
        {
            float sectionStart = section.transform.position.z - SectionLength / 2f;
            float sectionEnd   = section.transform.position.z + SectionLength / 2f;

            Assert.GreaterOrEqual(sectionStart, -0.01f, "Section starts before nodeA");
            Assert.LessOrEqual(sectionEnd, distance + 0.01f, "Section ends after nodeB");
        }
    }

    // ── Orientation ──────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpawnSections_SectionsFaceFromNodeToNode()
    {
        float distance = SectionLength * 3;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance));
        wallNode.OnBuildStart();
        yield return null;

        foreach (var section in GetSpawnedSections(wallNode))
        {
            Assert.AreEqual(0f, section.transform.forward.x, 0.01f);
            Assert.AreEqual(1f, section.transform.forward.z, 0.01f);
        }
    }

    [UnityTest]
    public IEnumerator SpawnSections_SectionsFaceCorrectly_DiagonalWall()
    {
        var end = new Vector3(SectionLength * 3, 0, SectionLength * 3);
        var wallNode = CreateWallSetup(Vector3.zero, end);
        wallNode.OnBuildStart();
        yield return null;

        var expectedForward = end.normalized;
        foreach (var section in GetSpawnedSections(wallNode))
        {
            Assert.AreEqual(expectedForward.x, section.transform.forward.x, 0.01f);
            Assert.AreEqual(expectedForward.z, section.transform.forward.z, 0.01f);
        }
    }

    // ── Stump ────────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpawnSections_NoStump_WhenExactMultiple()
    {
        float distance = SectionLength * 3;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance), withStumpPrefab: true);
        wallNode.OnBuildStart();
        yield return null;

        int sectionCount = GetSpawnedSections(wallNode).Count;
        Assert.AreEqual(sectionCount, wallNode.transform.childCount,
            "No stump expected when distance is an exact multiple of SectionLength");
    }

    [UnityTest]
    public IEnumerator SpawnSections_SpawnsStump_WhenRemainderExistsAndPrefabSet()
    {
        Vector3 start = Vector3.zero;
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        Vector3 end = new Vector3(0, 0, distance);
        var wallNode = CreateWallSetup(start, end, withStumpPrefab: true);
        wallNode.OnBuildStart();
        yield return null;

        var sections = GetSpawnedSections(wallNode);
        var stumps = new List<Transform>();
        foreach (Transform child in wallNode.transform)
        {
            if (child.GetComponent<WallSection>() == null)
                stumps.Add(child);
        }

        Assert.AreEqual(3, sections.Count, "Expected 3 full wall sections");
        Assert.AreEqual(2, stumps.Count, "Expected one stump at each end of the wall");

        float expectedGap = (distance - 3 * SectionLength) / 2f;

        stumps.Sort((a, b) => a.position.z.CompareTo(b.position.z));
        Transform stumpA = stumps[0];
        Transform stumpB = stumps[1];

        Assert.AreEqual(start, stumpA.position, "StumpA must be placed at the start node position");
        Assert.AreEqual(end,   stumpB.position, "StumpB must be placed at the end node position");
        Assert.AreEqual(expectedGap, stumpA.localScale.z, 0.001f, "StumpA Z scale must equal the gap size");
        Assert.AreEqual(expectedGap, stumpB.localScale.z, 0.001f, "StumpB Z scale must equal the gap size");
    }

    [UnityTest]
    public IEnumerator SpawnSections_NoStump_WhenPrefabNotSet()
    {
        float distance = SectionLength * 3 + SectionLength * 0.5f;
        var wallNode = CreateWallSetup(Vector3.zero, new Vector3(0, 0, distance), withStumpPrefab: false);
        wallNode.OnBuildStart();
        yield return null;

        int sectionCount = GetSpawnedSections(wallNode).Count;
        Assert.AreEqual(sectionCount, wallNode.transform.childCount,
            "No stump expected when StumpPrefab is null");
    }
}
