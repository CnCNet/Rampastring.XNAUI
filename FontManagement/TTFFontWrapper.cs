using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class TTFFontWrapper : IFont
{
    private const string CapHeightReferenceGlyph = "H";
    internal readonly SpriteFontBase _font;
    private readonly int _verticalCenteringValue;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
        var bounds = _font.TextBounds(CapHeightReferenceGlyph, Vector2.Zero);
        _verticalCenteringValue = (int)Math.Ceiling(bounds.Y + bounds.Y2);
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <summary>
    /// Returns the value <c>V</c> to plug into <c>(controlHeight - V) / 2</c> for
    /// vertical centering. NOT a geometric height: this is <c>top + bottom</c> of the
    /// cap glyph 'H' from the draw origin (i.e. <c>minY + maxY</c> from FontStashSharp's
    /// <c>TextBounds</c>), chosen so the cap-glyph midpoint lands at <c>controlHeight / 2</c>
    /// independent of descenders. The geometric glyph height would be <c>maxY - minY</c>.
    /// </summary>
    public int GetVerticalCenteringValue() => _verticalCenteringValue;

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        var vectorScale = new Vector2(scale, scale);

        // Some fonts render `\r` as a visible character, e.g., Unifont. Therefore, we normalize newlines.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var segment = new StringSegment(text);

        spriteBatch.DrawString(_font, segment, location, color, 0f, Vector2.Zero, vectorScale, depth);
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
