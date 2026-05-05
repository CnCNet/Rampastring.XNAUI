using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class TTFFontWrapper : IFont
{
    internal readonly SpriteFontBase _font;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <summary>
    /// Returns the vertical centering offset for controls: use (controlHeight - GetAscent()) / 2
    /// as the draw Y to visually centre capital letters in a fixed-height control.
    ///
    /// TextBounds("H") gives Bounds where Y = top of 'H' from draw origin (= ascent - capHeight)
    /// and Y2 = bottom of 'H' from draw origin (= ascent). Drawing at (h - Y - Y2) / 2 places
    /// the visual midpoint of cap letters at exactly h/2, independent of descenders or the gap
    /// between ascent and cap height that TTF metrics include for accented capitals.
    /// </summary>
    public int GetAscent()
    {
        var b = _font.TextBounds("H", Vector2.Zero);
        return (int)Math.Ceiling(b.Y + b.Y2);
    }

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
