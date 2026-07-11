using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI.Extensions;
using Rampastring.XNAUI.FontManagement;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls;

public enum DropDownState
{
    CLOSED,
    OPENED_DOWN,
    OPENED_UP
}

/// <summary>
/// A drop-down control with optional scrollable item list, hover animation,
/// scroll bar child control and close-on-click-outside behaviour.
/// </summary>
public class XNADropDown : XNAControl
{
    protected const int DefaultScrollBarWidth = 3;
    protected const int ScrollWheelOverflowThreshold = 1000;

    /// <summary>Current scroll offset in item count for scrollable lists.</summary>
    protected int _scrollOffset;

    /// <summary>Maximum number of visible items when scrollable. 0 = show all.</summary>
    protected int _maxVisibleItems;

    /// <summary>True while the user is dragging the scroll bar thumb.</summary>
    protected bool _isDraggingScrollBar;

    /// <summary>When true, the next click is ignored to prevent accidental selection after drag.</summary>
    protected bool _skipNextItemSelection;

    /// <summary>Index of the item currently under the cursor, or -1 if none.</summary>
    protected int _correctHoveredIndex = -1;

    /// <summary>Cached mouse state from the previous frame for delta calculations.</summary>
    protected MouseState _previousMouseState;

    /// <summary>Accumulated time in seconds for the pulsing hover animation.</summary>
    protected double _hoverAnimationTime;

    /// <summary>The thin scroll bar child control shown when the list is scrollable.</summary>
    protected XNADropDownScrollBar _dropDownScrollBar;

    /// <summary>True when MaxVisibleItems > 0 and there are more items than fit.</summary>
    protected bool _isScrollable => _maxVisibleItems > 0 && Items.Count > _maxVisibleItems;

    /// <summary>
    /// Gets or sets the maximum number of visible items when the dropdown is open.
    /// Use 0 to show all items without scrolling.
    /// </summary>
    public int MaxVisibleItems
    {
        get => _maxVisibleItems;
        set => _maxVisibleItems = Math.Max(0, value);
    }

