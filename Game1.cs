using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CSharpFirstPerson;


public class Game1 : Game
{
    Properties _gameProperties;
    MainCamera _mainCamera = MainCamera.Instance;
    private GraphicsDeviceManager _graphics;
    private KeyboardState lastKeyboardState;
    private SpriteBatch _spriteBatch;
    private CasinoMachineFactory casinoMachineFactory;
    private Player player1;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Window.AllowUserResizing = false;

        _gameProperties = new Properties("app.properties");
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // for loading non-graphic related content
    protected override void Initialize()
    {
        lastKeyboardState = Keyboard.GetState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // game world initialisation


        // player initialisation
        player1 = new Player(Content.Load<Texture2D>(_gameProperties.get("player.image")),
                            new Vector2(0, 0),
                            new Vector2(float.Parse(_gameProperties.get("playerMaxVelocity.x")), float.Parse(_gameProperties.get("playerMaxVelocity.y"))),
                            new Vector2(float.Parse(_gameProperties.get("playerMaxAcceleration.x")), float.Parse(_gameProperties.get("playerMaxAcceleration.y"))));

        
        // casino machine generation
        casinoMachineFactory = new CasinoMachineFactory(Content.Load<Texture2D>(_gameProperties.get("casinoMachine.image.1")));
        casinoMachineFactory.SpawnCasinoMachine();

        // main camera initialisation
        _mainCamera.InitMainCamera(Window, player1);
    }

    protected override void Update(GameTime gameTime)
    {
        // delta time and current keyboard state
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState ks = Keyboard.GetState();

        // player logic
        player1.Move(ks, deltaTime);

        // camera logic
        _mainCamera.MoveToFollowPlayer(player1);
        Console.WriteLine(_mainCamera.GetCoords());
        Console.WriteLine("p " + player1.GetCoords());
        
        // game window logic (move)
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // if f11 was just pressed, go fullscreen borderless
        if (Keyboard.GetState().IsKeyDown(Keys.F11) && !lastKeyboardState.IsKeyDown(Keys.F11))
        {
            Resolution.ToggleBorderless(Window, _graphics);
        }

        base.Update(gameTime);

        // saving for next update call
        lastKeyboardState = Keyboard.GetState();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkMagenta);

        Vector2 ratio = Resolution.ratio;
        _mainCamera.ApplyRatio(ratio);

        // drawing sprites
        _spriteBatch.Begin();

        foreach (CasinoMachine machine in casinoMachineFactory.GetCasinoMachines())
        {
            _spriteBatch.Draw(machine.GetTex(),
                            _mainCamera.TransformToView(machine.GetCoords()),
                            null,
                            Color.White,
                            0.0f,
                            new Vector2(machine.GetTex().Bounds.Width/2, machine.GetTex().Bounds.Height/2),
                            ratio,
                            0,
                            0);
        }
        _spriteBatch.Draw(player1.GetTex(),
                        _mainCamera.TransformToView(player1.GetCoords()),
                        null,
                        Color.White,
                        0.0f,
                        new Vector2(player1.GetTex().Bounds.Width/2, player1.GetTex().Bounds.Height/2),
                        ratio,
                        0,
                        0);
        _spriteBatch.End();

        Console.WriteLine("mcp " + _mainCamera.TransformToView(player1.GetCoords()));

        base.Draw(gameTime);
    }

}
