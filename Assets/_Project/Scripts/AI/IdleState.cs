using UnityEngine;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Idle state: NPC does nothing, waiting for events or state transitions
    /// Phase 4.2 - Initial States
    /// Phase 4.7 - Turn-Based Integration
    /// 
    /// Turn-based behavior:
    /// - Consumes no action points (NPC just waits)
    /// - Can transition to other states based on NPCController logic (perception, etc.)
    /// - Purely reactive state
    /// </summary>
    public class IdleState : IState
    {
        public string StateName => "Idle";
        
        private int turnsIdled;
        
        public void OnEnter(object entity)
        {
            turnsIdled = 0;
            Debug.Log($"[IdleState] Entered idle state");
        }
        
        public void OnUpdate(object entity)
        {
            // Turn-based idle: Do nothing, just count turns
            // NPCController will handle state transitions based on perception/events
            turnsIdled++;
            
            Debug.Log($"[IdleState] NPC idling (turn {turnsIdled})");
            
            // Optional: Could auto-transition after certain turns
            // For now, NPCController handles all transitions via perception system
        }
        
        public void OnExit(object entity)
        {
            Debug.Log($"[IdleState] Exited after {turnsIdled} turns");
        }
    }
}
