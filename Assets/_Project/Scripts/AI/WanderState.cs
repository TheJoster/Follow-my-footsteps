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
        private HexCoord targetPosition;
        private bool hasRequestedPath;
        private bool isMovingToTarget;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="home">Center point for wandering</param>
        /// <param name="radius">Maximum distance from home to wander (in hex cells)</param>
        public WanderState(HexCoord home, int radius = 5)
        {
            homePosition = home;
            wanderRadius = radius;
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
            if (npc == null)
            {
                Debug.LogError("[WanderState] OnUpdate called with null or invalid NPC!");
                return;
            }
            
            Debug.Log($"[WanderState] OnUpdate for {npc.EntityName}. AP: {npc.ActionPoints}");
            
            var movement = npc.GetMovementController();
            if (movement == null)
            {
                Debug.LogError($"[WanderState] {npc.EntityName} has no MovementController!");
                return;
            }
            
            // Reset movement flags if movement is complete
            if (isMovingToTarget && !movement.IsMoving)
            {
                Debug.Log($"[WanderState] {npc.EntityName} finished moving to {targetPosition}");
                isMovingToTarget = false;
                hasRequestedPath = false;
            }
            
            HexCoord currentPos = npc.RuntimeData.Position;
            
            // Check if we need a new target
            if (targetPosition == default || currentPos == targetPosition)
            {
                PickNewWanderTarget(npc);
            }
            
            // Move toward target if we have AP and aren't already moving
            if (npc.ActionPoints > 0 && !isMovingToTarget && !hasRequestedPath)
            {
                // Check if MovementController is already moving
                if (movement.IsMoving)
                {
                    Debug.Log($"[WanderState] {npc.EntityName} is already moving via MovementController");
                    isMovingToTarget = true; // Sync our flag
                    return;
                }
                
                var pathfinding = PathfindingManager.Instance;
                var hexGrid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
                
                if (pathfinding == null)
                {
                    Debug.LogError($"[WanderState] PathfindingManager.Instance is null!");
                    return;
                }
                
                if (hexGrid == null)
                {
                    Debug.LogError($"[WanderState] Could not find HexGrid in scene!");
                    return;
                }
                
                Debug.Log($"[WanderState] {npc.EntityName} requesting path from {currentPos} to {targetPosition}");
                hasRequestedPath = true;
                pathfinding.RequestPath(hexGrid, currentPos, targetPosition,
                    (path) => OnPathReceived(path, npc, movement));
            }
            else
            {
                Debug.Log($"[WanderState] {npc.EntityName} skipping movement (isMoving: {isMovingToTarget}, hasRequested: {hasRequestedPath}, AP: {npc.ActionPoints})");
            }
        }
        
        private void OnPathReceived(List<HexCoord> path, NPCController npc, MovementController movement)
        {
            hasRequestedPath = false;
            
            if (path != null && path.Count > 0)
            {
                // Truncate path to available AP (partial path following)
                int maxSteps = Mathf.Min(path.Count, npc.ActionPoints);
                List<HexCoord> truncatedPath = path.GetRange(0, maxSteps);
                
                // Consume AP only for the truncated path
                if (!npc.ConsumeActionPoints(maxSteps))
                {
                    Debug.LogWarning($"[WanderState] {npc.EntityName} failed to consume {maxSteps} AP");
                    isMovingToTarget = false;
                    return;
                }
                
                Debug.Log($"[WanderState] {npc.EntityName} moving {maxSteps}/{path.Count} steps toward {targetPosition}. {npc.ActionPoints} AP remaining");
                isMovingToTarget = true;
                bool success = movement.FollowPath(truncatedPath);
                Debug.Log($"[WanderState] FollowPath returned: {success}");
                
                // Keep same target if we didn't reach it (will continue next turn)
                if (maxSteps < path.Count)
                {
                    Debug.Log($"[WanderState] {npc.EntityName} making partial progress, {path.Count - maxSteps} steps remaining");
                }
            }
            else
            {
                // No path found - pick new target next turn
                Debug.LogWarning($"[WanderState] {npc.EntityName} cannot find path to {targetPosition}");
                isMovingToTarget = false;
                hasRequestedPath = false;
            }
        }
        
        public void OnExit(object entity)
        {
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
