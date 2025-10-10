using Microsoft.Xna.Framework;
using System;
using CasinoRoyale.Classes.GameObjects.Interfaces;

namespace CasinoRoyale.Classes.GameObjects
{
    public sealed class MainCamera
{
    private Vector2 coords;
    private Vector2 offset;
    private Vector2 ratio;
    private MainCamera() {
        coords = new Vector2(0, 0);
        ratio = new Vector2(1f, 1f);
    }

    // Only allow one instance of Main Camera to exist (le singleton)
    private static readonly Lazy<MainCamera> lazy = new(() => new MainCamera());
    public static MainCamera Instance { get { return lazy.Value; }}

    public void InitMainCamera(GameWindow _window, PlayableCharacter player)
    {
        coords = player.Coords;
        offset = new Vector2(_window.ClientBounds.Width/2, _window.ClientBounds.Height/2 + 130);
    }

    public void MoveToFollowPlayer(PlayableCharacter player)
    {
        coords = Vector2.Subtract(player.Coords, offset);
    }

    public Vector2 TransformToView(Vector2 vec2)
    {
        return Vector2.Subtract(vec2, coords) * ratio;
    }

    public void ApplyRatio(Vector2 ratio)
    {
        this.ratio = ratio;
    }

    public void SetCoords(Vector2 vec)
    {
        coords = vec;
    }

    public Vector2 GetCoords()
    {
        return coords;
    }
}
}