using System;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// IFontStashRenderer that rounds each glyph's destination position to integer
/// pixels before submitting it to the SpriteBatch. FontStashSharp normally
/// accumulates fractional X advances per glyph, which under PointClamp at
/// scale=1 makes individual letters sample slightly different source columns
/// depending on their sub-pixel offset — visible as the "two B's look different"
/// effect within a single string.
/// </summary>
internal sealed class PixelSnapFontRenderer : IFontStashRenderer
{
    public static readonly PixelSnapFontRenderer Instance = new();

    private SpriteBatch _batch;

    public SpriteBatch Batch
    {
        get => _batch;
        set => _batch = value ?? throw new ArgumentNullException(nameof(value));
    }

    public GraphicsDevice GraphicsDevice => _batch.GraphicsDevice;

    public void Draw(Texture2D texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 scale, float depth)
    {
        pos.X = (float)Math.Round(pos.X);
        pos.Y = (float)Math.Round(pos.Y);

        if (rotation == 0f && scale.X == 1f && scale.Y == 1f && depth == 0f)
        {
            _batch.Draw(texture, pos, src, color);
        }
        else
        {
            _batch.Draw(texture, pos, src, color, rotation, Vector2.Zero, scale, SpriteEffects.None, depth);
        }
    }
}
