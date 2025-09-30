# Casino Royale Multiplayer Testing Guide

## Quick Start - 3 Easy Steps

### Step 1: Start Relay Server
Open PowerShell window #1:
```powershell
cd "C:\Users\harry\Personal Projects\CasinoRoyaleLiteNetRelay\CasinoRoyaleRelayServer"
dotnet run
```

**Wait for:**
```
✓ Relay server started on port 9051
✓ Ready to accept connections
```

### Step 2: Start Instance 1 (First Player)
Open PowerShell window #2:
```powershell
cd "C:\Users\harry\Personal Projects\CasinoRoyaleServer"
.\RunInstance1.bat
```

**In the game window:**
- Click **"HOST GAME"** button
- Look at **top-left corner** for the lobby code (it's **LARGE, YELLOW, and CLEAR**)
- **Write down the 6-character code** (e.g., "ABC123")

**Check Instance1/Logs/debug.log**, you'll see:
```
[RELAY_MGR] Connected to relay server: 127.0.0.1:9051
[RELAY] Lobby created: ABC123
🎮 LOBBY CODE: ABC123
```

### Step 3: Start Instance 2 (Second Player)
Open PowerShell window #3:
```powershell
.\RunInstance2.bat
```

**In the game window:**
- **Carefully type the EXACT 6-character code** from Step 2
- Double-check each character!
- Press **Enter**

**Success looks like:**
```
[RELAY] Joined lobby: ABC123
[CLIENT] Connected to host via relay
[CLIENT] Sending JoinPacket to server...
Join process completed successfully - client is now fully connected!
```

## Troubleshooting

### "Lobby not found" Error
**Cause**: Typo in the lobby code  
**Solution**: Double-check each character - especially:
- **G** vs **Q** (they look similar)
- **6** vs **G**
- **2** vs **Z**

### Client Stuck in Menu
**Cause**: Not polling for messages  
**Status**: ✅ FIXED in latest build

### Connection Timeout
**Cause**: Relay server not running  
**Solution**: Make sure Step 1 completed successfully

### Port Already in Use
**Cause**: Multiple relay server instances running  
**Solution**: 
```powershell
Get-Process | Where-Object {$_.ProcessName -like "*Relay*"} | Stop-Process -Force
```

## What You Should See

### Relay Server Console:
```
[CONNECT] Connection request from: 127.0.0.1:xxxxx
[CONNECT] Peer connected: 127.0.0.1:xxxxx
[LOBBY] Created lobby ABC123 for host 127.0.0.1:xxxxx
[CONNECT] Connection request from: 127.0.0.1:yyyyy
[CONNECT] Peer connected: 127.0.0.1:yyyyy
[LOBBY] Client 127.0.0.1:yyyyy joined lobby ABC123 (1 clients)
[RELAY] Received game packet from 127.0.0.1:yyyyy: XX bytes
[RELAY] Forwarding XX bytes from client to host in lobby ABC123
[RELAY] Received game packet from 127.0.0.1:xxxxx: YY bytes
[RELAY] Forwarding YY bytes from host to 1 client(s) in lobby ABC123
```

### Host Console:
```
[RELAY_MGR] Connected to relay server: 127.0.0.1:9051
[RELAY] Lobby created: ABC123
🎮 LOBBY CODE: ABC123
[RELAY] Client joined: 0
[HOST] Peer connected: 127.0.0.1
[HOST] Received join from HH via relay
[HOST] Player 1 HH joined via relay
```

### Client Console:
```
[RELAY_MGR] Connected to relay server: 127.0.0.1:9051
[RELAY] Joined lobby: ABC123
[CLIENT] Connected to host via relay
[CLIENT] Sending JoinPacket to server...
[CLIENT] Received join acceptance from host
Join process completed successfully - client is now fully connected!
```

## Current Status

✅ LiteNetLib relay server created  
✅ Lobby code system working  
✅ Control messages (LOBBY_CREATED, CLIENT_JOINED) working  
✅ Client polling fixed  
✅ Lobby code display enlarged  
⏳ Testing game packet relay (JoinPacket → JoinAcceptPacket)  

**Next test will show if game packets are being relayed!**
