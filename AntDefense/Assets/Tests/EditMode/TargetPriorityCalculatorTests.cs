using NUnit.Framework;
using UnityEngine;

public class TargetPriorityCalculatorTests
{
    private TargetPriorityCalculator _calc;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject();
        _calc = go.AddComponent<TargetPriorityCalculator>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_calc.gameObject);
    }

    [Test]
    public void PureDistanceWeighting_CloserTargetHasLowerPriority()
    {
        _calc._actualValueWeighting = 0f; // distance only
        float near = _calc.CalculatePriority(5f, null);
        float far  = _calc.CalculatePriority(10f, null);
        Assert.Less(near, far);
    }

    [Test]
    public void PureValueWeighting_HigherValueHasLowerPriority()
    {
        _calc._actualValueWeighting = 0.95f; // value only
        float highValue = _calc.CalculatePriority(10f, 20f);
        float lowValue  = _calc.CalculatePriority(10f,  5f);
        Assert.Less(highValue, lowValue);
    }

    [Test]
    public void NullValue_TreatedAsZero()
    {
        _calc._actualValueWeighting = 0.5f;
        float withNull = _calc.CalculatePriority(10f, null);
        float withZero = _calc.CalculatePriority(10f, 0f);
        Assert.AreEqual(withNull, withZero, 0.001f);
    }

    [Test]
    public void EqualWeighting_CloserAndHigherValueBalances()
    {
        _calc._actualValueWeighting = 0.5f;
        // Both factors pull equally; closer + higher value should be lower priority number
        float preferred = _calc.CalculatePriority(2f, 10f);
        float other     = _calc.CalculatePriority(8f,  1f);
        Assert.Less(preferred, other);
    }

    [Test]
    public void ZeroDistanceZeroValue_ReturnsZero()
    {
        _calc._actualValueWeighting = 0.5f;
        float result = _calc.CalculatePriority(0f, 0f);
        Assert.AreEqual(0f, result, 0.001f);
    }
}
