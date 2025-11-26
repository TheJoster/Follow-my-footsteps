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
            // Check if NPC should attack
            if (entity is NPCController npcController)
            {
                // Force a perception scan at the start of turn
                var perception = npcController.GetComponent<PerceptionComponent>();
                if (perception != null)
                {
                    perception.ScanForTargets();
                }
                
                // Check if this NPC is hostile
                if (npcController.Definition != null && 
                    npcController.Definition.Type == Entities.NPCType.Hostile)
                {
                    // Try to detect player
                    if (perception != null)
                    {
                        var target = perception.GetClosestTarget();
                        if (target != null)
                        {
                            Debug.Log($"[IdleState] {npcController.EntityName} detected {target.name}, transitioning to AttackState");
                            npcController.GetStateMachine()?.ChangeState("AttackState");
                            return;
                        }
                    }
                }
            }
            
            // Turn-based idle: Do nothing, just count turns
            turnsIdled++;
            
            Debug.Log($"[IdleState] NPC idling (turn {turnsIdled})");
        }
        
        public void OnExit(object entity)
        {
            Debug.Log($"[IdleState] Exited after {turnsIdled} turns");
        }
    }
}
