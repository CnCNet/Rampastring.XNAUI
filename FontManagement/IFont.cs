using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public interface IFont
{
    Vector2 MeasureString(string text);

    int GetTextYPadding(int containerHeight, string text);
    int GetSingleLineTextYPadding(int containerHeight);

    void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth);
    bool HasCharacter(char c);
    string GetSafeString(string str);
}
