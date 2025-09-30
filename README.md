# 🎰 Casino Royale - Multiplayer Platform Game

A MonoGame-based multiplayer platformer with relay server networking.

## 🌐 Relay Server Connection

The game uses a relay server to enable multiplayer connections between players behind NAT/firewalls.

### Configuration

Relay server connection details are stored in **`app.properties`**:

```properties
relay.server.address=54.252.174.8
relay.server.port=9051
```

### How It Works

```
Host Client ←→ Relay Server (AWS EC2) ←→ Guest Client
            (UDP 9051)              (UDP 9051)
```

1. **Host** connects to relay server and receives a 6-character lobby code
2. **Client** enters the lobby code and connects through the same relay server
3. **Relay server** forwards all game packets between host and client

### Changing the Relay Server

To use a different relay server:

1. Edit `app.properties` in the project root
2. Update `relay.server.address` and `relay.server.port`
3. Rebuild: `.\Scripts\BuildAll.bat`

The `app.properties` file is automatically copied to build outputs during compilation.

## 🚀 Quick Start

### Test Locally

```bash
.\Scripts\TestMultiplayer.bat
```

This starts two game instances:
1. In **Instance 1**: Click **"HOST GAME"**
2. Note the **lobby code** in top-left corner
3. In **Instance 2**: Type the **lobby code** and press Enter

Both instances will connect to the relay server configured in `app.properties`.

### Build

```bash
.\Scripts\BuildAll.bat
```

## 📁 Key Files

- **`app.properties`** - Relay server configuration (project root)
- **`GameStates/HostGameState.cs`** - Reads relay config and hosts game
- **`GameStates/ClientGameState.cs`** - Reads relay config and joins game
- **`Players/Common/Networking/LiteNetRelayManager.cs`** - Handles relay connection

## 🎮 Controls

- **WASD** / **Arrow Keys**: Move
- **Space**: Jump
- **ESC**: Exit

## 📖 Documentation

For detailed guides, see the `docs/` folder:
- Building and deployment
- AWS relay server setup
- Project structure
- Testing guide

---

**Happy Gaming! 🎮**