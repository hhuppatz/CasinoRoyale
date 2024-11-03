using System;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class Platform
{
    private PlatformState platformState;
    private Texture2D tex;

    public Platform(uint platNum, Texture2D tex, Vector2 L, Vector2 R)
    {
        this.tex = tex;
        platformState.platNum = platNum;
        platformState.L = L;
        platformState.R = R;
    }

    public PlatformState GetState()
    {
        return platformState;
    }

    public Vector2 GetLCoords()
    {
        return platformState.L;
    }

    public Vector2 GetRCoords()
    {
        return platformState.R;
    }

    public int GetWidth()
    {
        return Math.Abs((int)platformState.L.X - (int)platformState.R.X);
    }

    public Texture2D GetTex()
    {
        return tex;
    }

    public Vector2 GetCoords()
    {
        return (platformState.R + platformState.L)/2;
    }
}

public struct PlatformState: INetSerializable
{
    public uint platNum;
    public Vector2 L;
    public Vector2 R;
    public void Serialize(NetDataWriter writer) {
        writer.Put(platNum);
        writer.Put(L);
        writer.Put(R);
    }
    public void Deserialize(NetDataReader reader) {
        platNum = reader.GetUInt();
        L = reader.GetVector2();
        R = reader.GetVector2();
    }
}