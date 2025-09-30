# Casino Royale - Project Structure

## 📁 Source Code (Clean - Ready for Git)

```
CasinoRoyaleServer/
├── GameStates/              # Game state management
│   ├── GameState.cs        # Base game state (common logic)
│   ├── MenuGameState.cs    # Main menu
│   ├── HostGameState.cs    # Hosting a game
│   └── ClientGameState.cs  # Joining a game
│
├── GameObjects/             # Game entities
│   ├── PlayableCharacter.cs
│   ├── Platform.cs
│   ├── CasinoMachine.cs
│   └── Interfaces/
│
├── Players/                 # Networking
│   ├── Common/
│   │   ├── Networking/
│   │   │   ├── LiteNetRelayManager.cs  # NEW: LiteNetLib relay
│   │   │   ├── IPeer.cs                 # Peer abstraction
│   │   │   ├── Packets.cs              # Packet definitions
│   │   │   └── SerializingExtensions.cs
│   │   └── PlayerIDs.cs
│   └── Host/
│       └── NetworkPlayer.cs
│
├── Utils/                   # Utilities
│   ├── Logger.cs
│   ├── Properties.cs
│   ├── Resolution.cs
│   └── Vector2Extensions.cs
│
├── Content/                 # Game assets
│   ├── ball.png
│   ├── CasinoFloor1.png
│   ├── CasinoMachine1.png
│   ├── Arial.spritefont
│   └── Content.mgcb
│
├── CasinoRoyaleGame.cs     # Main game class
├── Program.cs              # Entry point
├── app.properties          # Configuration
│
├── Scripts/                # Build & run scripts
│   ├── BuildAll.bat        # Build everything
│   ├── TestMultiplayer.bat # Automated testing
│   ├── RunInstance1.bat    # Run first instance
│   ├── RunInstance2.bat    # Run second instance
│   └── KillAll.bat         # Kill all processes
│
└── Build/                  # Build outputs (gitignored)
    ├── bin/
    ├── obj/
    ├── Instance1/          # First game instance
    └── Instance2/          # Second game instance
```

## 🗑️ Removed Files (Obsolete)

- ❌ `Players/Common/Networking/RelayClient.cs` - Old WebSocket relay
- ❌ `Players/Common/Networking/RelayManager.cs` - Old WebSocket relay
- ❌ `Players/Host/Host.cs` - Replaced by HostGameState
- ❌ `Players/Client/Client.cs` - Replaced by ClientGameState
- ❌ `Menu/GameMenu.cs` - Replaced by MenuGameState

## 🚀 Running the Game

### Local Multiplayer Testing:

```bash
# Terminal 1: Relay Server
cd ..\CasinoRoyaleLiteNetRelay\CasinoRoyaleRelayServer
dotnet run

# Terminal 2: Instance 1
.\Scripts\RunInstance1.bat

# Terminal 3: Instance 2
.\Scripts\RunInstance2.bat
```

### Or use the automated script:

```bash
.\Scripts\TestMultiplayer.bat
```

## 🔧 Development

### Build:
```bash
.\Scripts\BuildAll.bat
```

Outputs to `Build/` folder:
- `Build/bin/` - Compiled binaries
- `Build/obj/` - Intermediate files
- `Build/Instance1/` - First game instance (ready to run)
- `Build/Instance2/` - Second game instance (ready to run)

### Clean Build:
```bash
Remove-Item -Recurse -Force Build
.\Scripts\BuildAll.bat
```

## 📝 Log Files

- `Build/Instance1/Logs/debug.log` - First instance logs
- `Build/Instance2/Logs/debug.log` - Second instance logs

## 🌐 Deployment

See `../CasinoRoyaleLiteNetRelay/DEPLOYMENT_AWS.md` for relay server deployment guide.

## 🎮 Game Architecture

**State-Based Design:**
```
CasinoRoyaleGame
    ├── MenuGameState (Choose Host/Join)
    ├── HostGameState (Running as host)
    └── ClientGameState (Running as client)
```

**Networking:**
```
Player 1 ←→ LiteNetRelayManager ←→ Relay Server ←→ LiteNetRelayManager ←→ Player 2
           (LiteNetLib UDP)                       (LiteNetLib UDP)
```

**Benefits:**
- Pure UDP (fast, efficient)
- No WebSocket complexity
- Native LiteNetLib integration
- Easy to deploy
