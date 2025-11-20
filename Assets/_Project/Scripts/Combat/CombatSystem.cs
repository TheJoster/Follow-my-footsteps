using UnityEngine;
using FollowMyFootsteps.Grid;

namespace FollowMyFootsteps.Combat
{
    /// <summary>
    /// Damage type enumeration for resistance calculations
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        Fire,
        Ice,
        Poison
    }

    /// <summary>
    /// Static combat system managing damage calculations and combat mechanics
    /// </summary>
    public static class CombatSystem
    {
        /// <summary>
        /// Calculate and apply damage from attacker to target
        /// </summary>
        /// <param name="attacker">GameObject attacking</param>
        /// <param name="target">GameObject being attacked</param>
        /// <param name="baseDamage">Base damage amount</param>
        /// <param name="damageType">Type of damage for resistance calculations</param>
        /// <param name="canCrit">Whether this attack can critically hit</param>
        /// <param name="critChance">Chance to crit (0.0-1.0)</param>
        /// <param name="critMultiplier">Damage multiplier on crit</param>
        /// <returns>Actual damage dealt</returns>
        public static int DealDamage(
            GameObject attacker,
            GameObject target,
            int baseDamage,
            DamageType damageType = DamageType.Physical,
            bool canCrit = true,
            float critChance = 0.1f,
            float critMultiplier = 1.5f)
        {
            if (target == null)
            {
                Debug.LogWarning("[CombatSystem] Target is null, cannot deal damage");
                return 0;
            }

            // Get target's health component
            HealthComponent targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth == null)
            {
                Debug.LogWarning($"[CombatSystem] Target {target.name} has no HealthComponent");
                return 0;
            }

            if (targetHealth.IsDead)
            {
                Debug.Log($"[CombatSystem] Target {target.name} is already dead");
                return 0;
            }

            // Calculate final damage
            int finalDamage = baseDamage;

            // Apply critical hit
            bool isCriticalHit = false;
            if (canCrit && Random.value < critChance)
            {
                isCriticalHit = true;
                finalDamage = Mathf.RoundToInt(finalDamage * critMultiplier);
            }

            // TODO: Apply damage type resistances from target (Phase 5.1)
            // finalDamage = ApplyResistances(finalDamage, damageType, target);

            // Apply damage with critical hit information
            int damageDealt = targetHealth.TakeDamage(finalDamage, attacker, isCriticalHit);

            // Log combat event
            string attackerName = attacker != null ? attacker.name : "Unknown";
            string critText = isCriticalHit ? " (CRITICAL HIT!)" : "";
            Debug.Log($"[CombatSystem] {attackerName} dealt {damageDealt} {damageType} damage to {target.name}{critText}");

            return damageDealt;
        }

        /// <summary>
        /// Check if attacker can reach target with melee attack
        /// </summary>
        /// <param name="attackerPos">Attacker's hex coordinate</param>
        /// <param name="targetPos">Target's hex coordinate</param>
        /// <param name="attackRange">Attack range in hexes (default: 1 for melee)</param>
        /// <returns>True if target is in range</returns>
        public static bool IsInAttackRange(HexCoord attackerPos, HexCoord targetPos, int attackRange = 1)
        {
            int distance = HexCoord.Distance(attackerPos, targetPos);
            return distance <= attackRange;
        }

        /// <summary>
        /// Calculate action point cost for an attack
        /// </summary>
        /// <param name="attackType">Type of attack (melee, ranged, ability)</param>
        /// <returns>AP cost</returns>
        public static int GetAttackAPCost(string attackType = "melee")
        {
            return attackType switch
            {
                "melee" => 1,
                "ranged" => 1,
                "ability" => 2,
                "heavy" => 2,
                _ => 1
            };
        }

        /// <summary>
        /// Check if attacker has line of sight to target
        /// </summary>
        /// <param name="hexGrid">Reference to hex grid</param>
        /// <param name="attackerPos">Attacker's position</param>
        /// <param name="targetPos">Target's position</param>
        /// <returns>True if line of sight is clear</returns>
        public static bool HasLineOfSight(HexGrid hexGrid, HexCoord attackerPos, HexCoord targetPos)
        {
            if (hexGrid == null) return false;

            // Simple implementation: check if any cells between attacker and target are impassable
            // TODO: Implement proper line of sight algorithm (Phase 5.2)
            
            // For now, just check distance (can attack if in perception range)
            int distance = HexCoord.Distance(attackerPos, targetPos);
            return distance <= 10; // Max attack visibility range
        }

        /// <summary>
        /// Get damage with random variance
        /// </summary>
        /// <param name="baseDamage">Base damage value</param>
        /// <param name="variance">Variance percentage (0.0-1.0)</param>
        /// <returns>Damage with variance applied</returns>
        public static int GetDamageWithVariance(int baseDamage, float variance = 0.1f)
        {
            float min = baseDamage * (1f - variance);
            float max = baseDamage * (1f + variance);
            return Mathf.RoundToInt(Random.Range(min, max));
        }

        // TODO: Future combat features for Phase 5+
        // - ApplyResistances(damage, damageType, target) - armor/resistance system
        // - CalculateThreat(attacker, damage) - aggro/threat management
        // - ProcessStatusEffects(target) - poison, stun, slow effects
        // - GetAttackPriority(attacker) - attack queue ordering
    }
}
