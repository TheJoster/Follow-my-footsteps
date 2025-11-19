using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Wander state: NPC randomly moves to nearby locations within a defined area
    /// Used by Friendly NPCs for ambient movement
    /// Phase 4.2 - Friendly NPC States
    /// </summary>
    public class WanderState : IState
    {
        public string StateName => "Wander";
        
        private HexCoord homePosition;
        private int wanderRadius;
        private float minWaitTime;
        private float maxWaitTime;
        private float waitTimer;
        private float waitDuration;
        private bool isWaiting;
        private HexCoord targetPosition;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="home">Center point for wandering</param>
        /// <param name="radius">Maximum distance from home to wander (in hex cells)</param>
        /// <param name="minWait">Minimum wait time at each location</param>
        /// <param name="maxWait">Maximum wait time at each location</param>
        public WanderState(HexCoord home, int radius = 5, float minWait = 2f, float maxWait = 5f)
        {
            homePosition = home;
            wanderRadius = radius;
            minWaitTime = minWait;
            maxWaitTime = maxWait;
            isWaiting = false;
        }
        
        public void OnEnter(object entity)
        {
            Debug.Log($"[WanderState] Entered. Wandering within {wanderRadius} cells of {homePosition}");
            PickNewWanderTarget();
        }
        
        public void OnUpdate(object entity)
        {
            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitDuration)
                {
                    // Wait complete, pick new target
                    isWaiting = false;
                    PickNewWanderTarget();
                }
            }
            else
            {
                // TODO: Integrate with MovementController
                // 1. Check if NPC reached targetPosition
                // 2. If yes, start waiting
                // 3. If not, continue moving toward target
                // 4. If blocked, pick new target
                
                // Placeholder: Simulate arrival
                // if (HasReachedTarget(entity, targetPosition))
                // {
                //     StartWaiting();
                // }
            }
        }
        
        public void OnExit(object entity)
        {
            isWaiting = false;
            waitTimer = 0f;
            Debug.Log("[WanderState] Exited");
        }
        
        /// <summary>
        /// Pick a random position within wander radius
        /// </summary>
        private void PickNewWanderTarget()
        {
            // Generate random offset within radius
            int offsetQ = Random.Range(-wanderRadius, wanderRadius + 1);
            int offsetR = Random.Range(-wanderRadius, wanderRadius + 1);
            
            // Ensure within circular radius (Manhattan distance for hex grid)
            if (Mathf.Abs(offsetQ) + Mathf.Abs(offsetR) > wanderRadius)
            {
                // Retry if outside radius
                PickNewWanderTarget();
                return;
            }
            
            targetPosition = new HexCoord(
                homePosition.q + offsetQ,
                homePosition.r + offsetR
            );
            
            Debug.Log($"[WanderState] New wander target: {targetPosition}");
            
            // TODO: Request pathfinding to targetPosition
            // PathfindingManager.FindPath(currentPosition, targetPosition);
        }
        
        /// <summary>
        /// Start waiting at current location
        /// </summary>
        private void StartWaiting()
        {
            isWaiting = true;
            waitTimer = 0f;
            waitDuration = Random.Range(minWaitTime, maxWaitTime);
            Debug.Log($"[WanderState] Arrived. Waiting for {waitDuration:F1}s");
        }
        
        /// <summary>
        /// Update home position (useful if NPC's territory changes)
        /// </summary>
        public void SetHomePosition(HexCoord newHome)
        {
            homePosition = newHome;
        }
        
        /// <summary>
        /// Get current wander target
        /// </summary>
        public HexCoord GetTargetPosition()
        {
            return targetPosition;
        }
    }
}
