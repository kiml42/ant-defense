using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DigestionPlayModeTests
{
    private GameObject _go;
    private Digestion _digestion;
    private HealthController _health;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject();
        _health = _go.AddComponent<HealthController>();
        _health.MaxHealth = 100f;

        _digestion = _go.AddComponent<Digestion>();
        _digestion.HealthController = _health;
        _digestion.MaxFood = 100f;
        _digestion.StartFood = 50f;
        _digestion.Expenditure = 10f; // fast drain so tests don't take long
        _digestion.HealRate = 0f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_go);
    }

    [UnityTest]
    public IEnumerator FoodDrainsOverTime()
    {
        // Wait a couple of physics frames for FixedUpdate to run
        yield return new WaitForSeconds(0.2f);

        Assert.Less(_digestion.CurrentFood, 50f);
    }

    [UnityTest]
    public IEnumerator StarvationCausesDamage()
    {
        _digestion.StartFood = 0f;
        // StartFood is read in Start() which has already run — set CurrentFood directly
        _digestion.UseFood(_digestion.CurrentFood); // drain to zero

        yield return new WaitForSeconds(0.2f);

        Assert.Greater(_health.Damage, 0f);
    }

    [UnityTest]
    public IEnumerator HealingConsumesFood()
    {
        _digestion.HealRate = 50f;
        _health.Injure(20f); // create damage to heal

        float foodBefore = _digestion.CurrentFood;

        yield return new WaitForSeconds(0.1f);

        Assert.Less(_digestion.CurrentFood, foodBefore);
        Assert.Less(_health.Damage, 20f);
    }
}
