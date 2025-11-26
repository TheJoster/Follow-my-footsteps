using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Entities;

namespace FollowMyFootsteps.AI
{
    /// <summary>
    /// Perception system for NPC vision, target detection, memory, and threat assessment.
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
        
        [Header("Threat Assessment")]
        [Tooltip("Weight for damage received (higher = prioritize attackers)")]
        [SerializeField] private float damageReceivedWeight = 2.0f;
        
        [Tooltip("Weight for attack potential (higher = prioritize dangerous enemies)")]
        [SerializeField] private float attackPotentialWeight = 1.0f;
        
        [Tooltip("Weight for proximity (higher = prioritize closer enemies)")]
        [SerializeField] private float proximityWeight = 0.5f;
        
        [Tooltip("How long threat memory lasts (seconds)")]
        [SerializeField] private float threatMemoryDuration = 10f;
        
        [Header("Faction System")]
        [Tooltip("Reference to global faction settings (optional - if null, uses NPCType-based logic)")]
        [SerializeField] private FactionSettings factionSettings;
        
        [Header("Debug")]
        [Tooltip("Enable debug visualization")]
        [SerializeField] private bool showDebugGizmos = true;

        [Header("Allied Protection")]
        [Tooltip("Enable responding to allied distress calls")]
        [SerializeField] private bool respondToDistressCalls = true;
        
        [Tooltip("Range to hear distress calls from allies (in hex cells). Set to 0 to use vision range.")]
        [SerializeField] private int hearingRange = 10;
        
        [Tooltip("Priority bonus for protecting weak allies (0-100)")]
        [SerializeField] private float protectWeakBonus = 50f;

        // Current visible targets
        private List<GameObject> visibleTargets = new List<GameObject>();
        
        // Primary target (usually player or closest enemy)
        private GameObject primaryTarget;
        
        // Last known position of primary target
        private HexCoord lastKnownPosition;
        private float timeSinceLastSeen;
        private bool hasLastKnownPosition;
        
        // Threat tracking
        private Dictionary<GameObject, ThreatInfo> threatTable = new Dictionary<GameObject, ThreatInfo>();
        
        // Allied protection tracking
        private GameObject currentAllyToProtect;
        private GameObject currentAllyAttacker;
        
        /// <summary>
        /// Tracks threat information for a specific attacker
        /// </summary>
        private class ThreatInfo
        {
            public int TotalDamageReceived;
            public int AttackDamage;
            public float LastAttackTime;
            public int AttackCount;
            public bool HasAttackedUs; // True if this entity has actually attacked us
            public bool IsAttackingAlly; // True if attacking one of our allies
            public float AllyProtectionPriority; // Priority for protecting the ally (higher = protect first)
            
            public ThreatInfo(int damage, int attackerDamage, bool hasAttacked = true)
            {
                TotalDamageReceived = damage;
                AttackDamage = attackerDamage;
                LastAttackTime = Time.time;
                AttackCount = 1;
                HasAttackedUs = hasAttacked;
                IsAttackingAlly = false;
                AllyProtectionPriority = 0f;
            }
            
            public void AddDamage(int damage, int attackerDamage)
            {
                TotalDamageReceived += damage;
                AttackDamage = Mathf.Max(AttackDamage, attackerDamage);
                LastAttackTime = Time.time;
                AttackCount++;
                HasAttackedUs = true; // They've definitely attacked us now
            }
        }
        
        // Reference to our NPC controller for faction checks
        private NPCController myNpcController;
        
        // Scan timer
        private float scanTimer;
        
        // Cached components
        private HexGrid hexGrid;
        private HexCoord currentPosition;

        /// <summary>
        /// Get the ally this NPC is currently trying to protect
        /// </summary>
        public GameObject AllyToProtect => currentAllyToProtect;
        
