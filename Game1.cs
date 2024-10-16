using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace CSharpFirstPerson;
public class Game1 : Game
{
    Properties _gameProperties;
    MainCamera _mainCamera = MainCamera.Instance;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    CasinoMachineFactory casinoMachineFactory;
    Player player1;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _gameProperties = new Properties("app.properties");
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // for loading non-graphic related content
    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Vector2 playerMaxVelocity = new Vector2(float.Parse(_gameProperties.get("playerMaxVelocityX")), 
                                                float.Parse(_gameProperties.get("playerMaxVelocityY")));
        player1 = new Player(Content.Load<Texture2D>("ball"), playerMaxVelocity);
        casinoMachineFactory = new CasinoMachineFactory(Content.Load<Texture2D>("CasinoMachine1"));
        casinoMachineFactory.SpawnCasinoMachine();
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState ks = Keyboard.GetState();
        
        player1.Move(ks, deltaTime);
        _mainCamera.MoveToFollowPlayer(player1);

        base.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        _spriteBatch.Begin();
        foreach (CasinoMachine machine in casinoMachineFactory.GetCasinoMachines())
        {
            _spriteBatch.Draw(machine.GetTex(), _mainCamera.TransformToView(machine.GetCoords()), Color.White);
        }
        _spriteBatch.Draw(player1.GetTex(), _mainCamera.TransformToView(player1.GetCoords()), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