    /// <summary>
    /// Creates a new drop-down control.
    /// </summary>
    /// <param name="windowManager">The WindowManager associated with this control.</param>
    public XNADropDown(WindowManager windowManager) : base(windowManager)
    {
        ItemHeight = UISettings.ActiveSettings.DropDownDefaultItemHeight.GetValueOrDefault((int)FontManager.GetTextDimensions("Test String @", FontIndex).Y + 1);
        Height = ItemHeight + 2;
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
    public event SelectedIndexChangedEventHandler SelectedIndexChanged;

    /// <summary>
    /// Raised when the user re-selects an already selected drop-down item.
    /// </summary>
    public event EventHandler IndexReselected;

    public int TopIndex { get; set; }

    /// <summary>The height of drop-down items in pixels.</summary>
    public int ItemHeight { get; set; }

    /// <summary>The list of items displayed in the drop-down.</summary>
    public List<XNADropDownItem> Items = new List<XNADropDownItem>();

    /// <summary>Gets the current drop-down state (closed, opened down, or opened up).</summary>
    public DropDownState DropDownState { get; private set; }

    private bool _allowDropDown = true;

    /// <summary>
    /// Controls whether the drop-down control can be dropped down.
    /// </summary>
    public bool AllowDropDown
    {
        get { return _allowDropDown; }
        set
        {
            _allowDropDown = value;
            if (!_allowDropDown && DropDownState != DropDownState.CLOSED)
                CloseDropDown();
        }
    }

    private int _selectedIndex = -1;

    /// <summary>
    /// Gets or sets the currently selected item index.
    /// Fires <see cref="SelectedIndexChanged"/> or <see cref="IndexReselected"/> on set.
    /// </summary>
    public int SelectedIndex
    {
        get { return _selectedIndex; }
        set
        {
            int oldSelectedIndex = _selectedIndex;

            _selectedIndex = value;

            if (value != oldSelectedIndex)
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            else
                IndexReselected?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Gets the currently selected item, or null if none is selected.</summary>
    public XNADropDownItem SelectedItem
    {
        get
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return null;

            return Items[SelectedIndex];
        }
    }

    public int FontIndex { get; set; }

    private Color? _borderColor;
    public Color BorderColor
    {
        get => _borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set { _borderColor = value; }
    }

    private Color? _focusColor;
    public Color FocusColor
    {
        get => _focusColor ?? UISettings.ActiveSettings.FocusColor;
        set { _focusColor = value; }
    }

    private Color? _backColor;
    public Color BackColor
    {
        get => _backColor ?? UISettings.ActiveSettings.BackgroundColor;
        set { _backColor = value; }
    }

    private Color? _textColor;
    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.AltColor;
        set { _textColor = value; }
    }

    private Color? _disabledItemColor;
    public Color DisabledItemColor
    {
        get => _disabledItemColor ?? UISettings.ActiveSettings.DisabledItemColor;
        set { _disabledItemColor = value; }
    }

    public bool OpenUp { get; set; }

    private bool _showEllipsisOnOverflow = false;

    /// <summary>
    /// When the selected item's text is too wide for the closed control, it is always
    /// cut off at the last fully fitting character. When enabled, an ellipsis ("...") is
    /// appended to indicate truncation.
    /// </summary>
    public bool ShowEllipsisOnOverflow
    {
        get => _showEllipsisOnOverflow;
        set
        {
            _showEllipsisOnOverflow = value;
            InvalidateDisplayTextCache();
        }
    }

    public Texture2D DropDownTexture { get; set; }
    public Texture2D DropDownOpenTexture { get; set; }
    public EnhancedSoundEffect ClickSoundEffect { get; set; }

    private bool clickedAfterOpen = false;
    private int numFittingItems = 0;

    /// <summary>
    /// The width of the open dropdown list, computed as the maximum of the
    /// control's width and the widest item's content width.
    /// </summary>
    private int expandedListWidth = 0;

    private int? closedWidth = null;

    private void SyncWidthToDropDownState()
    {
        if (DropDownState != DropDownState.CLOSED)
        {
            if (closedWidth == null)
                closedWidth = Width;

            int openWidth = Math.Max(closedWidth.Value, expandedListWidth);
            if (Width != openWidth)
                Width = openWidth;
        }
        else
        {
            if (closedWidth != null && Width != closedWidth.Value)
                Width = closedWidth.Value;
            closedWidth = null;
            expandedListWidth = 0;
        }
    }

    private int CurrentDisplayWidth
    {
        get
        {
            SyncWidthToDropDownState();
            return Width;
        }
    }

    private (string cachedDisplayText, int cachedSelectedIndex, int cachedWidth, int cachedFontIndex, string cachedItemText) displayTextCache = (null, -1, 0, 0, null);

    #region AddItem methods

    public void AddItem(XNADropDownItem item) => Items.Add(item);

    public void AddItem(string text)
    {
        var item = new XNADropDownItem { Text = text };
        Items.Add(item);
    }

    public void AddItem(string text, Texture2D texture)
    {
        var item = new XNADropDownItem { Text = text, Texture = texture };
        Items.Add(item);
    }

    public void AddItem(string text, Color color)
    {
        var item = new XNADropDownItem { Text = text, TextColor = color };
        Items.Add(item);
    }

    #endregion

    /// <summary>
    /// Initializes the drop-down textures, creates the scroll bar child control,
    /// and subscribes to input events.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        DropDownTexture = AssetLoader.LoadTexture("comboBoxArrow.png");
        DropDownOpenTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");
        Height = DropDownTexture.Height;

        _dropDownScrollBar = new XNADropDownScrollBar(WindowManager)
        {
            ScrollBarWidth = DefaultScrollBarWidth,
            BarColor = BackColor,
            ThumbColor = FocusColor,
            ThumbBorderColor = BorderColor
        };
        _dropDownScrollBar.Scrolled += ScrollBar_Scrolled;

        _previousMouseState = Mouse.GetState();
        ClientRectangleUpdated += (s, e) => InvalidateDisplayTextCache();
    }

