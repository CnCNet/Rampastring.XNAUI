using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A control that has multiple tabs, of which only one can be selected at a time.
/// </summary>
public class XNATabControl : XNAControl
{
    public XNATabControl(WindowManager windowManager) : base(windowManager)
    {
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);

    [Obsolete("XNATabControl supports removing a tab via an INI configuration `RemoveTabIndex{id}`. Therefore, it is not reliable to use this event to determine the selected tab index. Use the callback methods in AddTab() instead.")]
    public event SelectedIndexChangedEventHandler SelectedIndexChanged;

    private int _selectedTab = -1;

    [Obsolete("XNATabControl supports removing a tab via an INI configuration `RemoveTabIndex{id}`. Therefore, it is not reliable to use this property to determine the selected tab index.")]
    public int SelectedTab
    {
        get { return _selectedTab; }
        set => SetSelectedTab(value);
    }

    private void SetSelectedTab(int value)
    {
        if (_selectedTab == value)
            return;

        int oldSelectedTab = _selectedTab;
        _selectedTab = value;

        if (oldSelectedTab >= 0 && oldSelectedTab < Tabs.Count)
            Tabs[oldSelectedTab].Selected = false;

        if (value >= 0 && value < Tabs.Count)
            Tabs[value].Selected = true;

        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public int FontIndex { get; set; }

    public bool DisposeTexturesOnTabRemove { get; set; }

    private Color? _textColor;

    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.AltColor;
        set { _textColor = value; }
    }

    private Color? _textColorDisabled;

    public Color TextColorDisabled
    {
        get => _textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
        set { _textColorDisabled = value; }
    }

    private List<Tab> Tabs = new List<Tab>();

    public EnhancedSoundEffect ClickSound { get; set; }

    public override void Initialize()
    {
        base.Initialize();
    }

    public void MakeSelectable(int index)
    {
        Tabs[index].Selectable = true;
    }

    public void MakeUnselectable(int index)
    {
        Tabs[index].Selectable = false;
    }

    public void RemoveTab(int index)
    {
        if (DisposeTexturesOnTabRemove)
        {
            Tabs[index].DefaultTexture.Dispose();
            Tabs[index].PressedTexture.Dispose();
        }

        if (index < 0 || index >= Tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Tab index is out of range. Got " + index + ", but the tab count is " + Tabs.Count);
        
        // Handle the selected tab index when a tab is removed
        bool selectedTabRemoved = index == _selectedTab;
        if (selectedTabRemoved)
        {
            Tabs[index].Selected = false;
            _selectedTab = -1;
        }
        else if (index < _selectedTab)
        {
            _selectedTab--;
        }

        Tabs.RemoveAt(index);

        if (selectedTabRemoved && Tabs.Count > 0)
            SetSelectedTab(0);
    }

    public void RemoveTab(string text)
    {
        int index = Tabs.FindIndex(t => t.Text == text);

        if (index == -1)
            throw new ArgumentException("No tab with the specified text exists.", nameof(text));

        RemoveTab(index);
    }

    /// <summary>
    /// Adds a tab to the control.
    /// </summary>
    /// <param name="text">The tab header text.</param>
    /// <param name="defaultTexture">The texture to use when the tab is not selected.</param>
    /// <param name="pressedTexture">The texture to use when the tab is selected.</param>
    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture)
    {
        AddTab(text, defaultTexture, pressedTexture, true, null, null);
    }

    /// <summary>
    /// Adds a tab to the control.
    /// </summary>
    /// <param name="text">The tab header text.</param>
    /// <param name="defaultTexture">The texture to use when the tab is not selected.</param>
    /// <param name="pressedTexture">The texture to use when the tab is selected.</param>
    /// <param name="selectable">Whether the tab can be selected or not.</param>
    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {

        AddTab(text, defaultTexture, pressedTexture, selectable, null, null);
    }

    /// <summary>
    /// Adds a tab to the control.
    /// </summary>
    /// <param name="text">The tab header text.</param>
    /// <param name="defaultTexture">The texture to use when the tab is not selected.</param>
    /// <param name="pressedTexture">The texture to use when the tab is selected.</param>
    /// <param name="selectable">Whether the tab can be selected or not.</param>
    /// <param name="onSelected">A callback method that is called when the tab is selected. Note: if this is the first tab added to the control, it will be selected by default but this callback will NOT be called.</param>
    /// <param name="onDeselected">A callback method that is called when the tab is deselected.</param>
    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable = true, Action onSelected = null, Action onDeselected = null)
    {
        var tab = new Tab(text, defaultTexture, pressedTexture, selectable);
        if (Tabs.Count == 0)
        {
            tab.Selected = true;
            _selectedTab = 0;            
        }

        if (onSelected != null)
            tab.TabSelected += (s, e) => onSelected();

        if (onDeselected != null)
            tab.TabDeselected += (s, e) => onDeselected();

        Tabs.Add(tab);

        Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
        tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
        tab.TextYPosition = Renderer.GetTextYPadding(text, FontIndex, defaultTexture.Height);

        Width += defaultTexture.Width;
        Height = defaultTexture.Height;
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "RemapColor":
            case "TextColor":
                TextColor = AssetLoader.GetColorFromString(value);
                return;
            case "TextColorDisabled":
                TextColorDisabled = AssetLoader.GetColorFromString(value);
                return;
        }

        if (key.StartsWith("RemoveTabIndex", StringComparison.InvariantCulture))
        {
            int index = int.Parse(key.Substring(14), CultureInfo.InvariantCulture);

            if (Conversions.BooleanFromString(value, false))
                RemoveTab(index);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        base.OnLeftClick(inputEventArgs);
        inputEventArgs.Handled = true;

        Point p = GetCursorPoint();

        int w = 0;
        int i = 0;
        foreach (Tab tab in Tabs)
        {
            w += tab.DefaultTexture.Width;

            if (p.X < w)
            {
                if (tab.Selectable)
                {
                    ClickSound?.Play();

                    SetSelectedTab(i);
                }

                return;
            }

            i++;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        int x = 0;

        for (int i = 0; i < Tabs.Count; i++)
        {
            Tab tab = Tabs[i];

            Texture2D texture = i == _selectedTab ? tab.PressedTexture : tab.DefaultTexture;

            DrawTexture(texture, new Point(x, 0), RemapColor);

            DrawStringWithShadow(tab.Text, FontIndex,
                new Vector2(x + tab.TextXPosition, tab.TextYPosition),
                tab.Selectable && Enabled ? TextColor : TextColorDisabled);

            x += tab.DefaultTexture.Width;
        }
    }
}

internal class Tab
{
    public Tab() { }

    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    public Texture2D DefaultTexture { get; set; }

    public Texture2D PressedTexture { get; set; }

    public string Text { get; set; }

    public bool Selectable { get; set; }

    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set
        {
            bool previousSelected = _selected;
            _selected = value;

            if (!previousSelected && _selected)
                TabSelected?.Invoke(this, EventArgs.Empty);
            else if (previousSelected && !_selected)
                TabDeselected?.Invoke(this, EventArgs.Empty);
        }

    }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }

    public event EventHandler TabSelected;
    public event EventHandler TabDeselected;
}
