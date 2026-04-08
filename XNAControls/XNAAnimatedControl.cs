using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.IO.Compression;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A control that plays an animated GIF.
/// Frame pixel data is kept deflate-compressed in system RAM; a single
/// <see cref="Texture2D"/> is re-uploaded via <see cref="Texture2D.SetData{T}(T[])"/>
/// only when the displayed frame changes, so GPU memory stays constant at
/// one frame's worth regardless of how many frames the animation has.
/// Load frames by calling <see cref="LoadGif"/>.
/// </summary>
public class XNAAnimatedControl : XNAControl
{
    public XNAAnimatedControl(WindowManager windowManager) : base(windowManager)
    {
    }

    // Per-frame deflate-compressed premultiplied RGBA bytes, stored in system RAM.
    private byte[][] _compressedFrames = Array.Empty<byte[]>();
    private int[] _frameDelaysMs = Array.Empty<int>();

    // Shared buffers reused on every frame upload (allocated once in LoadGif).
    private byte[] _decompressBuffer;   // raw RGBA bytes for one frame
    private Color[] _pixelBuffer;       // Color[] passed to SetData

    // Single GPU texture; pixel data is replaced on each frame advance.
    private Texture2D _renderTexture;

    private int _currentFrame;
    private double _frameElapsedMs;
    private bool _frameDirty;

    /// <summary>
    /// Gets or sets whether the animation loops continuously. Default: true.
    /// </summary>
    public bool Looping { get; set; } = true;

    /// <summary>
    /// Loads an animated GIF from the given path.
    /// The path may be absolute or a filename relative to the configured
    /// <see cref="AssetLoader.AssetSearchPaths"/>.
    /// If no explicit size has been set on this control, the control is
    /// auto-sized to match the GIF's canvas dimensions.
    /// </summary>
    public void LoadGif(string path)
    {
        _renderTexture?.Dispose();
        _renderTexture = null;
        _compressedFrames = Array.Empty<byte[]>();
        _frameDelaysMs = Array.Empty<int>();

        var (frames, delays, width, height) = AssetLoader.LoadGifFrames(path);
        if (frames.Length == 0)
            return;

        _compressedFrames = frames;
        _frameDelaysMs = delays;
        _currentFrame = 0;
        _frameElapsedMs = 0;
        _frameDirty = true;

        int totalPixels = width * height;
        _decompressBuffer = new byte[totalPixels * 4];
        _pixelBuffer = new Color[totalPixels];
        _renderTexture = AssetLoader.CreateTexture(Color.Transparent, width, height);

        if (Width == 0 && Height == 0)
        {
            Width = width;
            Height = height;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_compressedFrames.Length == 0)
            return;

        if (_compressedFrames.Length == 1)
        {
            if (_frameDirty)
                UploadCurrentFrame();
            return;
        }

        _frameElapsedMs += gameTime.ElapsedGameTime.TotalMilliseconds;

        while (_frameElapsedMs >= _frameDelaysMs[_currentFrame])
        {
            _frameElapsedMs -= _frameDelaysMs[_currentFrame];
            _currentFrame++;

            if (_currentFrame >= _compressedFrames.Length)
            {
                if (Looping)
                {
                    _currentFrame = 0;
                }
                else
                {
                    _currentFrame = _compressedFrames.Length - 1;
                    _frameElapsedMs = 0;
                    break;
                }
            }

            _frameDirty = true;
        }

        if (_frameDirty)
            UploadCurrentFrame();
    }

    public override void Draw(GameTime gameTime)
    {
        if (_renderTexture != null)
        {
            DrawTexture(
                _renderTexture,
                new Rectangle(0, 0, Width, Height),
                RemapColor * Alpha);
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderTexture?.Dispose();
            _renderTexture = null;
            _compressedFrames = Array.Empty<byte[]>();
            _frameDelaysMs = Array.Empty<int>();
            _decompressBuffer = null;
            _pixelBuffer = null;
        }

        base.Dispose(disposing);
    }

    private void UploadCurrentFrame()
    {
        // Decompress the current frame's raw RGBA bytes into the shared buffer.
        using (var ms = new MemoryStream(_compressedFrames[_currentFrame]))
        using (var deflate = new DeflateStream(ms, CompressionMode.Decompress))
        {
            int offset = 0;
            int remaining = _decompressBuffer.Length;
            while (remaining > 0)
            {
                int read = deflate.Read(_decompressBuffer, offset, remaining);
                if (read == 0)
                    break;
                offset += read;
                remaining -= read;
            }
        }

        // Convert raw bytes [R,G,B,A,...] to Color[] for SetData.
        for (int i = 0; i < _pixelBuffer.Length; i++)
        {
            _pixelBuffer[i] = new Color(
                _decompressBuffer[i * 4],
                _decompressBuffer[i * 4 + 1],
                _decompressBuffer[i * 4 + 2],
                _decompressBuffer[i * 4 + 3]);
        }

        _renderTexture.SetData(_pixelBuffer);
        _frameDirty = false;
    }
}
