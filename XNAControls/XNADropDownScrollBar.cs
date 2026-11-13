using Microsoft.Xna.Framework;
using System;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A thin scroll bar designed for drop-down controls. Renders via <see cref="XNAControl.FillRectangle"/>
/// instead of textures, and has no up/down buttons.
/// </summary>
/// <remarks>
/// Inherits scroll state and math from <see cref="XNAScrollBarBase"/>.
/// Colors can be configured via <see cref="BarColor"/>, <see cref="ThumbColor"/>, and
/// <see cref="ThumbBorderColor"/> properties. Width is controlled by <see cref="ScrollBarWidth"/>.
/// </remarks>
public class XNADropDownScrollBar : XNAScrollBarBase
{
    /// <summary>
    /// The width of the scroll bar track and thumb in pixels.
    /// </summary>
    public int ScrollBarWidth { get; set; } = 3;

    /// <summary>Background color of the scroll bar track.</summary>
    public Color BarColor { get; set; } = Color.Black;

    /// <summary>Fill color of the scroll thumb.</summary>
    public Color ThumbColor { get; set; } = Color.White;

    /// <summary>Border color drawn around the scroll thumb.</summary>
    public Color ThumbBorderColor { get; set; } = Color.Gray;

    /// <inheritdoc/>
    public override int ScrollWidth => ScrollBarWidth;

    public XNADropDownScrollBar(WindowManager windowManager) : base(windowManager)
    {
        HeaderHeight = 0;
        FooterHeight = 0;
    }

    /// <summary>
    /// Draws the scroll bar track and thumb using filled rectangles.
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        if (scrollablePixels <= 0)
            return;

        FillRectangle(new Rectangle(0, 0, ScrollBarWidth, Height), BarColor);

        Rectangle thumbRect = new Rectangle(0, buttonY, ScrollBarWidth, thumbHeight);
        FillRectangle(thumbRect, ThumbColor);
        DrawRectangle(thumbRect, ThumbBorderColor, 1);

        base.Draw(gameTime);
    }
}
