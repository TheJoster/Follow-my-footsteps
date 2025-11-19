namespace FollowMyFootsteps.Core
{
    /// <summary>
    /// Interface for entities that participate in the turn-based simulation.
    /// Implement this for player, NPCs, and any other entities that take turns.
    /// </summary>
    public interface ITurnEntity
    {
        /// <summary>
        /// Display name of the entity (for UI and debugging).
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// Whether the entity is active and can take turns.
        /// Inactive entities (e.g., dead, stunned) are skipped during turn processing.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Current action points available this turn.
        /// Used to limit actions per turn.
        /// </summary>
        int ActionPoints { get; }

        /// <summary>
        /// Maximum action points per turn.
        /// Action points refresh to this value at turn start.
        /// </summary>
        int MaxActionPoints { get; }

        /// <summary>
        /// Called when it's this entity's turn to act.
        /// Entity should perform its turn logic here (AI decisions, etc.).
        /// For player, this is when input is enabled.
        /// </summary>
        void TakeTurn();

        /// <summary>
        /// Called at the start of this entity's turn.
        /// Use for setup, refreshing action points, applying start-of-turn effects, etc.
        /// </summary>
        void OnTurnStart();

        /// <summary>
        /// Called at the end of this entity's turn.
        /// Use for cleanup, applying end-of-turn effects, damage over time, etc.
        /// </summary>
        void OnTurnEnd();

        /// <summary>
        /// Consume action points for an action.
        /// Returns true if there were enough points, false otherwise.
        /// </summary>
        /// <param name="amount">Number of action points to consume</param>
        /// <returns>True if action points were consumed, false if not enough points</returns>
        bool ConsumeActionPoints(int amount);
    }
}
