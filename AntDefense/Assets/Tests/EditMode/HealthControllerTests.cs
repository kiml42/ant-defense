using NUnit.Framework;
using UnityEngine;

public class HealthControllerTests
{
    private HealthController _health;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject();
        _health = go.AddComponent<HealthController>();
        _health.MaxHealth = 100f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_health.gameObject);
    }

    [Test]
    public void StartsAtFullHealth()
    {
        Assert.AreEqual(0f, _health.Damage);
    }

    [Test]
    public void InjureReducesHealth()
    {
        _health.Injure(30f);
        Assert.AreEqual(30f, _health.Damage, 0.001f);
    }

    [Test]
    public void HealRestoresDamage()
    {
        _health.Injure(40f);
        _health.Heal(20f);
        Assert.AreEqual(20f, _health.Damage, 0.001f);
    }

    [Test]
    public void HealCannotExceedMaxHealth()
    {
        _health.Heal(50f);
        Assert.AreEqual(0f, _health.Damage);
    }

    [Test]
    public void MultipleInjuriesAccumulate()
    {
        _health.Injure(10f);
        _health.Injure(15f);
        Assert.AreEqual(25f, _health.Damage, 0.001f);
    }

    [Test]
    public void FullHealClearsDamage()
    {
        _health.Injure(50f);
        _health.Heal(50f);
        Assert.AreEqual(0f, _health.Damage, 0.001f);
    }

    [Test]
    public void DeathDoesNotThrowWithNoDeathActions()
    {
        // Injuring to zero triggers Die(); should not throw with no DeathActionBehaviour attached
        Assert.DoesNotThrow(() => _health.Injure(100f));
    }
}
