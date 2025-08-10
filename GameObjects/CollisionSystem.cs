using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public class CollisionSystem
{
    private static float GRAVITY = 9.8f;
    private static float GRAVITY_HALF = 4.9f;

    private Rectangle _gameArea;
    public Rectangle GameArea { get => _gameArea; }
    private List<Platform> _platforms;
    public List<Platform> Platforms { get => _platforms; }
    private List<CasinoMachine> _casinoMachines;
    public List<CasinoMachine> CasinoMachines { get => _casinoMachines; }

    public CollisionSystem(Rectangle gameArea, List<Platform> platforms, List<CasinoMachine> casinoMachines)
    {
        _gameArea = gameArea;
        _platforms = platforms;
        _casinoMachines = casinoMachines;
    }

    // Coordinate system has 0,0 in top left and grows more positive to the right and down
    public void TryMovePlayer(PlayableCharacter player, KeyboardState ks, float dt)
    {
        // Get player input
        Vector2 m_playerAttemptedVelocity = Vector2.Zero;
        bool m_playerAttemptedJump = false;
        if (ks.IsKeyDown(Keys.A))
        {
            m_playerAttemptedVelocity += new Vector2(-50, 0);
        }
        if (ks.IsKeyDown(Keys.D))
        {
            m_playerAttemptedVelocity += new Vector2(50, 0);
        }
        if (ks.IsKeyDown(Keys.W))
        {
            m_playerAttemptedJump = true;
        }
        /*if (ks.IsKeyDown(Keys.S))
        {
            m_playerAttemptedVelocity += new Vector2(0, 100);
        }*/

        // Deal with player jumping
        player.InJump = m_playerAttemptedJump;
        if (player.InJumpSquat)
        {
            player.JumpSquatTimer -= dt;
        }
        else if (player.InJump)
        {
            m_playerAttemptedVelocity += new Vector2(0, -player.Velocity.Y * dt + GRAVITY_HALF * dt * dt);
            if (m_playerAttemptedVelocity.Y > 0)
            {
                player.InJump = false;
            }
        }
        else
        {
            m_playerAttemptedVelocity += new Vector2(0, GRAVITY * player.Mass);
        }

        // Apply external forces to velocity

        // Apply game rules to movement

        player.Coords = Vector2.Min(Vector2.Max(player.Coords + GetMoveDistance(player, m_playerAttemptedVelocity, dt), new Vector2(GameArea.Left, GameArea.Top)), new Vector2(GameArea.Right, GameArea.Bottom));
    }

    private Vector2 GetMoveDistance(PlayableCharacter player, Vector2 velocity, float dt)
    {
        Vector2 m_DoubleDistanceMoved = velocity * dt * 2;
        Rectangle m_NewHitbox = new Rectangle() with {X = player.Hitbox.X + (int)m_DoubleDistanceMoved.X, Y = player.Hitbox.Y + (int)m_DoubleDistanceMoved.Y, Size = player.Hitbox.Size};

        int m_CollisionOccured = 0;
        for (int i = 0; i < Platforms.Count; i++)
        {
            while (m_NewHitbox.Intersects(Platforms[i].Hitbox) && m_CollisionOccured < 8)
            {
                m_CollisionOccured++;

                m_DoubleDistanceMoved /= 2;
                m_NewHitbox.X -= (int)m_DoubleDistanceMoved.X;
                m_NewHitbox.Y -= (int)m_DoubleDistanceMoved.Y;
            }

            if (m_CollisionOccured == 8) m_DoubleDistanceMoved.Y = 0f;
            // If found the platform player is colliding with already, don't need to check every other in level (assumed)
            if (m_CollisionOccured > 0) break;
        }

        return m_DoubleDistanceMoved / 2;

        /*
        // Attempt at line segment intersection recognition using
        // equations from Wikipedia's article on line segment intersection using endpoints
        float x1 = oldCoords.X;
        float y1 = oldCoords.Y;
        float x3 = newCoords.X;
        float y3 = newCoords.Y;
        
        float x2, y2, x4, y4;

        for (int i = 0; i < Platforms.Count; i++)
        {
            x2 = Platforms[i].GetLCoords().X;
            y2 = Platforms[i].GetLCoords().Y;
            x4 = Platforms[i].GetRCoords().X;
            y4 = Platforms[i].GetRCoords().Y;

            float t = ((x1-x3)*(y3-y4) - (y1-y3)*(x3-x4)) / ((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));
            float u = ((x1-x2)*(y1-y3) - (y1-y2)*(x1-x3))/((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));

            if (t <= 1 && t >= 0 || (u < 1 && u > 0))
            {
                finalCoords = new Vector2(x1 + t*(x2 - x1), y1 + t*(y2 - y1));
                break;
            }
        }
        */
    }
}