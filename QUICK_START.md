# Casino Royale - Quick Start

## 🚀 Run Multiplayer Game (3 Commands)

### Terminal 1 - Relay Server:
```powershell
cd "C:\Users\harry\Personal Projects\CasinoRoyaleLiteNetRelay\CasinoRoyaleRelayServer"
dotnet run
```

### Terminal 2 - Instance 1:
```powershell
cd "C:\Users\harry\Personal Projects\CasinoRoyaleServer"
.\RunInstance1.bat
```
→ Click "HOST GAME"  
→ Note lobby code (top-left, large yellow text)

### Terminal 3 - Instance 2:
```powershell
.\RunInstance2.bat
```
→ Type lobby code exactly  
→ Press Enter

## 📂 Log Files

- **Instance1/Logs/debug.log** - First player's logs
- **Instance2/Logs/debug.log** - Second player's logs
- Relay server prints to console

## ✅ Success Looks Like

**Relay Server:**
```
[LOBBY] Created lobby ABC123
[LOBBY] Client joined lobby ABC123
[RELAY] Forwarding XX bytes from client to host
[RELAY] Forwarding YY bytes from host to client
```

**Instance1 (Host):**
```
🎮 LOBBY CODE: ABC123
[HOST] Peer connected via relay
[HOST] Player HH joined via relay
```

**Instance2 (Client):**
```
[CLIENT] Joined lobby: ABC123
[CLIENT] Sending JoinPacket to server...
Join process completed successfully!
```

## 🔧 Rebuild Everything
```powershell
.\BuildAll.bat
```

Creates fresh Instance1 and Instance2 folders.
