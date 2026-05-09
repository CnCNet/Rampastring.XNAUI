using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public class SpriteFontWrapper : IFont
{
    internal readonly SpriteFont _font;
    private readonly int _verticalCenteringValue;

    public SpriteFontWrapper(SpriteFont font)
    {
        _font = font;
        _verticalCenteringValue = _font.LineSpacing;
    }

    public Vector2 MeasureString(string text) => _font.MeasureString(text);

    /// <summary>
    /// Returns the value <c>V</c> to plug into <c>(controlHeight - V) / 2</c> for
    /// vertical centering. For XNA SpriteFont this is <c>LineSpacing</c>: it is the
    /// constant value that <c>MeasureString(text).Y</c> always returns for SpriteFont
    /// regardless of text content, so this preserves the pre-PR centering behavior
    /// exactly (SpriteFont has no descender-induced shift to fix, unlike TTF).
    /// </summary>
    public int GetVerticalCenteringValue() => _verticalCenteringValue;

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth) =>
        spriteBatch.DrawString(_font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);

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
