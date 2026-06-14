using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks that the wall prefabs are correctly configured.
/// These run in edit mode so no scene is needed — they inspect prefab assets directly.
/// </summary>
public class WallPrefabIntegrationTests
{
    private const string WallNodePrefabPath    = "Assets/Prefabs/Placeables/WallNode.prefab";
    private const string WallSectionPrefabPath = "Assets/Prefabs/Placeables/WallSection.prefab";

    private WallNode    _wallNode;
    private WallSection _wallSection;

    [SetUp]
    public void SetUp()
    {
        var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallNodePrefabPath);
        Assert.IsNotNull(nodePrefab, $"Could not load prefab at {WallNodePrefabPath}");
        _wallNode = nodePrefab.GetComponentInChildren<WallNode>(includeInactive: true);
        Assert.IsNotNull(_wallNode, "WallNode prefab has no WallNode component");

        var sectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WallSectionPrefabPath);
        Assert.IsNotNull(sectionPrefab, $"Could not load prefab at {WallSectionPrefabPath}");
        _wallSection = sectionPrefab.GetComponentInChildren<WallSection>(includeInactive: true);
        Assert.IsNotNull(_wallSection, "WallSection prefab has no WallSection component");
    }

    // ── WallNode prefab ──────────────────────────────────────────────────────

    [Test]
    public void WallNode_HasSectionPrefabAssigned()
    {
        Assert.IsNotNull(_wallNode.SectionPrefab, "SectionPrefab must be assigned on the WallNode prefab");
    }

    [Test]
    public void WallNode_HasWallGhostAssigned()
    {
        Assert.IsNotNull(_wallNode.WallGhost, "WallGhost must be assigned on the WallNode prefab");
    }

    [Test]
    public void WallNode_MaxLengthIsPositive()
    {
        Assert.Greater(_wallNode.MaxLength, 0f);
    }

    [Test]
    public void WallNode_CostPerMeterIsPositive()
    {
        Assert.Greater(_wallNode.CostPerMeter, 0f);
    }

    [Test]
    public void WallNode_SectionLengthIsLessThanMaxLength()
    {
        // A wall must be able to fit at least one section
        Assert.Less(_wallNode.SectionLength, _wallNode.MaxLength,
            "SectionLength must be less than MaxLength so at least one section can be spawned");
    }

    // ── WallSection prefab ───────────────────────────────────────────────────

    [Test]
    public void WallSection_SectionLengthIsPositive()
    {
        Assert.Greater(_wallSection.SectionLength, 0f);
    }

    [Test]
    public void WallSection_HasHealthController()
    {
        var health = _wallSection.GetComponentInChildren<HealthController>(includeInactive: true);
        Assert.IsNotNull(health, "WallSection prefab must have a HealthController");
    }

    [Test]
    public void WallSection_HealthController_HasPositiveMaxHealth()
    {
        var health = _wallSection.GetComponentInChildren<HealthController>(includeInactive: true);
        Assert.Greater(health.MaxHealth, 0f);
    }

    // ── Cross-prefab consistency ─────────────────────────────────────────────

    [Test]
    public void WallNode_SectionPrefabMatchesWallSectionPrefab()
    {
        // The SectionPrefab referenced by the WallNode should be the canonical WallSection prefab
        Assert.AreEqual(_wallSection.SectionLength, _wallNode.SectionPrefab.SectionLength, 0.001f,
            "WallNode.SectionPrefab.SectionLength must match the WallSection prefab's SectionLength");
    }
}
