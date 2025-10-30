using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CasinoRoyale.Utils;

// From the Monogame tutorials
public static class Resolution
{
    private static bool _isFullscreen = false;
    private static bool _isBorderless = false;
    private static int _width = 1;
    private static int _height = 1;
    private static int _curr_width = 1;
    private static int _curr_height = 1;
    public static Vector2 ratio = new Vector2(1f, 1f);

    public static void ToggleFullscreen(GameWindow _window, GraphicsDeviceManager _graphics) {
        bool oldIsFullscreen = _isFullscreen;

        if (_isBorderless) {
            _isBorderless = false;
        } else {
            _isFullscreen = !_isFullscreen;
        }

        ApplyFullscreenChange(_window, _graphics, oldIsFullscreen);
    }

    public static void ToggleBorderless(GameWindow _window, GraphicsDeviceManager _graphics) {
        bool oldIsFullscreen = _isFullscreen;

        _isBorderless = !_isBorderless;
        _isFullscreen = _isBorderless;

        ApplyFullscreenChange(_window, _graphics, oldIsFullscreen);
    }

    private static void ApplyFullscreenChange(GameWindow _window, GraphicsDeviceManager _graphics, bool oldIsFullscreen) {
        if (_isFullscreen) {
            if (oldIsFullscreen) {
                ApplyHardwareMode(_graphics);
            } else {
                SetFullscreen(_window, _graphics);
            }
        } else {
            UnsetFullscreen(_graphics);
        }
    }

    private static void ApplyHardwareMode(GraphicsDeviceManager _graphics) {
        _graphics.HardwareModeSwitch = !_isBorderless;
        _graphics.ApplyChanges();
    }

    private static void SetFullscreen(GameWindow _window, GraphicsDeviceManager _graphics) {
        _width = _window.ClientBounds.Width;
        _height = _window.ClientBounds.Height;

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.HardwareModeSwitch = !_isBorderless;

        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();
        _curr_width = _window.ClientBounds.Width;
        _curr_height = _window.ClientBounds.Height;
        ratio = new Vector2((float)_curr_width/_width, (float)_curr_height/_height);
    }

    private static void UnsetFullscreen(GraphicsDeviceManager _graphics) {
        _graphics.PreferredBackBufferWidth = _width;
        _graphics.PreferredBackBufferHeight = _height;
        _curr_width = _width;
        _curr_height = _height;
        ratio = new Vector2((float)_curr_width/_width, (float)_curr_height/_height);
        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }
}