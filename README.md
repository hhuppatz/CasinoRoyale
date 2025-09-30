# 🎰 Casino Royale - Multiplayer Platform Game

A MonoGame-based multiplayer platformer with physics-based gameplay and LiteNetLib networking.

## ✨ Features

- **🎮 Multiplayer Support**: Host and join games with simple 6-character lobby codes
- **🌐 NAT Traversal**: LiteNetLib relay server bypasses NAT/firewall issues
- **⚡ Real-time Gameplay**: UDP-based networking for low-latency synchronization
- **🎯 State-Based Architecture**: Clean separation of menu, host, and client logic
- **🔧 Easy Testing**: Automated scripts for local multiplayer testing

## 🚀 Quick Start

### 1. Build the Game

```bash
.\Scripts\BuildAll.bat
```

### 2. Test Multiplayer Locally

```bash
.\Scripts\TestMultiplayer.bat
```

This will automatically start:
1. LiteNetLib Relay Server
2. Game Instance 1 (Host)
3. Game Instance 2 (Client)

**Then:**
1. In **Instance 1**, click **"HOST GAME"**
2. Note the **6-character lobby code** displayed in the top-left corner
3. In **Instance 2**, type the **exact lobby code** and press Enter
4. Both players should now see each other and can move around!

## 🎯 Manual Testing

If you prefer manual control:

```bash
# Terminal 1: Start Relay Server
cd ..\CasinoRoyaleLiteNetRelay\CasinoRoyaleRelayServer
dotnet run

# Terminal 2: Start First Player
.\Scripts\RunInstance1.bat

# Terminal 3: Start Second Player
.\Scripts\RunInstance2.bat
```

## 📁 Project Structure

```
CasinoRoyaleServer/
├── GameStates/          # Menu, Host, Client game states
├── GameObjects/         # Player, Platform, CasinoMachine entities
├── Players/Common/      # Networking (LiteNetRelayManager, Packets)
├── Utils/               # Logger, Properties, Extensions
├── Content/             # Game assets (sprites, fonts)
├── Scripts/             # Build & run batch files
└── Build/               # Build outputs (gitignored)
```

See [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for detailed breakdown.

## 🛠️ Development

### Requirements
- .NET 8.0 SDK
- MonoGame 3.8.2
- Windows (batch scripts) - Linux/Mac users can adapt scripts

### Build Commands

```bash
# Build everything
.\Scripts\BuildAll.bat

# Run single instance
.\Scripts\RunInstance1.bat

# Kill all running instances
.\Scripts\KillAll.bat

# Clean build
Remove-Item -Recurse -Force Build
.\Scripts\BuildAll.bat
```

### Logs

Game logs are saved in:
- `Build/Instance1/Logs/debug.log`
- `Build/Instance2/Logs/debug.log`

## 🌐 Remote Deployment

The relay server can be hosted on AWS, DigitalOcean, Google Cloud, etc.

See [CasinoRoyaleLiteNetRelay/DEPLOYMENT_AWS.md](../CasinoRoyaleLiteNetRelay/DEPLOYMENT_AWS.md) for detailed deployment guide.

**Quick Steps:**
1. Build relay server for Linux: `dotnet publish -c Release -r linux-x64 --self-contained`
2. Deploy to EC2/VPS with UDP port 9051 open
3. Update `app.properties` with your server's IP:
   ```properties
   relay.server.address=YOUR_SERVER_IP
   relay.server.port=9051
   ```

## 🎮 Controls

- **WASD** / **Arrow Keys**: Move
- **Space**: Jump
- **ESC**: Exit game

## 🏗️ Architecture

**Networking:**
```
Host ←→ LiteNetRelayManager ←→ Relay Server ←→ LiteNetRelayManager ←→ Client
      (UDP)                                            (UDP)
```

**Game States:**
```
MenuGameState → HostGameState / ClientGameState
                      ↓
                  GameState (common logic)
```

**Benefits:**
- Pure UDP (fast, efficient)
- No WebSocket complexity
- Easy to scale
- Works behind NAT

## 📝 Testing Guide

For comprehensive testing instructions, see [TESTING_GUIDE.md](TESTING_GUIDE.md).

## 🐛 Troubleshooting

**Build fails:**
- Clean build: `Remove-Item -Recurse -Force Build`
- Check .NET 8.0 SDK is installed: `dotnet --version`

**Can't connect:**
- Check relay server is running
- Check `app.properties` has correct relay server address
- Check firewall allows UDP port 9051

**Game crashes:**
- Check logs in `Build/Instance1/Logs/debug.log`
- Ensure Content files are copied (ball.xnb, Arial.xnb, etc.)

## 📜 License

This project is for educational purposes. MonoGame and LiteNetLib are used under their respective licenses.

## 🙏 Credits

- **MonoGame** - Cross-platform game framework
- **LiteNetLib** - Lightweight UDP networking library
- Built with ❤️ and lots of debugging

---

**Happy Gaming! 🎮**
