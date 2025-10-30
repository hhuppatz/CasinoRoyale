namespace CasinoRoyale.Classes.GameStates.Interfaces;

/// Interface for managing game state transitions
/// States can request transitions without knowing about CasinoRoyaleGame
public interface IGameStateManager
{
    void TransitionToState(GameState newState);

    void ReturnToMenu();
}