    /// <summary>
    /// Syncs _scrollOffset when the user clicks on the scroll bar track.
    /// </summary>
    private void ScrollBar_Scrolled(object sender, EventArgs e)
    {
        _scrollOffset = _dropDownScrollBar.ViewTop;
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "OpenUp":
                OpenUp = Conversions.BooleanFromString(value, OpenUp);
                return;
            case "DropDownTexture":
                DropDownTexture = AssetLoader.LoadTextureUncached(value);
                return;
            case "DropDownOpenTexture":
                DropDownOpenTexture = AssetLoader.LoadTextureUncached(value);
                return;
            case "ItemHeight":
                ItemHeight = Conversions.IntFromString(value, ItemHeight);
                return;
            case "ClickSoundEffect":
                ClickSoundEffect = new EnhancedSoundEffect(value);
                return;
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, FontIndex);
                return;
            case "BorderColor":
                BorderColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "FocusColor":
                FocusColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "BackColor":
                BackColor = AssetLoader.GetRGBAColorFromString(value);
                return;
            case "DisabledItemColor":
                DisabledItemColor = AssetLoader.GetColorFromString(value);
                return;
            case "ShowEllipsisOnOverflow":
                ShowEllipsisOnOverflow = Conversions.BooleanFromString(value, false);
                return;
        }

        if (key.StartsWith("Option", StringComparison.InvariantCulture))
        {
            AddItem(value);
            return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    protected Color GetItemTextColor(XNADropDownItem item) =>
        item.TextColor ?? TextColor;

    /// <summary>
    /// Per-frame update: tracks hover index, handles scroll bar dragging,
    /// and closes the drop-down when the user clicks outside its bounds.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (DropDownState == DropDownState.CLOSED)
        {
            _correctHoveredIndex = -1;
            return;
        }

        _hoverAnimationTime += gameTime.ElapsedGameTime.TotalSeconds;

        Point cursorPoint = GetCursorPoint();
        MouseState mouseState = Mouse.GetState();

        if (_isDraggingScrollBar)
            _correctHoveredIndex = -1;
        else
            _correctHoveredIndex = GetHoveredIndexWithScroll(cursorPoint);

        // Scroll bar drag handling
        if (_isScrollable)
        {
            Rectangle sbBounds = new Rectangle(
                Width - _dropDownScrollBar.ScrollBarWidth,
                DropDownState == DropDownState.OPENED_DOWN ? DropDownTexture.Height + 1 : 1,
                _dropDownScrollBar.ScrollBarWidth,
                Height - DropDownTexture.Height - 2);

            if (mouseState.LeftButton == ButtonState.Pressed && sbBounds.Contains(cursorPoint))
            {
                if (!_isDraggingScrollBar)
                {
                    _isDraggingScrollBar = true;
                    _dropDownScrollBar.HandleTrackClick(cursorPoint);
                }
            }

            if (_isDraggingScrollBar)
            {
                Point sbLocal = new Point(cursorPoint.X - sbBounds.X, cursorPoint.Y - sbBounds.Y);
                _dropDownScrollBar.HandleTrackClick(new Point(sbLocal.X + _dropDownScrollBar.ScrollBarWidth / 2, sbLocal.Y));
                _scrollOffset = _dropDownScrollBar.ViewTop;
            }

            if (mouseState.LeftButton == ButtonState.Released && _isDraggingScrollBar)
            {
                _skipNextItemSelection = true;
                _isDraggingScrollBar = false;
            }
        }

        // Close the dropdown when the user clicks outside its bounds while it is open.
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            Point p = GetCursorPoint();
            if (p.X < 0 || p.X > CurrentDisplayWidth || p.Y < 0 || p.Y > Height)
            {
                CloseDropDown();
                _previousMouseState = mouseState;
                return;
            }
        }

        _previousMouseState = mouseState;
    }

    /// <summary>
    /// Handles left mouse button press: opens the drop-down list and detaches it
    /// from the parent panel so it can overlay other controls.
    /// </summary>
    public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
    {
        base.OnMouseLeftDown(inputEventArgs);

        if (!AllowDropDown)
            return;

        inputEventArgs.Handled = true;

        if (DropDownState != DropDownState.CLOSED)
            return;

        ClickSoundEffect?.Play();
        clickedAfterOpen = false;
        OpenDropDown();
        Detach();
    }

    /// <summary>
    /// Opens the drop-down list: calculates height, positions the scroll bar child
    /// when scrollable, and adds it as a child control.
    /// </summary>
    public virtual void OpenDropDown()
    {
        TopIndex = 0;
        _scrollOffset = 0;

        expandedListWidth = Width;
        foreach (var item in Items)
        {
            int itemWidth = 4;
            if (item.Texture != null)
                itemWidth += item.Texture.Width + 1;
            if (item.Text != null)
                itemWidth += (int)Math.Ceiling(Renderer.MeasureString(item.Text, FontIndex).X);
            if (itemWidth > expandedListWidth)
                expandedListWidth = itemWidth;
        }

        int visibleItemCount = _maxVisibleItems > 0
            ? Math.Min(_maxVisibleItems, Items.Count)
            : Items.Count;

        if (!OpenUp)
        {
            DropDownState = DropDownState.OPENED_DOWN;

            if (_isScrollable)
            {
                Height = DropDownTexture.Height + 2 + ItemHeight * visibleItemCount;
            }
            else
            {
                numFittingItems = (WindowManager.RenderResolutionY - (GetWindowRectangle().Bottom + 1)) / (ItemHeight * GetTotalScalingRecursive());
                Height = DropDownTexture.Height + 2 + ItemHeight * Math.Min(numFittingItems, Items.Count);
            }
        }
        else
        {
            DropDownState = DropDownState.OPENED_UP;
            Y -= 1 + ItemHeight * visibleItemCount;
            Height = DropDownTexture.Height + 1 + ItemHeight * visibleItemCount;
        }

        SyncWidthToDropDownState();

        // Position and configure the scroll bar child when the list is scrollable.
        if (_isScrollable)
        {
            int sbWidth = _dropDownScrollBar.ScrollBarWidth;
            int listHeight = Height - DropDownTexture.Height - 2;
            int sbY = DropDownState == DropDownState.OPENED_DOWN ? DropDownTexture.Height + 1 : 1;

            _dropDownScrollBar.ClientRectangle = new Rectangle(Width - sbWidth, sbY, sbWidth, listHeight);
            _dropDownScrollBar.Length = Items.Count;
            _dropDownScrollBar.DisplayedPixelCount = _maxVisibleItems;
            _dropDownScrollBar.ViewTop = _scrollOffset;
            _dropDownScrollBar.Refresh();

            if (_dropDownScrollBar.Parent == null)
                AddChild(_dropDownScrollBar);
        }
    }

    /// <summary>
    /// Handles left click when the drop-down is open: selects items, handles
    /// scroll bar area clicks, and closes the list when clicking outside.
    /// </summary>
    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        base.OnLeftClick(inputEventArgs);
        inputEventArgs.Handled = true;

        if (DropDownState == DropDownState.CLOSED)
            return;

        if (_skipNextItemSelection)
        {
            _skipNextItemSelection = false;
            return;
        }

        Point cursorPoint = GetCursorPoint();

        if (_isScrollable)
        {
            Rectangle itemsRect = DropDownState == DropDownState.OPENED_DOWN
                ? new Rectangle(0, DropDownTexture.Height, CurrentDisplayWidth - _dropDownScrollBar.ScrollBarWidth, Height - DropDownTexture.Height)
                : new Rectangle(0, 0, CurrentDisplayWidth - _dropDownScrollBar.ScrollBarWidth, Height - DropDownTexture.Height);

            if (!_isDraggingScrollBar && itemsRect.Contains(cursorPoint))
            {
                int relativeY = cursorPoint.Y - itemsRect.Y - 1;
                int clickedIndex = relativeY / ItemHeight + _scrollOffset;

                if (clickedIndex >= 0 && clickedIndex < Items.Count && Items[clickedIndex].Selectable)
                {
                    SelectedIndex = clickedIndex;
                    ClickSoundEffect?.Play();
                    CloseDropDown();
                    return;
                }
            }

            Rectangle sbBounds = new Rectangle(
                Width - _dropDownScrollBar.ScrollBarWidth,
                DropDownState == DropDownState.OPENED_DOWN ? DropDownTexture.Height + 1 : 1,
                _dropDownScrollBar.ScrollBarWidth,
                Height - DropDownTexture.Height - 2);

            if (sbBounds.Contains(cursorPoint))
                return;
        }

        int itemIndexOnCursor = GetItemIndexOnCursor();

        if (itemIndexOnCursor == -1 && !clickedAfterOpen)
        {
            clickedAfterOpen = true;
            return;
        }

        if (itemIndexOnCursor > -1)
        {
            if (Items[itemIndexOnCursor].Selectable)
                SelectedIndex = itemIndexOnCursor;
            else
                return;
        }

        ClickSoundEffect?.Play();
        CloseDropDown();
    }

    /// <summary>
    /// Closes the drop-down list: resets height, re-attaches to parent,
    /// clears hover/drag state, resets hover animation, and removes the scroll bar child.
    /// </summary>
    protected virtual void CloseDropDown()
    {
        if (DropDownState == DropDownState.OPENED_UP)
            Y = Bottom - DropDownTexture.Height;

        Height = DropDownTexture.Height;
        DropDownState = DropDownState.CLOSED;
        SyncWidthToDropDownState();
        Attach();
        _isDraggingScrollBar = false;
        _skipNextItemSelection = false;
        _correctHoveredIndex = -1;
        _hoverAnimationTime = 0;

        if (_dropDownScrollBar.Parent != null)
            RemoveChild(_dropDownScrollBar);
    }

    /// <summary>
    /// Handles scroll wheel input. When closed, cycles through items.
    /// When open and scrollable, scrolls the item list and syncs the scroll bar thumb.
    /// When open outside the dropdown bounds, suppresses the event to prevent
    /// parent controls from scrolling.
    /// </summary>
    public override void OnMouseScrolled(InputEventArgs inputEventArgs)
    {
        if (!AllowDropDown)
            return;

        if (DropDownState != DropDownState.CLOSED)
        {
            Point p = GetCursorPoint();
            if (p.X < 0 || p.X > CurrentDisplayWidth || p.Y < 0 || p.Y > Height)
            {
                inputEventArgs.Handled = true;
                return;
            }
        }

        if (DropDownState == DropDownState.CLOSED)
        {
            if (Cursor.ScrollWheelValue < 0)
            {
                if (SelectedIndex >= Items.Count - 1)
                    return;
                inputEventArgs.Handled = true;
                if (Items[SelectedIndex + 1].Selectable)
                    SelectedIndex++;
            }

            if (Cursor.ScrollWheelValue > 0)
            {
                if (SelectedIndex < 1)
                    return;
                inputEventArgs.Handled = true;
                if (Items[SelectedIndex - 1].Selectable)
                    SelectedIndex--;
            }
        }
        else if (_isScrollable)
        {
            bool scrolled = false;

            if (Cursor.ScrollWheelValue < 0)
            {
                int maxScrollOffset = Math.Max(0, Items.Count - _maxVisibleItems);
                if (_scrollOffset < maxScrollOffset)
                {
                    inputEventArgs.Handled = true;
                    _scrollOffset = Math.Min(_scrollOffset + 1, maxScrollOffset);
                    scrolled = true;
                }
            }
            else if (Cursor.ScrollWheelValue > 0)
            {
                if (_scrollOffset > 0)
                {
                    inputEventArgs.Handled = true;
                    _scrollOffset = Math.Max(0, _scrollOffset - 1);
                    scrolled = true;
                }
            }

            if (scrolled)
            {
                _dropDownScrollBar.ViewTop = _scrollOffset;
                _dropDownScrollBar.RefreshButtonY();
            }
        }

        base.OnMouseScrolled(inputEventArgs);
    }

    private bool AllowScrollingItemList()
    {
        if (OpenUp)
            return false;
        return numFittingItems < Items.Count;
    }

    /// <summary>
    /// Returns the index of the item that the cursor currently points to,
    /// or -1 if the cursor is in the header area, or -2 if outside the control.
    /// </summary>
    private int GetItemIndexOnCursor()
    {
        Point p = GetCursorPoint();

        if (p.X < 0 || p.X > CurrentDisplayWidth || p.Y > Height || p.Y < 0)
            return -2;

        int itemIndex;

        if (DropDownState == DropDownState.OPENED_DOWN)
        {
            if (p.Y < DropDownTexture.Height + 1)
                return -1;
            int y = p.Y - DropDownTexture.Height - 1;
            itemIndex = TopIndex + (y / ItemHeight);
        }
        else
        {
            if (p.Y > ClientRectangle.Height - DropDownTexture.Height - 1)
                return -1;
            itemIndex = (p.Y - 1) / ItemHeight;
        }

        if (itemIndex < Items.Count && itemIndex > -1)
            return itemIndex;

        return -1;
    }

    private void InvalidateDisplayTextCache()
    {
        displayTextCache = (null, -1, 0, 0, null);
    }

    private string GetDisplayTextForSelectedItem(XNADropDownItem item, int textX)
    {
        (string cachedDisplayText, int cachedSelectedIndex, int cachedWidth, int cachedFontIndex, string cachedItemText) = displayTextCache;

        if (cachedDisplayText != null &&
            cachedSelectedIndex == SelectedIndex &&
            cachedWidth == Width &&
            cachedFontIndex == FontIndex &&
            cachedItemText == item.Text)
        {
            return cachedDisplayText;
        }

        string displayText = item.Text;

        if (item.Text != null)
        {
            int availableWidth = ShowEllipsisOnOverflow ? (Width - textX - DropDownTexture.Width - 2) : (Width - textX - 2);

            if (availableWidth <= 0)
            {
                displayText = string.Empty;
            }
            else
            {
                Vector2 textSize = Renderer.MeasureString(item.Text, FontIndex);

                if (textSize.X > availableWidth)
                {
                    const string ellipsis = "...";
                    float ellipsisWidth = ShowEllipsisOnOverflow
                        ? Renderer.MeasureString(ellipsis, FontIndex).X
                        : 0f;
                    float maxWidth = availableWidth - ellipsisWidth;

                    if (maxWidth <= 0)
                    {
                        displayText = string.Empty;
                    }
                    else
                    {
                        int bestFit = 0;
                        int low = 0;
                        int high = item.Text.Length;

                        while (low <= high)
                        {
                            int mid = low + ((high - low) / 2);
                            string test = item.Text.SubstringSurrogateAware(0, mid);
                            float currentWidth = Renderer.MeasureString(test, FontIndex).X;

                            if (currentWidth <= maxWidth)
                            {
                                bestFit = mid;
                                low = mid + 1;
                            }
                            else
                            {
                                high = mid - 1;
                            }
                        }

                        displayText = item.Text.SubstringSurrogateAware(0, bestFit);
                        if (ShowEllipsisOnOverflow)
                            displayText += ellipsis;
                    }
                }
            }
        }

        displayTextCache = (displayText, SelectedIndex, Width, FontIndex, item.Text);
        return displayText;
    }

    public override void Draw(GameTime gameTime)
    {
        Rectangle dropDownRect;
        if (DropDownState == DropDownState.CLOSED)
            dropDownRect = new Rectangle(0, 0, Width, Height);
        else if (DropDownState == DropDownState.OPENED_DOWN)
            dropDownRect = new Rectangle(0, 0, Width, DropDownTexture.Height);
        else
            dropDownRect = new Rectangle(0, Height - DropDownTexture.Height, Width, DropDownTexture.Height);

        FillRectangle(new Rectangle(dropDownRect.X + 1, dropDownRect.Y + 1,
            dropDownRect.Width - 2, dropDownRect.Height - 2), BackColor);
        DrawRectangle(dropDownRect, BorderColor);

        if (SelectedIndex > -1 && SelectedIndex < Items.Count)
        {
            XNADropDownItem item = Items[SelectedIndex];
            int textX = 3;
            if (item.Texture != null)
            {
                DrawTexture(item.Texture,
                    new Rectangle(1, dropDownRect.Y + 2, item.Texture.Width, item.Texture.Height), Color.White);
                textX += item.Texture.Width + 1;
            }

            if (item.Text != null)
            {
                string displayText = GetDisplayTextForSelectedItem(item, textX);
                int textY = dropDownRect.Y + Renderer.GetTextYPadding(displayText, FontIndex, dropDownRect.Height);
                DrawStringWithShadow(displayText, FontIndex,
                    new Vector2(textX, textY), GetItemTextColor(item));
            }
        }

        if (AllowDropDown)
        {
            var ddRectangle = new Rectangle(Width - DropDownTexture.Width,
                dropDownRect.Y, DropDownTexture.Width, DropDownTexture.Height);

            if (DropDownState != DropDownState.CLOSED)
            {
                DrawTexture(DropDownOpenTexture, ddRectangle, RemapColor);

                Rectangle listRectangle;
                if (DropDownState == DropDownState.OPENED_DOWN)
                    listRectangle = new Rectangle(0, DropDownTexture.Height, CurrentDisplayWidth, Height - DropDownTexture.Height);
                else
                    listRectangle = new Rectangle(0, 0, CurrentDisplayWidth, Height - DropDownTexture.Height);

                DrawRectangle(listRectangle, BorderColor);

                int visibleCount = _isScrollable ? _maxVisibleItems : Math.Min(numFittingItems, Items.Count - TopIndex);

                for (int i = 0; i < visibleCount; i++)
                {
                    int y = listRectangle.Y + 1 + i * ItemHeight;
                    DrawItem(_isScrollable ? _scrollOffset + i : TopIndex + i, y);
                }
            }
            else
            {
                DrawTexture(DropDownTexture, ddRectangle, RemapColor);
            }
        }

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a single drop-down item with selection and pulsing hover highlight.
    /// </summary>
    protected virtual void DrawItem(int index, int y)
    {
        if (_isScrollable)
        {
            int visibleIndex = index - _scrollOffset;
            if (visibleIndex < 0 || visibleIndex >= _maxVisibleItems)
                return;

            int baseY = DropDownState == DropDownState.OPENED_DOWN
                ? DropDownTexture.Height + 1
                : 1;
            y = baseY + visibleIndex * ItemHeight;
        }

        XNADropDownItem item = Items[index];

        int itemWidth = CurrentDisplayWidth - (_isScrollable ? _dropDownScrollBar.ScrollBarWidth + 1 : 2);

        if (SelectedIndex == index)
            FillRectangle(new Rectangle(1, y, itemWidth, ItemHeight), FocusColor);
        else if (_correctHoveredIndex == index)
        {
            float t = (float)(Math.Sin(_hoverAnimationTime * 2.0) * 0.5 + 0.5);
            FillRectangle(new Rectangle(1, y, itemWidth, ItemHeight),
                Color.Lerp(BackColor, FocusColor, t));
        }
        else
            FillRectangle(new Rectangle(1, y, itemWidth, ItemHeight), BackColor);

        int textX = 2;
        if (item.Texture != null)
        {
            DrawTexture(item.Texture, new Rectangle(1, y + 1, item.Texture.Width, item.Texture.Height), Color.White);
            textX += item.Texture.Width + 1;
        }

        Color textColor = item.Selectable ? GetItemTextColor(item) : DisabledItemColor;
        if (item.Text != null)
        {
            int textY = y + Renderer.GetTextYPadding(item.Text, FontIndex, ItemHeight);
            DrawStringWithShadow(item.Text, FontIndex, new Vector2(textX, textY), textColor);
        }
    }

    /// <summary>
    /// Adjusts the dropdown height for OPENED_UP state when the visible item count changes.
    /// </summary>
    protected void AdjustDropDownHeight()
    {
        int originalHeight = Height;
        int visibleItemCount = _maxVisibleItems > 0 ? Math.Min(_maxVisibleItems, Items.Count) : Items.Count;
        int newHeight = DropDownTexture.Height + 2 + ItemHeight * visibleItemCount;

        if (DropDownState == DropDownState.OPENED_UP)
            Y -= (newHeight - originalHeight);

        Height = newHeight;
    }

    /// <summary>
    /// Ensures the currently selected item is visible in the scrollable list.
    /// Adjusts <see cref="_scrollOffset"/> if needed and syncs the scroll bar thumb.
    /// </summary>
    /// <param name="resetOnNoSelection">If true and no item is selected, resets scroll offset to 0.</param>
    protected void EnsureSelectedIndexVisible(bool resetOnNoSelection = false)
    {
        if (!_isScrollable || SelectedIndex < 0)
        {
            if (resetOnNoSelection)
                _scrollOffset = 0;
            return;
        }

        if (SelectedIndex < _scrollOffset)
            _scrollOffset = SelectedIndex;
        else if (SelectedIndex >= _scrollOffset + _maxVisibleItems)
            _scrollOffset = SelectedIndex - _maxVisibleItems + 1;

        if (_isScrollable)
        {
            _dropDownScrollBar.ViewTop = _scrollOffset;
            _dropDownScrollBar.RefreshButtonY();
        }
    }

    /// <summary>
    /// Returns the index of the item that the cursor currently points to,
    /// taking the current scroll offset into account.
    /// </summary>
    protected int GetHoveredIndexWithScroll(Point cursorPoint)
    {
        if (DropDownState == DropDownState.CLOSED)
            return -1;

        if (!_isScrollable)
        {
            Rectangle itemsArea = DropDownState == DropDownState.OPENED_DOWN
                ? new Rectangle(0, DropDownTexture.Height + 1, Width, Height - DropDownTexture.Height - 2)
                : new Rectangle(0, 1, Width, Height - DropDownTexture.Height - 2);

            if (!itemsArea.Contains(cursorPoint))
                return -1;

            int relativeY = cursorPoint.Y - itemsArea.Y;
            int index = relativeY / ItemHeight;
            if (index >= 0 && index < Items.Count && Items[index].Selectable)
                return index;
            return -1;
        }

        Rectangle itemsAreaScrolled = DropDownState == DropDownState.OPENED_DOWN
            ? new Rectangle(0, DropDownTexture.Height + 1, CurrentDisplayWidth - _dropDownScrollBar.ScrollBarWidth, Height - DropDownTexture.Height - 2)
            : new Rectangle(0, 1, CurrentDisplayWidth - _dropDownScrollBar.ScrollBarWidth, Height - DropDownTexture.Height - 2);

        if (!itemsAreaScrolled.Contains(cursorPoint))
            return -1;

        int relativeYScrolled = cursorPoint.Y - itemsAreaScrolled.Y;
        int visibleIndex = relativeYScrolled / ItemHeight;

        if (visibleIndex < 0 || visibleIndex >= _maxVisibleItems)
            return -1;

        int realIndex = visibleIndex + _scrollOffset;
        if (realIndex >= 0 && realIndex < Items.Count && Items[realIndex].Selectable)
            return realIndex;

        return -1;
    }
}
