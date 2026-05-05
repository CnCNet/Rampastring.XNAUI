using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class TTFFontWrapper : IFont
{
    private const string CapHeightReferenceGlyph = "H";
    internal readonly SpriteFontBase _font;
    private readonly int _ascent;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
        var bounds = _font.TextBounds(CapHeightReferenceGlyph, Vector2.Zero);
        // FontStashSharp bounds are (minX, minY, maxX, maxY). We use Y + Y2 here because
        // controls position text with (controlHeight - ascent) / 2, and this term preserves the
        // glyph's top offset from the draw origin. If we mistakenly used Y2 - Y, it would only be box height and would mis-center text when Y != 0.
        _ascent = (int)Math.Ceiling(bounds.Y + bounds.Y2);
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <summary>
    /// Returns a stable height used for vertically centering text in fixed-height controls.
    /// The cached value is derived from the bounds of the capital 'H', used here as a
    /// reference cap-height glyph so descenders do not shift the baseline between strings.
    /// </summary>
    public int GetAscent() => _ascent;

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        var vectorScale = new Vector2(scale, scale);

        // Some fonts render `\r` as a visible character, e.g., Unifont. Therefore, we normalize newlines.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var segment = new StringSegment(text);

        spriteBatch.DrawString(_font, segment, location, color, 0f, Vector2.Zero, vectorScale, depth);
    }

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth)
    {
        spriteBatch.DrawString(_font, text, location, color, rotation, origin, scale, depth);
    }

    /// <summary>
    /// For TTF fonts, this always returns true because FontStashSharp can dynamically
    /// generate glyphs for any character. If a glyph is not available in the font file,
    /// a replacement glyph (like � or ?) will be rendered instead.
    /// </summary>
    public bool HasCharacter(char c) => true;

    /// <summary>
    /// Returns the string as-is for TTF fonts.
    /// TTF fonts handle all characters through dynamic glyph generation and fallback.
    /// </summary>
    public string GetSafeString(string str) => str;
}
