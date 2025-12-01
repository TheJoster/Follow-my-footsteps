using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Patrol state: NPC moves along predefined waypoints in a loop or back-and-forth pattern
    /// Phase 4.2 - Initial States
    /// </summary>
    public class PatrolState : IState
    {
        public string StateName => "Patrol";
        
        private List<HexCoord> waypoints;
        private int currentWaypointIndex;
        private bool reverseDirection;
        private PatrolMode mode;
        private bool isMovingToWaypoint;
        private bool hasRequestedPath;
        
        public enum PatrolMode
        {
            Loop,       // 0 -> 1 -> 2 -> 0 -> 1 -> 2...
            PingPong    // 0 -> 1 -> 2 -> 1 -> 0 -> 1...
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="patrolWaypoints">List of waypoint coordinates to patrol</param>
        /// <param name="patrolMode">Loop or PingPong mode</param>
        public PatrolState(List<HexCoord> patrolWaypoints, PatrolMode patrolMode = PatrolMode.Loop)
        {
            waypoints = patrolWaypoints ?? new List<HexCoord>();
            mode = patrolMode;
            currentWaypointIndex = 0;
            reverseDirection = false;
        }
        
        public void OnEnter(object entity)
        {
            if (waypoints.Count == 0)
            {
                // No waypoints is common for NPCs without defined patrol routes
                // Silently fall back to Idle - this is expected behavior
                NPCController npc = entity as NPCController;
                if (npc != null)
                {
                    npc.ChangeState("Idle");
                }
                return;
            }
            
            Debug.Log($"[PatrolState] Entered. Patrolling {waypoints.Count} waypoints in {mode} mode");
        }
        
        public void OnUpdate(object entity)
        {
            if (waypoints.Count == 0) return;
            
            NPCController npc = entity as NPCController;
            if (npc == null)
            {
                Debug.LogError("[PatrolState] OnUpdate called with null or invalid NPC!");
                return;
            }
            
            // Check if an ally is in distress (all NPC types can respond to allied distress)
            var perception = npc.GetComponent<PerceptionComponent>();
            if (perception != null)
            {
                // Check if there's an attacker targeting our allies
                var allyAttacker = perception.AllyAttacker;
                var allyToProtect = perception.AllyToProtect;
                
                if (allyAttacker != null)
                {
                    bool isValid = perception.IsValidEnemy(allyAttacker);
                    Debug.Log($"[PatrolState] {npc.EntityName} checking ally distress - Attacker: {allyAttacker.name}, " +
                             $"AllyToProtect: {(allyToProtect != null ? allyToProtect.name : "null")}, IsValidEnemy: {isValid}");
                    
                    if (isValid)
                    {
                        Debug.Log($"[PatrolState] {npc.EntityName} responding to ally distress! Transitioning to AttackState");
                        npc.GetStateMachine()?.ChangeState("AttackState");
                        return;
                    }
                }
            }
            
            // Check if NPC should attack (hostile NPCs detect player)
            if (npc.Definition != null && npc.Definition.Type == NPCType.Hostile)
            {
                if (perception != null)
                {
                    // Force scan for targets
                    perception.ScanForTargets();
                    
                    var target = perception.GetClosestTarget();
                    if (target != null)
                    {
                        Debug.Log($"[PatrolState] {npc.EntityName} detected {target.name}, transitioning to AttackState");
                        npc.GetStateMachine()?.ChangeState("AttackState");
                        return;
                    }
                }
            }
            
            Debug.Log($"[PatrolState] OnUpdate called for {npc.EntityName}. AP: {npc.ActionPoints}, Moving: {isMovingToWaypoint}, HasRequested: {hasRequestedPath}");
            
            var movement = npc.GetMovementController();
            if (movement == null)
            {
                Debug.LogError($"[PatrolState] {npc.EntityName} has no MovementController!");
                return;
            }
            
            // Reset movement flags if movement is complete
            if (isMovingToWaypoint && !movement.IsMoving)
            {
                Debug.Log($"[PatrolState] {npc.EntityName} finished moving");
                isMovingToWaypoint = false;
                hasRequestedPath = false;
            }
            
            HexCoord currentPos = npc.RuntimeData.Position;
            HexCoord targetWaypoint = waypoints[currentWaypointIndex];
            
            Debug.Log($"[PatrolState] {npc.EntityName} at {currentPos}, target waypoint: {targetWaypoint} (index {currentWaypointIndex})");
            
            // Check if we reached the current waypoint
            if (currentPos == targetWaypoint)
            {
                // Reached waypoint - advance to next
                AdvanceToNextWaypoint();
                targetWaypoint = waypoints[currentWaypointIndex];
                isMovingToWaypoint = false;
                hasRequestedPath = false;
            }
            
            // If not moving and not already at waypoint, request path
            if (!isMovingToWaypoint && !hasRequestedPath)
            {
                // Check if MovementController is already moving (important!)
                if (movement.IsMoving)
                {
                    Debug.Log($"[PatrolState] {npc.EntityName} is already moving via MovementController");
                    isMovingToWaypoint = true; // Sync our flag
                    return;
                }
                
                // Check if we have enough AP to move
                if (npc.ActionPoints > 0)
                {
                    var pathfinding = PathfindingManager.Instance;
                    var hexGrid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
                    
                    if (pathfinding == null)
                    {
                        Debug.LogError($"[PatrolState] PathfindingManager.Instance is null!");
                        return;
                    }
                    
                    if (hexGrid == null)
                    {
                        Debug.LogError($"[PatrolState] Could not find HexGrid in scene!");
                        return;
                    }
                    
                    Debug.Log($"[PatrolState] {npc.EntityName} requesting path from {currentPos} to {targetWaypoint}");
                    hasRequestedPath = true;
                    pathfinding.RequestPath(hexGrid, currentPos, targetWaypoint, 
                        (path) => OnPathReceived(path, npc, movement));
                }
                else
                {
                    Debug.LogWarning($"[PatrolState] {npc.EntityName} has no AP! Cannot move.");
                }
            }
            else
            {
                Debug.Log($"[PatrolState] {npc.EntityName} skipping path request (isMoving: {isMovingToWaypoint}, hasRequested: {hasRequestedPath})");
            }
            
            // Check for enemies via perception (future: transition to Chase)
            // var perception = npc.GetComponent<PerceptionComponent>();
            // if (perception != null && perception.HasTarget())
            // {
            //     npc.ChangeState("Chase");
            // }
        }
        
        private void OnPathReceived(List<HexCoord> path, NPCController npc, Entities.MovementController movement)
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
                    Debug.LogWarning($"[PatrolState] {npc.EntityName} failed to consume {maxSteps} AP");
                    isMovingToWaypoint = false;
                    return;
                }
                
                HexCoord targetWaypoint = waypoints[currentWaypointIndex];
                Debug.Log($"[PatrolState] {npc.EntityName} moving {maxSteps}/{path.Count} steps toward waypoint {currentWaypointIndex} at {targetWaypoint}. {npc.ActionPoints} AP remaining");
                isMovingToWaypoint = true;
                bool success = movement.FollowPath(truncatedPath);
                Debug.Log($"[PatrolState] FollowPath returned: {success}");
                
                // Keep same waypoint if we didn't reach it (will continue next turn)
                if (maxSteps < path.Count)
                {
                    Debug.Log($"[PatrolState] {npc.EntityName} making partial progress, {path.Count - maxSteps} steps remaining to waypoint");
                }
            }
            else
            {
                // No path found - advance to next waypoint or idle
                Debug.LogWarning($"[PatrolState] {npc.EntityName} cannot find path to waypoint {waypoints[currentWaypointIndex]}");
                AdvanceToNextWaypoint();
            }
        }
        
        public void OnExit(object entity)
        {
            Debug.Log("[PatrolState] Exited");
        }
        
        /// <summary>
        /// Move to next waypoint based on patrol mode
        /// </summary>
        private void AdvanceToNextWaypoint()
        {
            if (waypoints.Count <= 1) return;
            
            if (mode == PatrolMode.Loop)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }
            else // PingPong
            {
                if (reverseDirection)
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex <= 0)
                    {
                        currentWaypointIndex = 0;
                        reverseDirection = false;
                    }
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count - 1)
                    {
                        currentWaypointIndex = waypoints.Count - 1;
                        reverseDirection = true;
                    }
                }
            }
            
            Debug.Log($"[PatrolState] Advanced to waypoint {currentWaypointIndex}: {waypoints[currentWaypointIndex]}");
        }
        
        /// <summary>
        /// Get current target waypoint
        /// </summary>
        public HexCoord GetCurrentWaypoint()
        {
            if (waypoints.Count == 0) return new HexCoord(0, 0);
            return waypoints[currentWaypointIndex];
        }
        
        /// <summary>
        /// Add a waypoint to the patrol route
        /// </summary>
        public void AddWaypoint(HexCoord waypoint)
        {
            waypoints.Add(waypoint);
        }
    }
}
