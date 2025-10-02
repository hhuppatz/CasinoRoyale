namespace CasinoRoyale.Classes.GameStates
{
    /// <summary>
    /// Interface for managing game state transitions
    /// States can request transitions without knowing about CasinoRoyaleGame
    /// </summary>
    public interface IGameStateManager
    {
        /// <summary>
        /// Transitions to a new game state
        /// </summary>
        void TransitionToState(GameState newState);
        
        /// <summary>
        /// Returns to the main menu
        /// </summary>
        void ReturnToMenu();
    }
}

