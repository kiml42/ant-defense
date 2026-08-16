using NUnit.Framework;

public class ProceduralLevelConfigEncoderTests
{
    [Test]
    public void Encode_ProducesVersionPrefixedString()
    {
        var config = new ProceduralLevelConfig(2, 6, 40, 4, 3, 42);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        Assert.IsTrue(encoded.StartsWith("v3:"), $"Expected 'v3:' prefix, got: {encoded}");
    }

    [Test]
    public void Encode_ContainsAllFields()
    {
        var config = new ProceduralLevelConfig(3, 7, 50, 5, 2, 99);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        StringAssert.Contains("nests=3", encoded);
        StringAssert.Contains("clusters=7", encoded);
        StringAssert.Contains("radius=50", encoded);
        StringAssert.Contains("density=5", encoded);
        StringAssert.Contains("walls=2", encoded);
        StringAssert.Contains("seed=99", encoded);
    }

    [Test]
    public void RoundTrip_PreservesAllValues()
    {
        var original = new ProceduralLevelConfig(4, 8, 35, 3, 7, 12345);
        var encoded = ProceduralLevelConfigEncoder.Encode(original);
        var ok = ProceduralLevelConfigEncoder.TryDecode(encoded, out var decoded);

        Assert.IsTrue(ok, "TryDecode returned false");
        Assert.IsNotNull(decoded);
        Assert.AreEqual(original.NestCount,     decoded.NestCount);
        Assert.AreEqual(original.ClusterCount,  decoded.ClusterCount);
        Assert.AreEqual(original.ClusterRadius, decoded.ClusterRadius);
        Assert.AreEqual(original.ClusterDensity, decoded.ClusterDensity);
        Assert.AreEqual(original.WallDensity,   decoded.WallDensity);
        Assert.AreEqual(original.Seed,          decoded.Seed);
    }

    [Test]
    public void TryDecode_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode("", out _));
    }

    [Test]
    public void TryDecode_NullString_ReturnsFalse()
    {
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode(null, out _));
    }

    [Test]
    public void TryDecode_WrongVersion_ReturnsFalse()
    {
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode("v2:nests=1,clusters=1,radius=40,density=1,walls=1,seed=1", out _));
    }

    [Test]
    public void TryDecode_MissingField_ReturnsFalse()
    {
        // 'seed' is missing
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode("v3:nests=1,clusters=1,radius=40,density=1,walls=1", out _));
    }

    [Test]
    public void TryDecode_NegativeSeed_RoundTrips()
    {
        var config = new ProceduralLevelConfig(1, 1, 40, 1, 1, -7);
        var ok = ProceduralLevelConfigEncoder.TryDecode(ProceduralLevelConfigEncoder.Encode(config), out var decoded);
        Assert.IsTrue(ok);
        Assert.AreEqual(-7, decoded.Seed);
    }

    [Test]
    public void WithIncrementedSeed_IncreasesSeedByOne()
    {
        var config = new ProceduralLevelConfig(2, 6, 40, 4, 3, 10);
        var next = config.WithIncrementedSeed();
        Assert.AreEqual(11, next.Seed);
        Assert.AreEqual(config.NestCount, next.NestCount);
    }

    [Test]
    public void EncodedString_IsSingleLine()
    {
        var config = new ProceduralLevelConfig(5, 10, 40, 10, 10, 999999);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        Assert.IsFalse(encoded.Contains('\n'), "Encoded string must not contain newlines");
        Assert.IsFalse(encoded.Contains('\r'), "Encoded string must not contain carriage returns");
    }
}
