using System.Collections.Generic;
using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Perception system for NPC vision, target detection, and memory.
    /// Phase 4.3 - Perception System
    /// </summary>
    public class PerceptionComponent : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("Vision range in hex cells")]
        [SerializeField] private int visionRange = 5;
        
        [Tooltip("Vision angle in degrees (360 = full circle, 90 = narrow cone)")]
        [SerializeField] private float visionAngle = 360f;
        
        [Tooltip("Enable vision cone (false = circular vision)")]
        [SerializeField] private bool useVisionCone = false;
        
        [Header("Target Detection")]
        [Tooltip("Layers to detect as targets")]
        [SerializeField] private LayerMask targetLayers;
        
        [Tooltip("How often to scan for targets (in seconds)")]
        [SerializeField] private float scanInterval = 0.5f;
        
        [Header("Memory System")]
        [Tooltip("How long to remember last known position (seconds)")]
        [SerializeField] private float memoryDuration = 5f;
        
        [Tooltip("Enable debug visualization")]
        [SerializeField] private bool showDebugGizmos = true;

        // Current visible targets
        private List<GameObject> visibleTargets = new List<GameObject>();
        
        // Primary target (usually player or closest enemy)
        private GameObject primaryTarget;
        
        // Last known position of primary target
        private HexCoord lastKnownPosition;
        private float timeSinceLastSeen;
        private bool hasLastKnownPosition;
        
        // Scan timer
        private float scanTimer;
        
        // Cached components
        private HexGrid hexGrid;
        private HexCoord currentPosition;

        /// <summary>
        /// Get the primary target
        /// </summary>
        public GameObject PrimaryTarget => primaryTarget;
        
        /// <summary>
        /// Check if currently has a visible target
        /// </summary>
        public bool HasVisibleTarget => primaryTarget != null;
        
        /// <summary>
        /// Get last known position of target
        /// </summary>
        public HexCoord LastKnownPosition => lastKnownPosition;
        
        /// <summary>
        /// Check if has a memory of target position
        /// </summary>
        public bool HasMemory => hasLastKnownPosition && timeSinceLastSeen < memoryDuration;
        
        /// <summary>
        /// Get all visible targets
        /// </summary>
        public IReadOnlyList<GameObject> VisibleTargets => visibleTargets;

        private void Awake()
        {
            hexGrid = Object.FindFirstObjectByType<HexGrid>();
            scanTimer = 0f;
        }

        private void Update()
        {
            // Periodic target scanning
            scanTimer += Time.deltaTime;
            
            if (scanTimer >= scanInterval)
            {
                ScanForTargets();
                scanTimer = 0f;
            }
            
            // Update memory timer
            if (hasLastKnownPosition)
            {
                timeSinceLastSeen += Time.deltaTime;
                
                if (timeSinceLastSeen >= memoryDuration)
                {
                    ForgetTarget();
                }
            }
        }

        /// <summary>
        /// Scan for visible targets within perception range
        /// </summary>
        public void ScanForTargets()
        {
            visibleTargets.Clear();
            primaryTarget = null;
            
            if (hexGrid == null)
            {
                Debug.LogWarning("[PerceptionComponent] HexGrid not found. Cannot scan.");
                return;
            }
            
            // Get current hex position
            // TODO: Get from NPCController or transform position
            currentPosition = new HexCoord(0, 0); // Placeholder
            
            // Get cells in vision range
            List<HexCell> cellsInRange = hexGrid.GetCellsInRange(currentPosition, visionRange);
            
            foreach (HexCell cell in cellsInRange)
            {
                // Cell is already valid if returned by GetCellsInRange
                HexCoord coord = cell.Coordinates;
                
                // TODO: Get entities at this cell
                // For now, use Physics2D for efficiency
                Vector3 worldPos = HexMetrics.GetWorldPosition(coord);
                Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.5f, targetLayers);
                
                foreach (Collider2D collider in colliders)
                {
                    GameObject target = collider.gameObject;
                    
                    // Skip self
                    if (target == gameObject)
                        continue;
                    
                    // Check vision cone if enabled
                    if (useVisionCone && !IsInVisionCone(target.transform.position))
                        continue;
                    
                    // Add to visible targets
                    if (!visibleTargets.Contains(target))
                    {
                        visibleTargets.Add(target);
                    }
                }
            }
            
            // Select primary target (closest for now)
            if (visibleTargets.Count > 0)
            {
                primaryTarget = GetClosestTarget();
                
                // Update last known position
                if (primaryTarget != null && hexGrid != null)
                {
                    lastKnownPosition = HexMetrics.WorldToHex(primaryTarget.transform.position);
                    hasLastKnownPosition = true;
                    timeSinceLastSeen = 0f;
                }
            }
        }

        /// <summary>
        /// Check if a world position is within the vision cone
        /// </summary>
        private bool IsInVisionCone(Vector3 targetPosition)
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 forward = transform.up; // Assuming 2D top-down, forward is up
            
            float angle = Vector3.Angle(forward, directionToTarget);
            
            return angle <= visionAngle / 2f;
        }

        /// <summary>
        /// Get the closest visible target
        /// </summary>
        public GameObject GetClosestTarget()
        {
            if (visibleTargets.Count == 0)
                return null;
            
            GameObject closest = visibleTargets[0];
            float closestDistance = Vector3.Distance(transform.position, closest.transform.position);
            
            for (int i = 1; i < visibleTargets.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, visibleTargets[i].transform.position);
                if (distance < closestDistance)
                {
                    closest = visibleTargets[i];
                    closestDistance = distance;
                }
            }
            
            return closest;
        }

        /// <summary>
        /// Forget the last known target position
        /// </summary>
        public void ForgetTarget()
        {
            hasLastKnownPosition = false;
            timeSinceLastSeen = 0f;
            lastKnownPosition = new HexCoord(0, 0);
        }

        /// <summary>
        /// Set vision range dynamically
        /// </summary>
        public void SetVisionRange(int range)
        {
            visionRange = Mathf.Max(1, range);
        }

        /// <summary>
        /// Get current vision range
        /// </summary>
        public int GetVisionRange()
        {
            return visionRange;
        }

        /// <summary>
        /// Check if a specific target is visible
        /// </summary>
        public bool CanSee(GameObject target)
        {
            return visibleTargets.Contains(target);
        }

        /// <summary>
        /// Calculate distance to target in hex cells
        /// </summary>
        public int GetDistanceToTarget(GameObject target)
        {
            if (hexGrid == null || target == null)
                return int.MaxValue;
            
            HexCoord targetCoord = HexMetrics.WorldToHex(target.transform.position);
            return HexMetrics.Distance(currentPosition, targetCoord);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos)
                return;
            
            // Draw vision range circle
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            
            if (hexGrid != null)
            {
                // Vision range visualization would go here
                // For now, simple circle
                UnityEngine.Debug.DrawLine(transform.position, transform.position + Vector3.up * visionRange, Color.yellow);
            }
            
            // Draw line to primary target
            if (primaryTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, primaryTarget.transform.position);
            }
            
            // Draw line to last known position
            if (hasLastKnownPosition && hexGrid != null)
            {
                Vector3 lastKnownWorldPos = HexMetrics.GetWorldPosition(lastKnownPosition);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawLine(transform.position, lastKnownWorldPos);
                Gizmos.DrawWireSphere(lastKnownWorldPos, 0.5f);
            }
        }
    }
}
