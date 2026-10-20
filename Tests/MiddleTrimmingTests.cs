#nullable enable
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.FontManagement;

namespace Rampastring.XNAUI.Tests;

[TestClass]
[DoNotParallelize]
public class MiddleTrimmingTests
{
    [TestInitialize]
    public void InitializeFonts()
    {
        FontManager.Initialize();
        FontManager.GetFontList().Add(new MeasuringFont(text => text.Length));
    }

    private static string Trim(string text, int width) => Renderer.GetStringWithLimitedWidthTrimMiddle(text, 0, width);

    [TestMethod]
    [DataRow("", 0)]
    [DataRow("file.map", 8)]
    [DataRow("file.map", 30)]
    public void FittingTextIsUnchanged(string text, int width) => Assert.AreEqual(text, Trim(text, width));

    [TestMethod]
    public void TruncationRetainsBeginningAndEnd() => Assert.AreEqual("abcd...xyz", Trim("abcdefghijkuvwxyz", 10));

    [TestMethod]
    [DataRow(-1, "")]
    [DataRow(0, "")]
    [DataRow(1, "")]
    [DataRow(2, "")]
    [DataRow(3, "...")]
    public void NarrowWidthsDoNotOverflow(int width, string expected) => Assert.AreEqual(expected, Trim("long-filename.map", width));

    [TestMethod]
    public void ExistingEndTruncationIsUnchanged() => Assert.AreEqual("abcd", Renderer.GetStringWithLimitedWidth("abcdefghij", 0, 4));

    [TestMethod]
    public void NullInputIsRejected() => Assert.ThrowsExactly<ArgumentNullException>(() => Trim(null!, 10));

    [TestMethod]
    public void InvalidFontIsRejected()
    {
        FontManager.GetFontList().Clear();
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => Trim("abc", 10));
    }

    [TestMethod]
    public void SurrogatePairsRemainIntactAtEveryWidth()
    {
        string text = "start" + string.Concat(Enumerable.Repeat("\U0001F642", 30)) + "_original.map";
        for (int width = 0; width <= text.Length; width++)
        {
            string result = Trim(text, width);
            Assert.IsTrue(result.Length <= width);
            for (int i = 0; i < result.Length; i++)
            {
                if (char.IsHighSurrogate(result[i]))
                    Assert.IsTrue(++i < result.Length && char.IsLowSurrogate(result[i]));
                else
                    Assert.IsFalse(char.IsLowSurrogate(result[i]));
            }
        }
    }

    [TestMethod]
    public void CombinedTextIsMeasuredAfterShaping()
    {
        FontManager.GetFontList()[0] = new MeasuringFont(text => text.Contains("...") && text.Length > 5 ? text.Length + 50 : text.Length);
        string result = Trim("abcdefghijkuvwxyz", 10);
        Assert.IsTrue(Renderer.MeasureString(result, 0).X <= 10);
        Assert.IsTrue(result.Contains("..."));
    }

    [TestMethod]
    public void ExistingDotsCanAppearBesideTheEllipsis() => Assert.AreEqual("abc....xyz", Trim("abc.defghijklmnop.xyz", 10));

    [TestMethod]
    public void LongPathRetainsMoreThanJustTheExtension()
    {
        string result = Trim("Current file: Maps/Custom/" + new string('W', 150) + "_original.map", 50);
        Assert.IsTrue(result.StartsWith("Current file: Maps/"));
        Assert.IsTrue(result.EndsWith("_original.map"));
        Assert.IsTrue(result.Length <= 50);
    }

    [TestMethod]
    public void VariableWidthFontsKeepEveryResultWithinBounds()
    {
        FontManager.GetFontList()[0] = new MeasuringFont(text => text.Sum(c => c == 'W' ? 9f : c == '.' ? 0.75f : 2.25f));
        foreach (string text in new[] { "Wider-prefix/" + new string('i', 100) + ".map", "tiny-prefix/" + new string('W', 100) + ".map", "file.with.many.dots", "no-extension" })
        {
            for (int width = 0; width < 250; width++)
            {
                string result = Trim(text, width);
                Assert.IsTrue(Renderer.MeasureString(result, 0).X <= width);
                if (Renderer.MeasureString(text, 0).X <= width)
                    Assert.AreEqual(text, result);
            }
        }
    }

    private sealed class MeasuringFont(Func<string, float> measure) : IFont
    {
        public Vector2 MeasureString(string text) => new Vector2(measure(text), 1);
        public int GetTextYPadding(int height, string text) => 0;
        public int GetSingleLineTextYPadding(int height) => 0;
        public void DrawString(SpriteBatch batch, string text, Vector2 location, Color color, float scale, float depth) => throw new NotSupportedException();
        public bool HasCharacter(char c) => true;
        public string GetSafeString(string text) => text;
    }
}
