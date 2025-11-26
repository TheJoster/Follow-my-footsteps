using UnityEngine;
using System;
using System.Collections.Generic;

namespace FollowMyFootsteps.Entities
{
    /// <summary>
    /// Global event manager for faction-wide alerts and distress calls.
    /// Allows NPCs to respond when allied faction members are attacked.
    /// Phase 5 - Allied Protection System
    /// </summary>
    public class FactionAlertManager : MonoBehaviour
    {
        private static FactionAlertManager instance;
        public static FactionAlertManager Instance => instance;

        [Header("Settings")]
        [Tooltip("Maximum range (in world units) for distress calls to be heard")]
        [SerializeField] private float maxAlertRange = 15f;
        
        [Tooltip("How long an alert remains active (seconds)")]
        [SerializeField] private float alertDuration = 10f;

        /// <summary>
        /// Represents a distress call from an NPC being attacked
        /// </summary>
        public class DistressCall
        {
            public GameObject Victim;           // The NPC being attacked
            public GameObject Attacker;         // Who is attacking them
            public Faction VictimFaction;       // Faction of the victim
            public Vector3 Position;            // Where the attack happened
            public float Timestamp;             // When the attack happened
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
            
            public bool IsExpired(float duration) => Time.time - Timestamp > duration;
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
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Clean up expired distress calls
            activeDistressCalls.RemoveAll(call => call.IsExpired(alertDuration) || 
                                                   call.Victim == null || 
                                                   call.Attacker == null);
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

            Debug.Log($"[FactionAlertManager] Distress call: {victim.name} ({distressCall.VictimFaction}) " +
                     $"attacked by {attacker.name} for {damage} damage! " +
                     $"Threat level: {distressCall.VictimThreatLevel:F1}, Health: {distressCall.VictimHealthPercent:P0}");

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
        /// <param name="customRange">Custom range (0 or negative to use default maxAlertRange)</param>
        /// <returns>List of relevant distress calls within range</returns>
        public List<DistressCall> GetRelevantDistressCalls(Faction myFaction, Vector3 myPosition, FactionSettings factionSettings, float customRange = 0f)
        {
            var relevant = new List<DistressCall>();
            float effectiveRange = customRange > 0 ? customRange : maxAlertRange;

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

                // Check if within alert range
                float distance = Vector3.Distance(myPosition, call.Position);
                if (distance > effectiveRange) continue;

                relevant.Add(call);
            }

            return relevant;
        }
        
        /// <summary>
        /// Get the loudest distress call within hearing range.
        /// Used when NPC can HEAR but not SEE the distress - they respond to the loudest call.
        /// Sound level is based on victim's health (lower health = louder/more desperate cries).
        /// </summary>
        /// <param name="myFaction">The faction looking for allies to help</param>
        /// <param name="myPosition">Position of the helper NPC</param>
        /// <param name="factionSettings">Faction settings for relationship checks</param>
        /// <param name="hearingRange">Hearing range in world units</param>
        /// <param name="excludeSelf">GameObject to exclude from results</param>
        /// <returns>The loudest distress call, or null if none heard</returns>
        public DistressCall GetLoudestDistressCall(Faction myFaction, Vector3 myPosition, 
                                                    FactionSettings factionSettings, float hearingRange,
                                                    GameObject excludeSelf = null)
        {
            var calls = GetRelevantDistressCalls(myFaction, myPosition, factionSettings, hearingRange);
            
            // Exclude self if specified
            if (excludeSelf != null)
            {
                calls.RemoveAll(c => c.Victim == excludeSelf);
            }
            
            if (calls.Count == 0) return null;
            
            // Sort by sound level (loudest first), then by distance (closer is easier to hear)
            calls.Sort((a, b) =>
            {
                // First: louder sound (lower health = more desperate = louder)
                int soundCompare = b.SoundLevel.CompareTo(a.SoundLevel);
                if (soundCompare != 0) return soundCompare;
                
                // Second: closer distance (easier to hear)
                float distA = Vector3.Distance(myPosition, a.Position);
                float distB = Vector3.Distance(myPosition, b.Position);
                int distCompare = distA.CompareTo(distB);
                if (distCompare != 0) return distCompare;
                
                // Third: more recent
                return b.Timestamp.CompareTo(a.Timestamp);
            });
            
            return calls[0];
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
        /// <param name="visionRange">Vision range in world units</param>
        /// <param name="excludeSelf">GameObject to exclude from results (usually the caller)</param>
        public DistressCall GetHighestPriorityDistressCall(Faction myFaction, Vector3 myPosition, 
                                                            FactionSettings factionSettings, float visionRange,
                                                            GameObject excludeSelf = null)
        {
            var calls = GetRelevantDistressCalls(myFaction, myPosition, factionSettings, visionRange);
            
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
        /// Check if a specific NPC is currently under attack
        /// </summary>
        public bool IsUnderAttack(GameObject npc)
        {
            return activeDistressCalls.Exists(c => c.Victim == npc && !c.IsExpired(alertDuration));
        }

        /// <summary>
        /// Get who is attacking a specific NPC
        /// </summary>
        public GameObject GetAttacker(GameObject victim)
        {
            var call = activeDistressCalls.Find(c => c.Victim == victim && !c.IsExpired(alertDuration));
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
