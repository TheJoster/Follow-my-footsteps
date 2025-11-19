using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;
using System.Collections.Generic;

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
        private bool hasRequestedPath;
        private bool isMovingToTarget;
        
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
            NPCController npc = entity as NPCController;
            PickNewWanderTarget(npc);
        }
        
        public void OnUpdate(object entity)
        {
            NPCController npc = entity as NPCController;
            if (npc == null) return;
            
            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                
                if (waitTimer >= waitDuration)
                {
                    // Wait complete, pick new target
                    isWaiting = false;
                    PickNewWanderTarget(npc);
                }
            }
            else
            {
                var movement = npc.GetMovementController();
                if (movement == null) return;
                
                HexCoord currentPos = npc.RuntimeData.Position;
                
                // Check if reached target
                if (currentPos == targetPosition)
                {
                    StartWaiting();
                    isMovingToTarget = false;
                    hasRequestedPath = false;
                }
                // Request path if not already moving
                else if (!isMovingToTarget && !hasRequestedPath && npc.ActionPoints > 0)
                {
                    var pathfinding = PathfindingManager.Instance;
                    if (pathfinding != null)
                    {
                        hasRequestedPath = true;
                        pathfinding.RequestPath(HexGrid.Instance, currentPos, targetPosition,
                            (path) => OnPathReceived(path, npc, movement));
                    }
                }
            }
        }
        
        private void OnPathReceived(List<HexCoord> path, NPCController npc, MovementController movement)
        {
            hasRequestedPath = false;
            
            if (path != null && path.Count > 0)
            {
                isMovingToTarget = true;
                movement.FollowPath(path);
            }
            else
            {
                // No path found - pick new target
                Debug.LogWarning($"[WanderState] {npc.EntityName} cannot find path to {targetPosition}");
                PickNewWanderTarget(npc);
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
        private void PickNewWanderTarget(NPCController npc = null)
        {
            // Generate random offset within radius
            int offsetQ = Random.Range(-wanderRadius, wanderRadius + 1);
            int offsetR = Random.Range(-wanderRadius, wanderRadius + 1);
            
            // Ensure within circular radius (Manhattan distance for hex grid)
            if (Mathf.Abs(offsetQ) + Mathf.Abs(offsetR) > wanderRadius)
            {
                // Retry if outside radius
                PickNewWanderTarget(npc);
                return;
            }
            
            targetPosition = new HexCoord(
                homePosition.q + offsetQ,
                homePosition.r + offsetR
            );
            
            hasRequestedPath = false;
            isMovingToTarget = false;
            
            Debug.Log($"[WanderState] New wander target: {targetPosition}");
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
