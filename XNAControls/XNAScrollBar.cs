using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A vertical scroll bar that can be utilized for various other controls.
/// Renders using textures for the thumb (top, middle, bottom parts) and background,
/// and provides up/down arrow buttons.
/// </summary>
/// <remarks>
/// See also the sibling <see cref="XNAHorizontalScrollBar"/> class.
/// Inherits scroll state and math from <see cref="XNAScrollBarBase"/>.
/// </remarks>
public class XNAScrollBar : XNAScrollBarBase
{
    /// <summary>
    /// Creates a new scroll bar.
    /// </summary>
    /// <param name="windowManager">The game window manager.</param>
    public XNAScrollBar(WindowManager windowManager) : base(windowManager)
    {
        ExclusiveInputCapture = true;
        HandlesDragging = true;

        var scrollUpTexture = AssetLoader.LoadTexture("sbUpArrow.png");

        btnScrollUp = new XNAButton(WindowManager);
        btnScrollUp.Name = nameof(btnScrollUp);
        btnScrollUp.ClientRectangle = new Rectangle(0, 0, scrollUpTexture.Width, scrollUpTexture.Height);
        btnScrollUp.IdleTexture = scrollUpTexture;
        if (AssetLoader.AssetExists("sbUpArrowHovered.png"))
            btnScrollUp.HoverTexture = AssetLoader.LoadTexture("sbUpArrowHovered.png");

        var scrollDownTexture = AssetLoader.LoadTexture("sbDownArrow.png");

        btnScrollDown = new XNAButton(WindowManager);
        btnScrollDown.Name = nameof(btnScrollDown);
        btnScrollDown.ClientRectangle = new Rectangle(0, Height - scrollDownTexture.Height,
            scrollDownTexture.Width, scrollDownTexture.Height);
        btnScrollDown.IdleTexture = scrollDownTexture;
        if (AssetLoader.AssetExists("sbDownArrowHovered.png"))
            btnScrollDown.HoverTexture = AssetLoader.LoadTexture("sbDownArrowHovered.png");

        ClientRectangleUpdated += XNAScrollBar_ClientRectangleUpdated;
    }

    /// <summary>
    /// Returns the width of the scroll bar's up-arrow texture.
    /// Overridden to avoid circular dependency with <see cref="XNAControl.Width"/>.
    /// </summary>
    public override int ScrollWidth => btnScrollUp.IdleTexture.Width;

    private void XNAScrollBar_ClientRectangleUpdated(object sender, EventArgs e)
    {
        btnScrollDown.ClientRectangle = new Rectangle(0,
            Height - btnScrollDown.Height,
            btnScrollDown.Width, btnScrollDown.Height);
        Refresh();
    }

    public override void Initialize()
    {
        base.Initialize();

        AddChild(btnScrollUp);
        AddChild(btnScrollDown);

        btnScrollUp.LeftClick += (s, e) => ScrollUp();
        btnScrollDown.LeftClick += (s, e) => ScrollDown();

        background = AssetLoader.LoadTexture("sbBackground.png");
        thumbMiddle = AssetLoader.LoadTexture("sbMiddle.png");
        thumbTop = AssetLoader.LoadTexture("sbThumbTop.png");
        thumbBottom = AssetLoader.LoadTexture("sbThumbBottom.png");

        HeaderHeight = btnScrollUp.Height;
        FooterHeight = btnScrollDown.Height;
    }

    public override void Kill()
    {
        // These textures are cached, don't allow the buttons to dispose them
        btnScrollDown.IdleTexture = null;
        btnScrollDown.HoverTexture = null;
        btnScrollUp.IdleTexture = null;
        btnScrollUp.HoverTexture = null;

        base.Kill();
    }

    /// <inheritdoc/>
    protected override void OnRefreshNoScroll()
    {
        btnScrollUp.Disable();
        btnScrollDown.Disable();
    }

    /// <inheritdoc/>
    protected override void OnRefreshWithScroll()
    {
        btnScrollUp.Enable();
        btnScrollDown.Enable();
    }

    /// <inheritdoc/>
    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        if (IsDrawn())
        {
            inputEventArgs.Handled = true;
            base.OnLeftClick(inputEventArgs);

            HandleTrackClick(GetCursorPoint());
        }
    }

    /// <inheritdoc/>
    public override void OnMouseMove()
    {
        base.OnMouseMove();

        if (Cursor.LeftDown)
        {
            HandleTrackClick(GetCursorPoint());
            isHeldDown = true;
            WindowManager.SelectedControl = this;
        }
    }

    /// <summary>
    /// Updates the scroll bar's logic each frame.
    /// Makes it possible to drag the scrollbar thumb even if the cursor
    /// leaves the scroll bar's surface.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (isHeldDown)
        {
            if (!Cursor.LeftDown)
            {
                isHeldDown = false;
                WindowManager.SelectedControl = null;
            }
            else
            {
                HandleTrackClick(GetCursorPoint());
            }
        }
    }

    /// <summary>
    /// Draws the scroll bar: background track, then the three-part thumb
    /// (top cap, stretched middle, bottom cap).
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        if (scrollablePixels > 0)
        {
            DrawTexture(background, new Rectangle(0, 0, Width, Height), Color.White);

            DrawTexture(thumbTop, new Rectangle(0, buttonY, ScrollWidth, thumbTop.Height), RemapColor);
            DrawTexture(thumbBottom, new Rectangle(0,
                buttonY + thumbHeight - thumbBottom.Height, ScrollWidth, thumbBottom.Height), Color.White);
            DrawTexture(thumbMiddle, new Rectangle(0,
                buttonY + thumbTop.Height, ScrollWidth, thumbHeight - thumbTop.Height - thumbBottom.Height), Color.White);
        }

        base.Draw(gameTime);
    }

    private XNAButton btnScrollUp;
    private XNAButton btnScrollDown;
    private Texture2D background;
    private Texture2D thumbMiddle;
    private Texture2D thumbTop;
    private Texture2D thumbBottom;
    private bool isHeldDown = false;
}
