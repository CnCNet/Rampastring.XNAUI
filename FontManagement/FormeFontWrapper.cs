#if !XNA && !NETFRAMEWORK
using FontStashSharp;
using Forme;
using Forme.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// A font wrapper that renders text using the Forme GPU-accelerated Slug algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Forme renders text directly from quadratic Bezier glyph outlines on the GPU without
/// precomputed textures, distance fields, or rasterized atlases. Text remains sharp at
/// any size and scale.
/// </para>
/// <para>
/// Because Forme uses its own renderer rather than <c>SpriteBatch</c>, each
/// <see cref="DrawString(SpriteBatch, string, Vector2, Color, float, float)"/> call
/// flushes the active <c>SpriteBatch</c>, draws with <see cref="FormeRenderer"/>, then
/// restarts the <c>SpriteBatch</c>. This preserves correct draw order at the cost of one
/// extra flush per text call.
/// </para>
/// </remarks>
public class FormeFontWrapper : IFont, IDisposable
{
    private readonly FormeFontDevice _fontDevice;
    private readonly FormeRenderer _renderer;
    private readonly int _sizePixels;

    // Ascent in pixels at size 1.0: multiply by (sizePixels * scale) when drawing.
    private readonly float _ascentRatio;

    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="FormeFontWrapper"/>.
    /// </summary>
    /// <param name="fontDevice">The GPU font device that owns the curve and band textures.</param>
    /// <param name="renderer">The shared <see cref="FormeRenderer"/> used for drawing.</param>
    /// <param name="sizePixels">The nominal em-square height in pixels for this font size.</param>
    public FormeFontWrapper(FormeFontDevice fontDevice, FormeRenderer renderer, int sizePixels)
    {
        _fontDevice = fontDevice ?? throw new ArgumentNullException(nameof(fontDevice));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _sizePixels = sizePixels;

        int unitsPerEm = Math.Max(1, fontDevice.Metrics.UnitsPerEm);
        _ascentRatio = (float)fontDevice.Metrics.Ascent / unitsPerEm;
    }

    /// <inheritdoc/>
    public Vector2 MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        var bounds = _fontDevice.Font.MeasureString(text.AsSpan(), (float)_sizePixels);
        return new Vector2(bounds.Width, bounds.Height);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The <paramref name="location"/> is interpreted as the top-left corner of the text,
    /// consistent with the rest of the XNAUI framework. Internally this is converted to a
    /// baseline origin as required by <see cref="FormeRenderer"/>.
    /// </remarks>
    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Forme expects a baseline origin; convert from the top-left corner used by XNAUI.
        float ascentPixels = _ascentRatio * _sizePixels * scale;
        Vector2 baseline = new Vector2(location.X, location.Y + ascentPixels);

        // Flush the active SpriteBatch before using FormeRenderer, then restart it.
        // FormeRenderer.Begin/End save and restore all GraphicsDevice state, so the
        // SpriteBatch can resume with Begin() using the same settings.
        Renderer.EndDraw();

        _renderer.Begin();
        // Note: explicitly passing through `new TextLayoutOptions()` to workaround the following issue:
        // '\n' in DrawString doesn't work without TextLayoutOptions #12
        // https://github.com/AristurtleDev/Forme/issues/12
        _renderer.DrawString(_fontDevice, text, baseline, _sizePixels * scale, color, options: new TextLayoutOptions());
        _renderer.End();

        Renderer.BeginDraw();
    }

    /// <inheritdoc/>
    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth)
    {
        // StringSegment overload: convert to string and delegate.
        // Rotation is not supported by FormeRenderer. For non-uniform scale, the larger
        // axis is used so that text is never clipped.
        float uniformScale = Math.Max(scale.X, scale.Y);
        DrawString(spriteBatch, text.ToString(), location, color, uniformScale, depth);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Always returns <see langword="true"/>. Forme skips glyphs not present in the baked
    /// character set rather than substituting a replacement character, so missing characters
    /// are simply not rendered.
    /// </remarks>
    public bool HasCharacter(char c) => true;

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <paramref name="str"/> unchanged. Forme handles missing glyphs silently by
    /// skipping them, so no character substitution is needed.
    /// </remarks>
    public string GetSafeString(string str) => str;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _fontDevice.Dispose();
        GC.SuppressFinalize(this);
    }
}
#endif
