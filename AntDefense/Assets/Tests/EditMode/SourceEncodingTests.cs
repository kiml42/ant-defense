using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards against encoding regressions in source files that contain non-ASCII literals.
/// These tests read the raw .cs file and check for the correct UTF-8 byte sequence so
/// that an editor/tool re-saving in the wrong encoding is caught immediately.
/// </summary>
public class SourceEncodingTests
{
    // UTF-8 encoding of '£' is 0xC2 0xA3.
    private static readonly byte[] PoundUtf8 = { 0xC2, 0xA3 };
    // The UTF-8 replacement character (U+FFFD) that appears when a byte sequence is mis-decoded.
    private static readonly byte[] ReplacementCharUtf8 = { 0xEF, 0xBF, 0xBD };

    [Test]
    public void UiPlane_CostText_ContainsPoundSign()
    {
        var bytes = ReadScriptBytes("UI/UiPlane.cs");
        Assert.IsTrue(ContainsSequence(bytes, PoundUtf8),
            "UiPlane.cs does not contain a valid UTF-8 '£' (0xC2 0xA3). The pound symbol may have been corrupted.");
    }

    [Test]
    public void UiPlane_CostText_DoesNotContainReplacementChar()
    {
        var bytes = ReadScriptBytes("UI/UiPlane.cs");
        Assert.IsFalse(ContainsSequence(bytes, ReplacementCharUtf8),
            "UiPlane.cs contains a UTF-8 replacement character (U+FFFD). A non-ASCII literal has been corrupted, likely by an encoding mismatch when saving.");
    }

    [Test]
    public void TranslateHandle_CostText_ContainsPoundSign()
    {
        var bytes = ReadScriptBytes("Placeables/TranslateHandle.cs");
        Assert.IsTrue(ContainsSequence(bytes, PoundUtf8),
            "TranslateHandle.cs does not contain a valid UTF-8 '£' (0xC2 0xA3). The pound symbol may have been corrupted.");
    }

    [Test]
    public void TranslateHandle_CostText_DoesNotContainReplacementChar()
    {
        var bytes = ReadScriptBytes("Placeables/TranslateHandle.cs");
        Assert.IsFalse(ContainsSequence(bytes, ReplacementCharUtf8),
            "TranslateHandle.cs contains a UTF-8 replacement character (U+FFFD). A non-ASCII literal has been corrupted.");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static byte[] ReadScriptBytes(string relativePath)
    {
        // Walk up from the test assembly location to find Assets/Scripts/
        var scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
        var fullPath = Path.Combine(scriptsRoot, relativePath);
        Assert.IsTrue(File.Exists(fullPath), $"Could not find script file at: {fullPath}");
        return File.ReadAllBytes(fullPath);
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }
}
