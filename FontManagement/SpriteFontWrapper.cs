using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class SpriteFontWrapper : IFont
{
    internal readonly SpriteFont _font;
    private readonly int _visualHeight;

    public SpriteFontWrapper(SpriteFont font)
    {
        _font = font;
        _visualHeight = _font.LineSpacing;
    }

    public Vector2 MeasureString(string text) => _font.MeasureString(text);

    // For XNA SpriteFonts, measured text height matches LineSpacing, so using the
    // cached line spacing preserves the previous centering behavior without measuring per draw.
    public int GetVisualHeight() => _visualHeight;

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth) =>
        spriteBatch.DrawString(_font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) =>
        spriteBatch.DrawString(_font, text.ToString(), location, color, rotation, origin, scale.X, SpriteEffects.None, depth);

    public bool HasCharacter(char c) => _font.Characters.Contains(c);

    public string GetSafeString(string str)
    {
        var sb = new StringBuilder(str);
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (c != '\r' && c != '\n' && !HasCharacter(c))
            {
                sb[i] = '?';
            }
        }

        return sb.ToString();
    }
}
