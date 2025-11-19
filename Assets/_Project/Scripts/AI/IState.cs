namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Interface for all AI states in the hierarchical state machine.
    /// Phase 4.2 - Hierarchical State Machine
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Unique identifier for this state
        /// </summary>
        string StateName { get; }
        
        /// <summary>
        /// Called when entering this state
        /// </summary>
        /// <param name="entity">The entity entering this state</param>
        void OnEnter(object entity);
        
        /// <summary>
        /// Called every frame/update while in this state
        /// </summary>
        /// <param name="entity">The entity in this state</param>
        void OnUpdate(object entity);
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        /// <param name="entity">The entity exiting this state</param>
        void OnExit(object entity);
    }
}
