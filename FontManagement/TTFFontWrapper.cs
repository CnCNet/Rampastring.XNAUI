using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class TTFFontWrapper : IFont
{
    private const string CapHeightReferenceGlyph = "H";
    internal readonly SpriteFontBase _font;
    private readonly int _verticalOffset;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
        var bounds = _font.TextBounds(CapHeightReferenceGlyph, Vector2.Zero);
        _verticalOffset = (int)Math.Ceiling(bounds.Y + bounds.Y2);
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <summary>
    /// Returns a stable vertical alignment offset used for centering text in fixed-height controls.
    /// This cached value is derived from the bounds of the capital 'H' reference glyph and is
    /// computed from <c>bounds.Y + bounds.Y2</c>. It is intended for baseline/visual centering
    /// calculations so descenders do not shift the text position between strings.
    /// This is not the glyph height or line height in pixels, and it may be negative depending
    /// on the font metrics.
    /// </summary>
    public int GetVerticalOffset() => _verticalOffset;

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
