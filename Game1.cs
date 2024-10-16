using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace CSharpFirstPerson;
public class Game1 : Game
{
    Texture2D casinoMachineTexture;
    CasinoMachineFactory casinoMachineFactory;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    // for loading non-graphic related content
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        casinoMachineTexture = Content.Load<Texture2D>("CasinoMachine1");
        casinoMachineFactory = new CasinoMachineFactory(casinoMachineTexture);
        casinoMachineFactory.SpawnCasinoMachine();
        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        _spriteBatch.Begin();
        foreach (CasinoMachine casinoMachine in casinoMachineFactory.GetCasinoMachines())
        {
            _spriteBatch.Draw(casinoMachine.GetTex(), casinoMachine.GetCoords(), Color.White);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