        /// <summary>
        /// Get the attacker of our ally
        /// </summary>
        public GameObject AllyAttacker => currentAllyAttacker;

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
            myNpcController = GetComponent<NPCController>();
            scanTimer = 0f;
        }
        
        private void OnEnable()
        {
            // Subscribe to distress calls
            if (FactionAlertManager.Instance != null && respondToDistressCalls)
            {
                FactionAlertManager.Instance.OnDistressCall += OnAllyDistressCall;
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from distress calls
            if (FactionAlertManager.Instance != null)
            {
                FactionAlertManager.Instance.OnDistressCall -= OnAllyDistressCall;
            }
        }

        private void Update()
        {
            // Periodic target scanning
            scanTimer += Time.deltaTime;
            
            if (scanTimer >= scanInterval)
            {
                ScanForTargets();
                CheckForAlliesInDistress();
                CleanupOldThreats();
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
        /// Called when any ally broadcasts a distress call.
        /// This is an immediate event-based response - the periodic CheckForAlliesInDistress
        /// handles the ongoing decision of which distress call to respond to.
        /// </summary>
        private void OnAllyDistressCall(FactionAlertManager.DistressCall distressCall)
        {
            if (!respondToDistressCalls) return;
            if (myNpcController == null || myNpcController.Definition == null) return;
            
            Faction myFaction = GetMyFaction();
            
            // Check if victim is an ally
            bool isAlly = false;
            if (factionSettings != null)
            {
                isAlly = factionSettings.IsFriendly(myFaction, distressCall.VictimFaction) ||
                         myFaction == distressCall.VictimFaction;
            }
            else
            {
                isAlly = myFaction == distressCall.VictimFaction;
            }
            
            if (!isAlly) return;
            
            // Don't respond to our own distress
            if (distressCall.Victim == gameObject) return;
            
            // Check distance - use hearing range (or fall back to vision range if not set)
            int effectiveHearingRange = hearingRange > 0 ? hearingRange : visionRange;
            float maxHearingDistance = effectiveHearingRange * HexMetrics.outerRadius * 2f;
            float distance = Vector3.Distance(transform.position, distressCall.Position);
            if (distance > maxHearingDistance) return;
            
            // Register the attacker as a threat (attacking our ally)
            RegisterAllyAttacker(distressCall.Attacker, distressCall.Victim, 
                                distressCall.DamageReceived, distressCall.VictimThreatLevel);
            
            // Determine if we can see or just hear the distress
            float visionRangeWorld = visionRange * HexMetrics.outerRadius * 2f;
            bool canSeeDistress = distance <= visionRangeWorld;
            
            string hearingType = canSeeDistress ? "saw" : "heard";
            Debug.Log($"[PerceptionComponent] {gameObject.name} {hearingType} distress call from {distressCall.Victim.name}! " +
                     $"Attacker: {distressCall.Attacker.name}, Sound Level: {distressCall.SoundLevel:F1}, " +
                     $"Victim Health: {distressCall.VictimHealthPercent:P0}");
        }
        }
        
        /// <summary>
        /// Periodically check for allies in distress and update protection targets.
        /// Uses two-tier system:
        /// - Vision range: Can assess priority (protect weakest ally)
        /// - Hearing range: Responds to loudest call (lowest health = most desperate)
        /// </summary>
        private void CheckForAlliesInDistress()
        {
            if (!respondToDistressCalls) return;
            if (FactionAlertManager.Instance == null) return;
            if (myNpcController == null || myNpcController.Definition == null) return;
            
            Faction myFaction = GetMyFaction();
            
            // Calculate ranges in world units
            float visionRangeWorld = visionRange * HexMetrics.outerRadius * 2f;
            int effectiveHearingRange = hearingRange > 0 ? hearingRange : visionRange;
            float hearingRangeWorld = effectiveHearingRange * HexMetrics.outerRadius * 2f;
            
            FactionAlertManager.DistressCall distressCall = null;
            
            // First, check vision range - we can see who needs help most (assess priority)
            distressCall = FactionAlertManager.Instance.GetHighestPriorityDistressCall(
                myFaction, transform.position, factionSettings, visionRangeWorld, gameObject);
            
            // If nothing in vision range, check hearing range - respond to loudest call
            if (distressCall == null && hearingRangeWorld > visionRangeWorld)
            {
                distressCall = FactionAlertManager.Instance.GetLoudestDistressCall(
                    myFaction, transform.position, factionSettings, hearingRangeWorld, gameObject);
            }
            
            if (distressCall != null)
            {
                currentAllyToProtect = distressCall.Victim;
                currentAllyAttacker = distressCall.Attacker;
                
                // Make sure attacker is registered as a threat
                RegisterAllyAttacker(distressCall.Attacker, distressCall.Victim,
                                    distressCall.DamageReceived, distressCall.VictimThreatLevel);
            }
            else
            {
                currentAllyToProtect = null;
                currentAllyAttacker = null;
            }
        }
        
        /// <summary>
        /// Register an attacker who is targeting one of our allies
        /// </summary>
        public void RegisterAllyAttacker(GameObject attacker, GameObject ally, int damage, float allyThreatLevel)
        {
            if (attacker == null || ally == null) return;
            if (attacker == gameObject) return; // Don't register self
            
            // Add attacker to visible targets
            if (!visibleTargets.Contains(attacker))
            {
                visibleTargets.Add(attacker);
            }
            
            // Create or update threat entry
            if (threatTable.TryGetValue(attacker, out ThreatInfo existingThreat))
            {
                existingThreat.IsAttackingAlly = true;
                existingThreat.AllyProtectionPriority = Mathf.Max(existingThreat.AllyProtectionPriority, 
                                                                   100f - allyThreatLevel); // Lower threat = higher priority
            }
            else
            {
                int attackerDamage = GetAttackerDamage(attacker);
                var threat = new ThreatInfo(0, attackerDamage, false) // They haven't attacked US, but our ally
                {
                    IsAttackingAlly = true,
                    AllyProtectionPriority = 100f - allyThreatLevel // Lower threat level = higher protection priority
                };
                threatTable[attacker] = threat;
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
            
            // Get current hex position from NPCController or transform
            NPCController npcController = GetComponent<NPCController>();
            if (npcController != null && npcController.RuntimeData != null)
            {
                currentPosition = npcController.RuntimeData.Position;
            }
            else
            {
                // Fallback: convert world position to hex
                currentPosition = HexMetrics.WorldToHex(transform.position);
            }
            
            // Get cells in vision range
            List<HexCell> cellsInRange = hexGrid.GetCellsInRange(currentPosition, visionRange);
            
            foreach (HexCell cell in cellsInRange)
            {
                // Cell is already valid if returned by GetCellsInRange
                HexCoord coord = cell.Coordinates;
                
                // Get entities at this cell using Physics2D
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
                        Debug.Log($"[PerceptionComponent] {gameObject.name} detected {target.name} at {coord}");
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
                    Debug.Log($"[PerceptionComponent] {gameObject.name} locked onto primary target: {primaryTarget.name}");
                }
            }
            else
            {
                Debug.Log($"[PerceptionComponent] {gameObject.name} found no targets in range {visionRange}");
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
        /// Set target layers for detection
        /// </summary>
        public void SetTargetLayers(LayerMask layers)
        {
            targetLayers = layers;
            Debug.Log($"[PerceptionComponent] {gameObject.name} targetLayers set to: {LayerMaskToString(targetLayers)}");
        }
        
        /// <summary>
        /// Helper to convert LayerMask to readable string
        /// </summary>
        private string LayerMaskToString(LayerMask mask)
        {
            string result = "";
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    result += LayerMask.LayerToName(i) + ", ";
                }
            }
            return string.IsNullOrEmpty(result) ? "None" : result.TrimEnd(',', ' ');
        }

        /// <summary>
        /// Get current vision range
        /// </summary>
        public int GetVisionRange()
        {
            return visionRange;
        }

        /// <summary>
        /// Get current hearing range (for distress calls)
        /// </summary>
        public int GetHearingRange()
        {
            return hearingRange > 0 ? hearingRange : visionRange;
        }

        /// <summary>
        /// Set the hearing range for distress calls
        /// </summary>
        public void SetHearingRange(int range)
        {
            hearingRange = Mathf.Max(0, range);
        }
        
        /// <summary>
        /// Register damage received from an attacker for threat assessment
        /// </summary>
        public void RegisterThreat(GameObject attacker, int damageReceived)
        {
            if (attacker == null) return;
            
            // Get attacker's attack damage for threat calculation
            int attackerDamage = GetAttackerDamage(attacker);
            
            if (threatTable.TryGetValue(attacker, out ThreatInfo existingThreat))
            {
                existingThreat.AddDamage(damageReceived, attackerDamage);
                Debug.Log($"[PerceptionComponent] {gameObject.name} updated threat from {attacker.name}: " +
                         $"Total damage received: {existingThreat.TotalDamageReceived}, Attacks: {existingThreat.AttackCount}");
            }
            else
            {
                // Mark that this entity has attacked us (damage > 0 means actual attack)
                threatTable[attacker] = new ThreatInfo(damageReceived, attackerDamage, hasAttacked: damageReceived > 0);
                Debug.Log($"[PerceptionComponent] {gameObject.name} registered new threat: {attacker.name} " +
                         $"(damage: {damageReceived}, attacker potential: {attackerDamage})");
            }
            
            // Add to visible targets if not there
            if (!visibleTargets.Contains(attacker))
            {
                visibleTargets.Add(attacker);
            }
        }
        
        /// <summary>
        /// Check if a target has actually attacked us (not just detected)
        /// </summary>
        public bool HasAttackedUs(GameObject target)
        {
            if (target == null) return false;
            return threatTable.TryGetValue(target, out ThreatInfo info) && info.HasAttackedUs;
        }
        
        /// <summary>
        /// Get our NPC type (Friendly, Neutral, Hostile)
        /// </summary>
        private NPCType GetMyNPCType()
        {
            if (myNpcController != null && myNpcController.Definition != null)
            {
                return myNpcController.Definition.Type;
            }
            return NPCType.Neutral; // Default to neutral if unknown
        }
        
        /// <summary>
        /// Get the NPC type of a target
        /// </summary>
        private NPCType? GetTargetNPCType(GameObject target)
        {
            if (target == null) return null;
            
            var targetNpc = target.GetComponent<NPCController>();
            if (targetNpc != null && targetNpc.Definition != null)
            {
                return targetNpc.Definition.Type;
            }
            
            // Player is considered "Friendly" to Friendly NPCs, "Enemy" to Hostile NPCs
            var playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                return null; // Special handling for player
            }
            
            return null;
        }
        
        /// <summary>
        /// Get the faction of this NPC
        /// </summary>
        private Faction GetMyFaction()
        {
            if (myNpcController != null && myNpcController.Definition != null)
            {
                return myNpcController.Definition.Faction;
            }
            return Faction.None;
        }
        
        /// <summary>
        /// Get the faction of a target
        /// </summary>
        private Faction GetTargetFaction(GameObject target)
        {
            if (target == null) return Faction.None;
            
            var targetNpc = target.GetComponent<NPCController>();
            if (targetNpc != null && targetNpc.Definition != null)
            {
                return targetNpc.Definition.Faction;
            }
            
            // Player is part of Player faction
            var playerController = target.GetComponent<PlayerController>();
            if (playerController != null)
            {
                return Faction.Player;
            }
            
            return Faction.None;
        }
        
        /// <summary>
        /// Check if a target is a valid enemy based on faction rules
        /// Rules (with FactionSettings):
        /// - Uses FactionSettings relationship matrix to determine hostility
        /// - Always allows retaliation against attackers
        /// - Also considers attackers of our allies as valid enemies
        /// 
        /// Rules (without FactionSettings - fallback to NPCType):
        /// - Hostile NPCs attack everyone (Player, Friendly, Neutral)
        /// - Friendly/Neutral NPCs only attack:
        ///   - Hostile NPCs
        ///   - Anyone who has attacked them first
        ///   - Anyone attacking their allies
        /// </summary>
        public bool IsValidEnemy(GameObject target)
        {
            if (target == null || target == gameObject) return false;
            
            // Has this target attacked us? If so, they're always a valid enemy (retaliation)
            if (HasAttackedUs(target))
            {
                Debug.Log($"[PerceptionComponent] {gameObject.name}: {target.name} is valid enemy (attacked us)");
                return true;
            }
            
            // Is this target attacking one of our allies? (ally protection)
            if (IsAttackingAlly(target))
            {
                Debug.Log($"[PerceptionComponent] {gameObject.name}: {target.name} is valid enemy (attacking ally)");
                return true;
            }
            
            // Try faction-based logic first (if FactionSettings configured)
            if (factionSettings != null)
            {
                return IsValidEnemyByFaction(target);
            }
            
            // Fallback to NPCType-based logic
            return IsValidEnemyByNPCType(target);
        }
        
        /// <summary>
        /// Check if a target is currently attacking one of our allies
        /// </summary>
        public bool IsAttackingAlly(GameObject target)
        {
            if (target == null) return false;
            
            if (threatTable.TryGetValue(target, out ThreatInfo threat))
            {
                return threat.IsAttackingAlly;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check hostility using the FactionSettings relationship matrix
        /// </summary>
        private bool IsValidEnemyByFaction(GameObject target)
        {
            Faction myFaction = GetMyFaction();
            Faction targetFaction = GetTargetFaction(target);
            
            // If either has no faction, fall back to NPCType logic
            if (myFaction == Faction.None || targetFaction == Faction.None)
            {
                return IsValidEnemyByNPCType(target);
            }
            
            // Check faction relationship
            bool isEnemy = factionSettings.IsEnemy(myFaction, targetFaction);
            
            if (isEnemy)
            {
                Debug.Log($"[PerceptionComponent] {gameObject.name} ({myFaction}): " +
                         $"{target.name} ({targetFaction}) is enemy by faction");
            }
            
            return isEnemy;
        }
        
        /// <summary>
        /// Check hostility using NPCType (fallback when no FactionSettings)
        /// </summary>
        private bool IsValidEnemyByNPCType(GameObject target)
        {
            NPCType myType = GetMyNPCType();
            NPCType? targetType = GetTargetNPCType(target);
            bool isPlayer = target.GetComponent<PlayerController>() != null;
            
            // Hostile NPCs attack everyone except other Hostiles
            if (myType == NPCType.Hostile)
            {
                // Don't attack other hostile NPCs (same faction)
                if (targetType == NPCType.Hostile)
                {
                    return false;
                }
                return true; // Attack player, friendly, neutral
            }
            
            // Friendly NPCs only attack Hostile NPCs (or anyone who attacked them - checked above)
            if (myType == NPCType.Friendly)
            {
                if (targetType == NPCType.Hostile)
                {
                    return true;
                }
                // Don't attack player or other friendly/neutral unless they attacked first
                return false;
            }
            
            // Neutral NPCs only attack if attacked first (checked above) or if target is Hostile
            if (myType == NPCType.Neutral)
            {
                if (targetType == NPCType.Hostile)
                {
                    return true;
                }
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the attack damage of a potential attacker
        /// </summary>
        private int GetAttackerDamage(GameObject attacker)
        {
            // Check if it's an NPC
            var npcController = attacker.GetComponent<NPCController>();
            if (npcController != null && npcController.Definition != null)
            {
                return npcController.Definition.AttackDamage;
            }
            
            // Check if it's the player
            var playerController = attacker.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Use player's base attack damage (could be from PlayerDefinition)
                return 15; // Default player damage
            }
            
            return 10; // Default fallback
        }
        
        /// <summary>
        /// Calculate threat score for a target
        /// Returns 0 if target is not a valid enemy (faction rules)
        /// Includes ally protection priority for targets attacking weak allies
        /// </summary>
        public float GetThreatScore(GameObject target)
        {
            if (target == null) return 0f;
            
            // Check faction rules - if not a valid enemy, no threat
            if (!IsValidEnemy(target))
            {
                return 0f;
            }
            
            float score = 0f;
            
            // Factor 1: Damage received from this target (highest priority)
            if (threatTable.TryGetValue(target, out ThreatInfo threat))
            {
                score += threat.TotalDamageReceived * damageReceivedWeight;
                score += threat.AttackDamage * attackPotentialWeight;
                
                // Bonus for entities that have actually attacked us
                if (threat.HasAttackedUs)
                {
                    score += 50f; // Significant bonus for actual attackers
                }
                
                // Bonus for entities attacking our allies (protect the weak!)
                if (threat.IsAttackingAlly)
                {
                    // AllyProtectionPriority is higher for weaker allies
                    score += threat.AllyProtectionPriority * (protectWeakBonus / 100f) * 50f;
                    score += 30f; // Base bonus for attacking allies
                }
            }
            else
            {
                // No threat history, use potential damage only
                int potentialDamage = GetAttackerDamage(target);
                score += potentialDamage * attackPotentialWeight * 0.5f; // Lower weight for potential threats
            }
            
            // Factor 2: Proximity (closer = more threatening)
            int distance = GetDistanceToTarget(target);
            if (distance > 0)
            {
                // Inverse distance: closer targets get higher score
                score += (visionRange / (float)distance) * proximityWeight * 10f;
            }
            
            return score;
        }
        
        /// <summary>
        /// Get the most threatening VALID target based on threat assessment and faction rules
        /// </summary>
        public GameObject GetMostThreateningTarget()
        {
            GameObject mostThreatening = null;
            float highestThreat = 0f;
            
            // Consider all visible targets and remembered threats
            var allPotentialTargets = new HashSet<GameObject>(visibleTargets);
            foreach (var threat in threatTable.Keys)
            {
                if (threat != null)
                    allPotentialTargets.Add(threat);
            }
            
            foreach (var target in allPotentialTargets)
            {
                if (target == null || target == gameObject) continue;
                
                // Skip targets that aren't valid enemies (faction rules)
                if (!IsValidEnemy(target)) continue;
                
                float threatScore = GetThreatScore(target);
                
                if (threatScore > highestThreat)
                {
                    highestThreat = threatScore;
                    mostThreatening = target;
                }
            }
            
            if (mostThreatening != null)
            {
                Debug.Log($"[PerceptionComponent] {gameObject.name} most threatening target: " +
                         $"{mostThreatening.name} (threat score: {highestThreat:F1})");
            }
            
            return mostThreatening;
        }
        
        /// <summary>
        /// Get the closest VALID enemy target (respects faction rules)
        /// </summary>
        public GameObject GetClosestValidEnemy()
        {
            GameObject closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var target in visibleTargets)
            {
                if (target == null || target == gameObject) continue;
                if (!IsValidEnemy(target)) continue;
                
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closest = target;
                    closestDistance = distance;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Clean up threats that have expired
        /// </summary>
        private void CleanupOldThreats()
        {
            var expiredThreats = new List<GameObject>();
            
            foreach (var kvp in threatTable)
            {
                if (kvp.Key == null || Time.time - kvp.Value.LastAttackTime > threatMemoryDuration)
                {
                    expiredThreats.Add(kvp.Key);
                }
            }
            
            foreach (var expired in expiredThreats)
            {
                threatTable.Remove(expired);
                if (expired != null)
                {
                    Debug.Log($"[PerceptionComponent] {gameObject.name} forgot threat: {expired.name}");
                }
            }
        }
        
        /// <summary>
        /// Force-set a specific target (used for retaliation when attacked)
        /// Also registers the threat for priority assessment
        /// </summary>
        public void SetRetaliationTarget(GameObject attacker, int damageReceived = 0)
        {
            if (attacker == null) return;
            
            // Register the threat
            if (damageReceived > 0)
            {
                RegisterThreat(attacker, damageReceived);
            }
            
            // Add attacker to visible targets if not already there
            if (!visibleTargets.Contains(attacker))
            {
                visibleTargets.Add(attacker);
            }
            
            // Set primary target to most threatening (could be this attacker or another)
            primaryTarget = GetMostThreateningTarget() ?? attacker;
            
            // Update last known position
            if (hexGrid != null && primaryTarget != null)
            {
                lastKnownPosition = HexMetrics.WorldToHex(primaryTarget.transform.position);
                hasLastKnownPosition = true;
                timeSinceLastSeen = 0f;
            }
            
            Debug.Log($"[PerceptionComponent] {gameObject.name} set retaliation target: {primaryTarget?.name ?? "none"}");
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
