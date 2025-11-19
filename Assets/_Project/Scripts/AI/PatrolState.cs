using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

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
                Debug.LogWarning("[PatrolState] No waypoints defined. Cannot patrol.");
                return;
            }
            
            Debug.Log($"[PatrolState] Entered. Patrolling {waypoints.Count} waypoints in {mode} mode");
        }
        
        public void OnUpdate(object entity)
        {
            if (waypoints.Count == 0) return;
            
            // TODO: Integrate with MovementController
            // 1. Check if NPC reached current waypoint
            // 2. If yes, advance to next waypoint
            // 3. Request pathfinding to next waypoint
            // 4. If enemy detected via perception, transition to ChaseState
            
            // For now, just log the current target waypoint
            HexCoord targetWaypoint = waypoints[currentWaypointIndex];
            
            // Placeholder: Simulate waypoint reached
            // In real implementation, NPCController will check position vs waypoint
            // AdvanceToNextWaypoint();
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
