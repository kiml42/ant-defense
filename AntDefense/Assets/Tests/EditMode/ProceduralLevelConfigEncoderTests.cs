using NUnit.Framework;

public class ProceduralLevelConfigEncoderTests
{
    [Test]
    public void Encode_ProducesVersionPrefixedString()
    {
        var config = new ProceduralLevelConfig(2, 6, 4, 3, 42);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        Assert.IsTrue(encoded.StartsWith("v1:"), $"Expected 'v1:' prefix, got: {encoded}");
    }

    [Test]
    public void Encode_ContainsAllFields()
    {
        var config = new ProceduralLevelConfig(3, 7, 5, 2, 99);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        StringAssert.Contains("nests=3", encoded);
        StringAssert.Contains("clusters=7", encoded);
        StringAssert.Contains("size=5", encoded);
        StringAssert.Contains("walls=2", encoded);
        StringAssert.Contains("seed=99", encoded);
    }

    [Test]
    public void RoundTrip_PreservesAllValues()
    {
        var original = new ProceduralLevelConfig(4, 8, 3, 7, 12345);
        var encoded = ProceduralLevelConfigEncoder.Encode(original);
        var ok = ProceduralLevelConfigEncoder.TryDecode(encoded, out var decoded);

        Assert.IsTrue(ok, "TryDecode returned false");
        Assert.IsNotNull(decoded);
        Assert.AreEqual(original.NestCount, decoded.NestCount);
        Assert.AreEqual(original.ClusterCount, decoded.ClusterCount);
        Assert.AreEqual(original.ClusterSize, decoded.ClusterSize);
        Assert.AreEqual(original.WallDensity, decoded.WallDensity);
        Assert.AreEqual(original.Seed, decoded.Seed);
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
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode("v2:nests=1,clusters=1,size=1,walls=1,seed=1", out _));
    }

    [Test]
    public void TryDecode_MissingField_ReturnsFalse()
    {
        // 'seed' is missing
        Assert.IsFalse(ProceduralLevelConfigEncoder.TryDecode("v1:nests=1,clusters=1,size=1,walls=1", out _));
    }

    [Test]
    public void TryDecode_NegativeSeed_RoundTrips()
    {
        var config = new ProceduralLevelConfig(1, 1, 1, 1, -7);
        var ok = ProceduralLevelConfigEncoder.TryDecode(ProceduralLevelConfigEncoder.Encode(config), out var decoded);
        Assert.IsTrue(ok);
        Assert.AreEqual(-7, decoded.Seed);
    }

    [Test]
    public void WithIncrementedSeed_IncreasesSeedByOne()
    {
        var config = new ProceduralLevelConfig(2, 6, 4, 3, 10);
        var next = config.WithIncrementedSeed();
        Assert.AreEqual(11, next.Seed);
        Assert.AreEqual(config.NestCount, next.NestCount);
    }

    [Test]
    public void EncodedString_IsSingleLine()
    {
        var config = new ProceduralLevelConfig(5, 10, 10, 10, 999999);
        var encoded = ProceduralLevelConfigEncoder.Encode(config);
        Assert.IsFalse(encoded.Contains('\n'), "Encoded string must not contain newlines");
        Assert.IsFalse(encoded.Contains('\r'), "Encoded string must not contain carriage returns");
    }
}
