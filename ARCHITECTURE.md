# Casino Royale - Game State Architecture

## Overview
This project uses a **State Pattern** to manage different game modes (Menu, Host, Client). The architecture was refactored to follow better design principles.

## Architecture Improvements

### Before (Problems)
```csharp
// States had to know about CasinoRoyaleGame
if (Game is CasinoRoyaleGame casinoGame)
{
    casinoGame.StartHost();  // Tight coupling!
}
```

**Issues:**
- ❌ Tight coupling between states and CasinoRoyaleGame
- ❌ States had to cast the Game object
- ❌ Adding new states required modifying CasinoRoyaleGame
- ❌ Violated Open/Closed Principle

### After (Solution)

#### 1. **IGameStateManager Interface**
```csharp
public interface IGameStateManager
{
    void TransitionToState(GameState newState);
    void ReturnToMenu();
}
```

States can now request transitions without knowing about the concrete game class.

#### 2. **Updated GameState Base Class**
```csharp
public abstract class GameState(Game game, IGameStateManager stateManager)
{
    protected IGameStateManager StateManager { get; }
    // ...
}
```

All states now have access to the state manager through their base class.

#### 3. **CasinoRoyaleGame Implements Interface**
```csharp
public class CasinoRoyaleGame : Game, IGameStateManager
{
    public void TransitionToState(GameState newState) { /*...*/ }
    public void ReturnToMenu() { /*...*/ }
}
```

#### 4. **Clean State Transitions**
```csharp
// In MenuGameState - no casting needed!
private void StartHost()
{
    var hostState = new HostGameState(Game, StateManager);
    StateManager.TransitionToState(hostState);
}
```

## Benefits

### ✅ Loose Coupling
States only depend on `IGameStateManager`, not the concrete `CasinoRoyaleGame` class.

### ✅ Open/Closed Principle
Adding new states doesn't require modifying `CasinoRoyaleGame` - just create a new state class.

### ✅ Testability
States can be tested with a mock `IGameStateManager`.

### ✅ Single Responsibility
- `CasinoRoyaleGame`: Manages game loop and state lifecycle
- `GameState` subclasses: Handle specific game mode logic
- `IGameStateManager`: Defines state transition contract

### ✅ Scalability
Easy to add new states:
```csharp
public class PauseGameState : GameState
{
    public PauseGameState(Game game, IGameStateManager stateManager) 
        : base(game, stateManager) { }
    
    public void Resume()
    {
        // Return to previous state or menu
        StateManager.ReturnToMenu();
    }
}
```

## State Diagram

```
┌──────────────┐
│ MenuGameState│
└──────┬───────┘
       │
       ├─────────────────┐
       │                 │
       ▼                 ▼
┌─────────────┐   ┌──────────────┐
│HostGameState│   │ClientGameState│
└─────────────┘   └──────────────┘
       │                 │
       └────────┬────────┘
                ▼
         [ReturnToMenu]
```

## Current Game States

### 1. **MenuGameState**
- Main menu UI
- Lobby code input
- Transitions to Host or Client

### 2. **HostGameState**
- Creates lobby
- Manages network players
- Authoritative game simulation
- Implements `INetEventListener`

### 3. **ClientGameState**
- Joins lobby
- Receives state updates from host
- Client-side prediction/interpolation
- Implements `INetEventListener`

## Future Enhancements

Potential additional states:
- `PauseGameState` - Pause menu during gameplay
- `SettingsGameState` - Game settings configuration
- `LobbyBrowserGameState` - Browse available lobbies
- `LoadingGameState` - Show loading screen during transitions
- `GameOverGameState` - End game statistics

## Usage Examples

### Transitioning from Menu to Host
```csharp
// In MenuGameState.StartHost()
var hostState = new HostGameState(Game, StateManager);
StateManager.TransitionToState(hostState);
```

### Returning to Menu from Game
```csharp
// In HostGameState or ClientGameState (press ESC)
if (keyboardState.IsKeyDown(Keys.Escape))
{
    StateManager.ReturnToMenu();
}
```

### Adding New State
```csharp
public class SettingsGameState : GameState
{
    public SettingsGameState(Game game, IGameStateManager stateManager) 
        : base(game, stateManager) { }
    
    public override void Update(GameTime gameTime)
    {
        // Handle settings input
        if (backButtonPressed)
        {
            StateManager.ReturnToMenu();
        }
    }
}
```

## Key Takeaways

1. **Dependency Inversion**: States depend on `IGameStateManager` abstraction
2. **Interface Segregation**: Small, focused interface for state transitions
3. **Separation of Concerns**: Each state manages its own behavior
4. **Maintainability**: Easy to understand and extend

---

*This architecture follows SOLID principles and provides a solid foundation for future game development.*

