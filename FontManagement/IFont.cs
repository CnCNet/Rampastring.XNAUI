using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public interface IFont
{
    Vector2 MeasureString(string text);

    /// <summary>
    /// Returns the value <c>V</c> to plug into <c>(controlHeight - V) / 2</c> for
    /// vertical centering. This is NOT a geometric font height — it is the value
    /// each implementation chooses so that the centering formula produces the
    /// correct visual result for that font type.
    /// </summary>
    int GetVerticalCenteringValue();
    void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth);
    void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth);
    bool HasCharacter(char c);
    string GetSafeString(string str);
}
