using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Flee state: NPC runs away from threats when health is low
    /// Used by low-health NPCs or cowardly types
    /// Phase 4.2 - Defensive NPC States
    /// </summary>
    public class FleeState : IState
    {
        public string StateName => "Flee";
        
        private object threatSource;
        private float safeDistance;
        private float healthThreshold;
        private HexCoord fleeTarget;
        private bool hasReachedSafety;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minSafeDistance">Minimum distance to flee to (in hex cells)</param>
        /// <param name="healthPercent">Health percentage that triggers fleeing (0.0 to 1.0)</param>
        public FleeState(float minSafeDistance = 8f, float healthPercent = 0.3f)
        {
            safeDistance = minSafeDistance;
            healthThreshold = healthPercent;
            hasReachedSafety = false;
        }
        
        public void OnEnter(object entity)
        {
            hasReachedSafety = false;
            Debug.Log("[FleeState] Entered. Fleeing from threat!");
            
            // TODO: Calculate flee direction (away from threat)
            // Vector2 fleeDirection = GetFleeDirection(entity, threatSource);
            // fleeTarget = CalculateFleeDestination(entity, fleeDirection, safeDistance);
            
            // TODO: Request pathfinding to flee target
            // PathfindingManager.FindPath(currentPosition, fleeTarget);
        }
        
        public void OnUpdate(object entity)
        {
            if (hasReachedSafety)
            {
                // TODO: Check if still in danger
                // float distanceToThreat = CalculateDistance(entity, threatSource);
                // if (distanceToThreat >= safeDistance)
                // {
                //     Debug.Log("[FleeState] Reached safety. Transitioning to Idle.");
                //     // NPCController will transition to Idle or Heal state
                // }
                return;
            }
            
            // TODO: Continue fleeing
            // 1. Check if reached flee target
            // 2. If blocked, recalculate flee path
            // 3. If cornered, may transition to desperate attack
            
            // Placeholder: Simulate reaching safety
            // if (HasReachedTarget(entity, fleeTarget))
            // {
            //     hasReachedSafety = true;
            //     Debug.Log("[FleeState] Reached flee target.");
            // }
        }
        
        public void OnExit(object entity)
        {
            threatSource = null;
            hasReachedSafety = false;
            Debug.Log("[FleeState] Exited");
        }
        
        /// <summary>
        /// Set the threat to flee from
        /// </summary>
        public void SetThreatSource(object threat)
        {
            threatSource = threat;
        }
        
        /// <summary>
        /// Check if health is low enough to trigger fleeing
        /// </summary>
        /// <param name="currentHealth">Current health value</param>
        /// <param name="maxHealth">Maximum health value</param>
        /// <returns>True if should flee</returns>
        public bool ShouldFlee(int currentHealth, int maxHealth)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            return healthPercent <= healthThreshold;
        }
        
        /// <summary>
        /// Get the health threshold for fleeing
        /// </summary>
        public float GetHealthThreshold()
        {
            return healthThreshold;
        }
    }
}
