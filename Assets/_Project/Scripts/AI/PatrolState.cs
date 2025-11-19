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
                Debug.LogWarning("[PatrolState] No waypoints defined. Falling back to Idle state.");
                
                // Try to switch to Idle state if no waypoints
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
            if (npc == null) return;
            
            var movement = npc.GetMovementController();
            if (movement == null) return;
            
            HexCoord currentPos = npc.RuntimeData.Position;
            HexCoord targetWaypoint = waypoints[currentWaypointIndex];
            
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
                // Check if we have enough AP to move
                if (npc.ActionPoints > 0)
                {
                    var pathfinding = PathfindingManager.Instance;
                    if (pathfinding != null)
                    {
                        hasRequestedPath = true;
                        pathfinding.RequestPath(HexGrid.Instance, currentPos, targetWaypoint, 
                            (path) => OnPathReceived(path, npc, movement));
                    }
                }
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
                isMovingToWaypoint = true;
                movement.FollowPath(path);
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
