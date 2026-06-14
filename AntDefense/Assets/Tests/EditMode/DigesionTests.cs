using NUnit.Framework;
using UnityEngine;

public class DigesionTests
{
    private Digestion _digestion;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject();
        _digestion = go.AddComponent<Digestion>();
        _digestion.MaxFood = 100f;
        // Set CurrentFood via AddFood since Start() hasn't run in edit-mode tests
        _digestion.AddFood(50f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_digestion.gameObject);
    }

    [Test]
    public void AddFoodIncreasesCurrentFood()
    {
        float before = _digestion.CurrentFood;
        _digestion.AddFood(10f);
        Assert.AreEqual(before + 10f, _digestion.CurrentFood, 0.001f);
    }

    [Test]
    public void UseFoodDecreasesCurrentFood()
    {
        float before = _digestion.CurrentFood;
        _digestion.UseFood(15f);
        Assert.AreEqual(before - 15f, _digestion.CurrentFood, 0.001f);
    }

    [Test]
    public void AddAndUseFoodAreSymmetric()
    {
        float before = _digestion.CurrentFood;
        _digestion.AddFood(20f);
        _digestion.UseFood(20f);
        Assert.AreEqual(before, _digestion.CurrentFood, 0.001f);
    }

    [Test]
    public void UseFoodCanGoBelowZero()
    {
        // FixedUpdate handles the starvation case; UseFood itself is unclamped
        _digestion.UseFood(200f);
        Assert.Less(_digestion.CurrentFood, 0f);
    }
}
