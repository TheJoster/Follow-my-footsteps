using UnityEngine;
using System;
using System.Collections.Generic;
using FollowMyFootsteps.Combat;
using FollowMyFootsteps.Grid;
using FollowMyFootsteps.Core;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Defines when distress call popups should be displayed.
    /// </summary>
    public enum DistressPopupTrigger
    {
        /// <summary>Show popups for all combat (Player attacks, NPC vs NPC)</summary>
        AllCombat,
        /// <summary>Only show popups when the Player is the attacker</summary>
        PlayerAttacksOnly,
        /// <summary>Never show distress popups (NPCs still respond internally)</summary>
        Disabled
    }

    /// <summary>
    /// Global event manager for faction-wide alerts and distress calls.
    /// Allows NPCs to respond when allied faction members are attacked.
    /// Phase 5 - Allied Protection System
    /// </summary>
    public class FactionAlertManager : MonoBehaviour
    {
        private static FactionAlertManager instance;
        private static bool isQuitting = false;
        
        /// <summary>
        /// Gets the FactionAlertManager instance, auto-creating one if it doesn't exist.
        /// Returns null during application shutdown to prevent creating objects in OnDestroy.
        /// </summary>
        public static FactionAlertManager Instance
        {
            get
            {
                // Don't create instance during shutdown
                if (isQuitting)
                    return null;
                    
                if (instance == null)
                {
                    // Try to find existing instance in scene
                    instance = FindFirstObjectByType<FactionAlertManager>();
                    
                    // Auto-create if not found
                    if (instance == null)
                    {
                        var managerObject = new GameObject("FactionAlertManager");
                        instance = managerObject.AddComponent<FactionAlertManager>();
                        DontDestroyOnLoad(managerObject);
                        Debug.Log("[FactionAlertManager] Auto-created FactionAlertManager singleton");
                    }
                }
                return instance;
            }
        }

        [Header("Faction Configuration")]
        [Tooltip("Global faction settings for relationship checks. If not set, will auto-load from Resources/FactionSettings.")]
        [SerializeField] private FactionSettings factionSettings;
        
        [Header("Settings")]
        [Tooltip("Maximum range (in world units) for distress calls to be heard")]
        [SerializeField] private float maxAlertRange = 15f;
        
        [Tooltip("How many full rounds an alert remains active (turn-based). A round = all entities take one turn.")]
        [SerializeField] private int alertDurationInRounds = 3;
        
        [Tooltip("When to show distress popup visuals above attacked entities")]
        [SerializeField] private DistressPopupTrigger popupTrigger = DistressPopupTrigger.PlayerAttacksOnly;
        
        [Header("Global Hearing Defaults")]
        [Tooltip("Default hearing range for NPCs (in hex cells). Used when NPC's HearingRange is 0.")]
        [SerializeField] [Range(1, 30)] private int defaultHearingRange = 10;
        
        [Tooltip("Default sound level range multiplier. Used when NPC's SoundLevelRangeMultiplier is 0. At 1.5, desperate cries travel 50% further.")]
        [SerializeField] [Range(1f, 2f)] private float defaultSoundLevelRangeMultiplier = 1.5f;
        
        /// <summary>
        /// Global faction settings for faction relationship checks.
        /// Used by PerceptionComponent to determine ally relationships.
        /// </summary>
        public FactionSettings FactionSettings => factionSettings;
        
        /// <summary>
        /// Global default hearing range for NPCs (used when NPC definition has 0)
        /// </summary>
        public int DefaultHearingRange => defaultHearingRange;
        
        /// <summary>
        /// Global default sound level range multiplier (used when NPC definition has 0)
        /// </summary>
        public float DefaultSoundLevelRangeMultiplier => defaultSoundLevelRangeMultiplier;

        /// <summary>
        /// Represents a distress call from an NPC being attacked
        /// </summary>
        public class DistressCall
        {
            public GameObject Victim;           // The NPC being attacked
            public GameObject Attacker;         // Who is attacking them
            public Faction VictimFaction;       // Faction of the victim
            public Vector3 Position;            // Where the attack happened
            public float Timestamp;             // When the attack happened (real time, for sound decay)
            public int CreatedOnTurn;           // Turn number when created (for expiration)
            public int DamageReceived;          // How much damage was dealt
            public float VictimThreatLevel;     // How dangerous the victim is (low = needs protection)
            public float VictimHealthPercent;   // Current health % of victim
            
            /// <summary>
            /// Sound level of the distress call (0-100).
            /// Lower health = louder cry for help.
            /// Used for hearing range decisions (NPC responds to loudest call they can hear).
            /// </summary>
            public float SoundLevel => CalculateSoundLevel();
            
            private float CalculateSoundLevel()
            {
                // Lower health = louder distress call (more desperate cries)
                // 100% health = 20 sound level (mild call)
                // 50% health = 60 sound level (urgent call)
                // 10% health = 92 sound level (desperate scream)
                // 0% health would be 100 (death cry)
                float baseSound = 20f;
                float healthFactor = (1f - VictimHealthPercent) * 80f;
                
                // Recent damage adds temporary loudness boost
                float damageBoost = Mathf.Min(DamageReceived * 0.5f, 20f);
                
                return Mathf.Clamp(baseSound + healthFactor + damageBoost, 0f, 100f);
            }
            
            /// <summary>
            /// Check if this distress call has expired based on turn count.
            /// </summary>
            /// <param name="durationInRounds">Number of rounds before expiration</param>
            /// <param name="currentTurn">Current turn number from SimulationManager</param>
            public bool IsExpired(int durationInRounds, int currentTurn)
            {
                return currentTurn - CreatedOnTurn > durationInRounds;
            }
            
            /// <summary>
            /// Legacy time-based check (kept for sound decay calculations)
            /// </summary>
            public bool IsExpiredByTime(float duration) => Time.time - Timestamp > duration;
        }

        // Active distress calls
        private List<DistressCall> activeDistressCalls = new List<DistressCall>();
        
        // Event for when a new distress call is made
        public event Action<DistressCall> OnDistressCall;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Auto-load FactionSettings if not assigned
            EnsureFactionSettingsExists();
            
            // Auto-create AlertPopupPool if it doesn't exist in the scene
            EnsureAlertPopupPoolExists();
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                isQuitting = true;
            }
        }
        
        private void OnApplicationQuit()
        {
            isQuitting = true;
        }
        
        /// <summary>
        /// Ensures FactionSettings is loaded for faction relationship checks.
        /// Tries to load from Resources/FactionSettings if not manually assigned.
        /// Creates a runtime instance with defaults if none found.
        /// </summary>
        private void EnsureFactionSettingsExists()
        {
            if (factionSettings == null)
            {
                // Try to load from Resources folder
                factionSettings = Resources.Load<FactionSettings>("FactionSettings");
                
                if (factionSettings != null)
                {
                    Debug.Log("[FactionAlertManager] Auto-loaded FactionSettings from Resources");
                }
                else
                {
                    // Create a runtime instance with default settings
                    // This allows the game to work even without a saved asset
                    factionSettings = ScriptableObject.CreateInstance<FactionSettings>();
                    factionSettings.SetupDefaultRelationships();
                    Debug.Log("[FactionAlertManager] Created runtime FactionSettings with default relationships. " +
                             "For persistent settings, create a FactionSettings asset in Resources/FactionSettings.");
                }
            }
        }
        
        /// <summary>
        /// Ensures the AlertPopupPool singleton exists for distress visualizations.
        /// Creates one automatically if not found in scene.
        /// </summary>
        private void EnsureAlertPopupPoolExists()
        {
            if (AlertPopupPool.Instance == null)
            {
                var poolObject = new GameObject("AlertPopupPool");
                poolObject.AddComponent<AlertPopupPool>();
                Debug.Log("[FactionAlertManager] Auto-created AlertPopupPool for distress visualizations");
            }
        }
        
        /// <summary>
        /// Determines if a distress popup should be shown based on settings.
        /// </summary>
        /// <param name="attacker">The attacker triggering the distress call</param>
        /// <returns>True if popup should be displayed</returns>
        private bool ShouldShowDistressPopup(GameObject attacker)
        {
            if (AlertPopupPool.Instance == null) return false;
            
            switch (popupTrigger)
            {
                case DistressPopupTrigger.AllCombat:
                    return true;
                    
                case DistressPopupTrigger.PlayerAttacksOnly:
                    return attacker.GetComponent<PlayerController>() != null;
                    
                case DistressPopupTrigger.Disabled:
                default:
                    return false;
            }
        }

        private void Update()
        {
            // Clean up expired distress calls (based on turn count, not time)
            int currentTurn = GetCurrentTurn();
            activeDistressCalls.RemoveAll(call => call.IsExpired(alertDurationInRounds, currentTurn) || 
                                                   call.Victim == null || 
                                                   call.Attacker == null);
        }
        
        /// <summary>
        /// Get current turn number from SimulationManager
        /// </summary>
        private int GetCurrentTurn()
        {
            return SimulationManager.Instance != null ? SimulationManager.Instance.CurrentTurnNumber : 0;
        }

        /// <summary>
        /// Broadcast a distress call when an entity is attacked.
        /// Called by HealthComponent or combat system when damage is received.
        /// Handles both NPCs and the Player.
        /// </summary>
        /// <param name="victim">The entity being attacked</param>
        /// <param name="attacker">The attacker</param>
        /// <param name="damage">Damage dealt</param>
        public void BroadcastDistressCall(GameObject victim, GameObject attacker, int damage)
        {
            if (victim == null || attacker == null) return;

            Faction victimFaction;
            float victimThreatLevel;
            float victimHealthPercent;

            // Check if victim is an NPC
            var victimNpc = victim.GetComponent<NPCController>();
            if (victimNpc != null && victimNpc.Definition != null)
            {
                victimFaction = victimNpc.Definition.Faction;
                victimThreatLevel = CalculateThreatLevel(victimNpc.Definition);
                victimHealthPercent = victimNpc.RuntimeData != null 
                    ? (float)victimNpc.RuntimeData.CurrentHealth / victimNpc.Definition.MaxHealth 
                    : 1f;
            }
            // Check if victim is the Player
            else if (victim.GetComponent<PlayerController>() != null)
            {
                var playerController = victim.GetComponent<PlayerController>();
                victimFaction = Faction.Player;
                // Player has moderate threat level (not weak, not strong)
                victimThreatLevel = 50f;
                var healthComp = victim.GetComponent<FollowMyFootsteps.Combat.HealthComponent>();
                victimHealthPercent = healthComp != null ? healthComp.HealthPercentage : 1f;
            }
            else
            {
                // Unknown entity type, can't broadcast
                return;
            }

            // Create distress call
            var distressCall = new DistressCall
            {
                Victim = victim,
                Attacker = attacker,
                VictimFaction = victimFaction,
                Position = victim.transform.position,
                Timestamp = Time.time,
                CreatedOnTurn = GetCurrentTurn(),
                DamageReceived = damage,
                VictimThreatLevel = victimThreatLevel,
                VictimHealthPercent = victimHealthPercent
            };

            // Add or update existing distress call
            var existing = activeDistressCalls.Find(c => c.Victim == victim && c.Attacker == attacker);
            if (existing != null)
            {
                existing.DamageReceived += damage;
                existing.Timestamp = Time.time;
                existing.VictimHealthPercent = distressCall.VictimHealthPercent;
            }
            else
            {
                activeDistressCalls.Add(distressCall);
            }

            Debug.Log($"[FactionAlertManager] Distress call on Turn {distressCall.CreatedOnTurn}: " +
                     $"{victim.name} ({distressCall.VictimFaction}) attacked by {attacker.name} for {damage} damage! " +
                     $"Expires after Turn {distressCall.CreatedOnTurn + alertDurationInRounds}");

            // Spawn visual distress popup above victim (based on settings)
            if (ShouldShowDistressPopup(attacker))
            {
                AlertPopupPool.Instance.SpawnDistressPopup(victim.transform.position, distressCall.SoundLevel);
            }

            // Notify listeners
            OnDistressCall?.Invoke(distressCall);
        }

        /// <summary>
        /// Get all active distress calls relevant to a specific faction within range.
        /// Returns unsorted list - use GetLoudestDistressCall or GetHighestPriorityDistressCall for sorted results.
        /// </summary>
        /// <param name="myFaction">The faction looking for allies to help</param>
        /// <param name="myPosition">Position of the helper NPC</param>
        /// <param name="factionSettings">Faction settings for relationship checks</param>
        /// <param name="rangeInHexes">Range in hex cells (0 or negative to use default maxAlertRange converted to hexes)</param>
        /// <returns>List of relevant distress calls within range</returns>
        public List<DistressCall> GetRelevantDistressCalls(Faction myFaction, Vector3 myPosition, FactionSettings factionSettings, int rangeInHexes = 0)
        {
            var relevant = new List<DistressCall>();
            
            // Convert default world range to hex cells if not specified
            int effectiveRange = rangeInHexes > 0 ? rangeInHexes : Mathf.RoundToInt(maxAlertRange / (HexMetrics.innerRadius * 2f));
            
            HexCoord myHexPos = HexMetrics.WorldToHexCoord(myPosition);

            foreach (var call in activeDistressCalls)
            {
                if (call.Victim == null || call.Attacker == null) continue;

                // Check if victim is an ally
                bool isAlly = false;
                if (factionSettings != null)
                {
                    isAlly = factionSettings.IsFriendly(myFaction, call.VictimFaction) ||
                             myFaction == call.VictimFaction;
                }
                else
                {
                    // Fallback: same faction = ally
                    isAlly = myFaction == call.VictimFaction;
                }

                if (!isAlly) continue;

                // Check if within alert range (using hex distance)
                HexCoord callHexPos = HexMetrics.WorldToHexCoord(call.Position);
                int hexDistance = HexCoord.Distance(myHexPos, callHexPos);
                if (hexDistance > effectiveRange) continue;

                relevant.Add(call);
            }

            return relevant;
        }
        
        /// <summary>
        /// Legacy overload for backwards compatibility - converts world units to hex cells.
        /// Prefer using the hex-based overload for accurate range calculations.
        /// </summary>
        public List<DistressCall> GetRelevantDistressCalls(Faction myFaction, Vector3 myPosition, FactionSettings factionSettings, float worldRange)
        {
            int hexRange = Mathf.RoundToInt(worldRange / (HexMetrics.innerRadius * 2f));
            return GetRelevantDistressCalls(myFaction, myPosition, factionSettings, hexRange);
        }
        
        /// <summary>
        /// Get ALL active distress calls within range, regardless of faction.
        /// Used for HEARING detection - when you can only hear a scream, you don't know if it's friend or foe.
        /// The NPC must investigate and get within vision range to determine faction relationship.
        /// </summary>
        /// <param name="myPosition">Position of the listener</param>
        /// <param name="rangeInHexes">Hearing range in hex cells</param>
        /// <param name="excludeSelf">GameObject to exclude (usually the listener)</param>
        /// <returns>List of all distress calls within range</returns>
        public List<DistressCall> GetAllDistressCallsInRange(Vector3 myPosition, int rangeInHexes, GameObject excludeSelf = null)
        {
            var relevant = new List<DistressCall>();
            
            HexCoord myHexPos = HexMetrics.WorldToHexCoord(myPosition);

            foreach (var call in activeDistressCalls)
            {
                if (call.Victim == null || call.Attacker == null) continue;
                
                // Exclude self
                if (excludeSelf != null && call.Victim == excludeSelf) continue;

                // Check if within hearing range (using hex distance)
                HexCoord callHexPos = HexMetrics.WorldToHexCoord(call.Position);
                int hexDistance = HexCoord.Distance(myHexPos, callHexPos);
                if (hexDistance > rangeInHexes) continue;

                relevant.Add(call);
            }

            return relevant;
        }
        
        /// <summary>
        /// Get the loudest distress call within hearing range.
        /// Used when NPC can HEAR but not SEE the distress - they respond to the loudest call.
        /// Sound level is based on victim's health (lower health = louder/more desperate cries).
        /// 
        /// IMPORTANT: This method does NOT filter by faction relationship!
        /// When you can only HEAR a scream, you don't know if it's friend or foe.
        /// The NPC investigates the sound and determines friend/foe when within vision range.
        /// </summary>
        /// <param name="myPosition">Position of the listener</param>
        /// <param name="hearingRangeHexes">Hearing range in hex cells</param>
        /// <param name="excludeSelf">GameObject to exclude from results</param>
        /// <returns>The loudest distress call, or null if none heard</returns>
        public DistressCall GetLoudestDistressCall(Vector3 myPosition, int hearingRangeHexes,
                                                    GameObject excludeSelf = null)
        {
            var calls = GetAllDistressCallsInRange(myPosition, hearingRangeHexes, excludeSelf);
            
            if (calls.Count == 0) return null;
            
            HexCoord myHexPos = HexMetrics.WorldToHexCoord(myPosition);
            
            // Sort by sound level (loudest first), then by distance (closer is easier to hear)
            calls.Sort((a, b) =>
            {
                // First: louder sound (lower health = more desperate = louder)
                int soundCompare = b.SoundLevel.CompareTo(a.SoundLevel);
                if (soundCompare != 0) return soundCompare;
                
                // Second: closer distance in hexes (easier to hear)
                HexCoord aHexPos = HexMetrics.WorldToHexCoord(a.Position);
                HexCoord bHexPos = HexMetrics.WorldToHexCoord(b.Position);
                int distA = HexCoord.Distance(myHexPos, aHexPos);
                int distB = HexCoord.Distance(myHexPos, bHexPos);
                int distCompare = distA.CompareTo(distB);
                if (distCompare != 0) return distCompare;
                
                // Third: more recent
                return b.Timestamp.CompareTo(a.Timestamp);
            });
            
            return calls[0];
        }
        
        /// <summary>
        /// Legacy overload for backwards compatibility - converts world units to hex cells.
        /// </summary>
        public DistressCall GetLoudestDistressCall(Vector3 myPosition, float hearingRangeWorld,
                                                    GameObject excludeSelf = null)
        {
            int hexRange = Mathf.RoundToInt(hearingRangeWorld / (HexMetrics.innerRadius * 2f));
            return GetLoudestDistressCall(myPosition, hexRange, excludeSelf);
        }

        /// <summary>
        /// Get the highest priority distress call within VISION range.
        /// Used when NPC can SEE the distress - they can assess who needs help most.
        /// Prioritizes weak allies (low threat level) who are in danger (low health).
        /// NOTE: Only use this for vision range! For hearing range, use GetLoudestDistressCall.
        /// </summary>
        /// <param name="myFaction">The faction looking for allies to help</param>
        /// <param name="myPosition">Position of the helper NPC</param>
        /// <param name="factionSettings">Faction settings for relationship checks</param>
        /// <param name="visionRangeHexes">Vision range in hex cells</param>
        /// <param name="excludeSelf">GameObject to exclude from results (usually the caller)</param>
        public DistressCall GetHighestPriorityDistressCall(Faction myFaction, Vector3 myPosition, 
                                                            FactionSettings factionSettings, int visionRangeHexes,
                                                            GameObject excludeSelf = null)
        {
            var calls = GetRelevantDistressCalls(myFaction, myPosition, factionSettings, visionRangeHexes);
            
            // Exclude self if specified
            if (excludeSelf != null)
            {
                calls.RemoveAll(c => c.Victim == excludeSelf);
            }
            
            if (calls.Count == 0) return null;
            
            // Sort by protection priority (can assess visually who needs help most)
            calls.Sort((a, b) =>
            {
                // First priority: lower threat level (protect the weak - villager before guard)
                int threatCompare = a.VictimThreatLevel.CompareTo(b.VictimThreatLevel);
                if (threatCompare != 0) return threatCompare;

                // Second priority: lower health % (more urgent - almost dead before scratched)
                int healthCompare = a.VictimHealthPercent.CompareTo(b.VictimHealthPercent);
                if (healthCompare != 0) return healthCompare;

                // Third priority: more recent (fresher calls)
                return b.Timestamp.CompareTo(a.Timestamp);
            });

            return calls[0];
        }
        
        /// <summary>
        /// Legacy overload for backwards compatibility - converts world units to hex cells.
        /// </summary>
        public DistressCall GetHighestPriorityDistressCall(Faction myFaction, Vector3 myPosition, 
                                                            FactionSettings factionSettings, float visionRangeWorld,
                                                            GameObject excludeSelf = null)
        {
            int hexRange = Mathf.RoundToInt(visionRangeWorld / (HexMetrics.innerRadius * 2f));
            return GetHighestPriorityDistressCall(myFaction, myPosition, factionSettings, hexRange, excludeSelf);
        }

        /// <summary>
        /// Check if a specific NPC is currently under attack
        /// </summary>
        public bool IsUnderAttack(GameObject npc)
        {
            int currentTurn = GetCurrentTurn();
            return activeDistressCalls.Exists(c => c.Victim == npc && !c.IsExpired(alertDurationInRounds, currentTurn));
        }

        /// <summary>
        /// Get who is attacking a specific NPC
        /// </summary>
        public GameObject GetAttacker(GameObject victim)
        {
            int currentTurn = GetCurrentTurn();
            var call = activeDistressCalls.Find(c => c.Victim == victim && !c.IsExpired(alertDurationInRounds, currentTurn));
            return call?.Attacker;
        }

        /// <summary>
        /// Calculate threat level of an NPC (lower = needs more protection).
        /// Based on combat capabilities.
        /// </summary>
        private float CalculateThreatLevel(NPCDefinition definition)
        {
            if (definition == null) return 50f;

            // Calculate threat based on combat stats
            // Lower values = weaker = higher priority for protection
            float threatLevel = 0f;

            // Factor 1: Attack damage (0-100 range)
            threatLevel += definition.AttackDamage * 2f;

            // Factor 2: Health pool
            threatLevel += definition.MaxHealth * 0.3f;

            // Factor 3: Crit potential
            float critDps = definition.AttackDamage * (1 + (definition.CritChance / 100f) * (definition.CritMultiplier - 1));
            threatLevel += critDps;

            // Normalize to 0-100 range approximately
            threatLevel = Mathf.Clamp(threatLevel / 2f, 0f, 100f);

            return threatLevel;
        }

        /// <summary>
        /// Calculate protection priority for an NPC (higher = needs more protection)
        /// </summary>
        public static float CalculateProtectionPriority(NPCDefinition definition, float currentHealthPercent)
        {
            if (definition == null) return 0f;

            // Start with inverse of threat level (weak NPCs get high priority)
            float priority = 100f - Instance.CalculateThreatLevel(definition);

            // Factor in current health (lower health = higher priority)
            priority += (1f - currentHealthPercent) * 50f;

            // Bonus for certain NPC types that should be protected
            if (definition.Type == NPCType.Friendly)
            {
                priority += 20f;
            }

            return priority;
        }

        /// <summary>
        /// Clear all distress calls (e.g., at end of combat)
        /// </summary>
        public void ClearAllDistressCalls()
        {
            activeDistressCalls.Clear();
        }

        /// <summary>
        /// Clear distress calls for a specific attacker (e.g., when they die)
        /// </summary>
        public void ClearDistressCallsForAttacker(GameObject attacker)
        {
            activeDistressCalls.RemoveAll(c => c.Attacker == attacker);
        }
    }
}
