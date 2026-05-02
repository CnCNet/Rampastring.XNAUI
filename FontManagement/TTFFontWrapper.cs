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
