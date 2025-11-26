# Phase 5: Faction System & Ally Protection

> **Status**: ✅ Implemented  
> **Last Updated**: November 26, 2025

---

## Overview

This document describes the **Faction System** and **Ally Protection System** implemented as part of Phase 5 (Combat & Interaction). These systems enable NPCs to:

1. Determine friend/foe relationships based on faction membership
2. Respond to allied NPCs in distress
3. Prioritize protecting weaker allies ("Protect the Weak")
4. Use realistic perception (vision vs. hearing) for decision-making

---

## Faction System

### Faction Enum

The game includes 11 factions:

| Faction | ID | Description |
|---------|-----|-------------|
| `None` | 0 | Unaffiliated entity |
| `Player` | 1 | The player character |
| `Villagers` | 2 | Peaceful townsfolk |
| `Guards` | 3 | Town protectors |
| `Bandits` | 4 | Hostile outlaws |
| `Goblins` | 5 | Hostile monsters |
| `Undead` | 6 | Hostile undead creatures |
| `Wildlife` | 7 | Neutral animals |
| `Cultists` | 8 | Hostile cultist faction |
| `Mercenaries` | 9 | Neutral for-hire fighters |
| `Nobility` | 10 | Allied upper class |

### Faction Standing

Relationships between factions are defined by standings:

| Standing | Value | Behavior |
|----------|-------|----------|
| `Allied` | +2 | Full cooperation, will protect |
| `Friendly` | +1 | Peaceful, may assist |
| `Neutral` | 0 | Ignores unless provoked |
| `Unfriendly` | -1 | Suspicious, may attack if threatened |
| `Hostile` | -2 | Attacks on sight |

### Default Faction Relationships

```
              Player  Villagers  Guards  Bandits  Goblins  Undead  Wildlife  Cultists  Mercenaries  Nobility
Player          -      Friendly  Friendly Hostile  Hostile  Hostile  Neutral   Hostile    Neutral    Friendly
Villagers    Friendly     -      Allied   Hostile  Hostile  Hostile  Neutral   Hostile    Neutral    Friendly
Guards       Friendly   Allied     -      Hostile  Hostile  Hostile  Neutral   Hostile   Unfriendly   Allied
Bandits      Hostile   Hostile  Hostile     -     Unfriendly Neutral  Neutral   Neutral   Friendly   Hostile
Goblins      Hostile   Hostile  Hostile  Unfriendly   -      Neutral  Neutral   Neutral    Neutral   Hostile
Undead       Hostile   Hostile  Hostile   Neutral  Neutral     -      Hostile   Allied    Hostile    Hostile
Wildlife     Neutral   Neutral  Neutral   Neutral  Neutral  Hostile     -       Neutral    Neutral    Neutral
Cultists     Hostile   Hostile  Hostile   Neutral  Neutral  Allied   Neutral      -        Neutral   Hostile
Mercenaries  Neutral   Neutral  Unfriendly Friendly Neutral  Hostile  Neutral   Neutral      -       Neutral
Nobility     Friendly  Friendly  Allied   Hostile  Hostile  Hostile  Neutral   Hostile    Neutral      -
```

### Configuration

Faction relationships are configured via `FactionSettings` ScriptableObject:
- **Location**: `Assets/_Project/ScriptableObjects/FactionSettings.asset`
- **Auto-populate**: Relationships are automatically set on asset creation
- **Runtime queries**: `FactionSettings.IsEnemy()`, `IsFriendly()`, `IsNeutral()`, `GetStanding()`

---

## Ally Protection System

### Overview

When an NPC or player is attacked, nearby allied NPCs can respond to help. The system uses a two-tier perception model:

1. **Vision Range**: NPC can see and assess who needs help most
2. **Hearing Range**: NPC can only hear distress calls, responds to loudest

### Distress Call System

When damage is dealt via `HealthComponent.TakeDamage()`, a distress call is broadcast through `FactionAlertManager`:

```csharp
FactionAlertManager.Instance.BroadcastDistressCall(victim, attacker, damage);
```

#### DistressCall Properties

| Property | Type | Description |
|----------|------|-------------|
| `Victim` | GameObject | The NPC being attacked |
| `Attacker` | GameObject | Who is attacking them |
| `VictimFaction` | Faction | Faction of the victim |
| `Position` | Vector3 | World position of the attack |
| `Timestamp` | float | When the attack happened |
| `DamageReceived` | int | Total damage dealt |
| `VictimThreatLevel` | float | How dangerous the victim is (0-100) |
| `VictimHealthPercent` | float | Current health % of victim (0.0-1.0) |
| `SoundLevel` | float | How loud the distress call is (0-100) |

### Sound Level Formula

The sound level of a distress call is based on the victim's health - lower health means more desperate (louder) cries for help:

```
SoundLevel = BaseSound + HealthFactor + DamageBoost
```

| Health % | Base Sound | Health Factor | Damage Boost (20 dmg) | Approx. Sound Level |
|----------|------------|---------------|----------------------|---------------------|
| 100%     | 20         | 0             | +10                  | 30 (mild call)      |
| 80%      | 20         | 16            | +10                  | 46 (alert)          |
| 50%      | 20         | 40            | +10                  | 70 (urgent call)    |
| 30%      | 20         | 56            | +10                  | 86 (desperate)      |
| 10%      | 20         | 72            | +10                  | 100 (screaming)     |

- **Base Sound**: 20 (always present)
- **Health Factor**: `(1 - healthPercent) * 80` (max 80)
- **Damage Boost**: `min(damageReceived * 0.5, 20)` (max 20, fades as call ages)
- **Sound Level Capped**: 0-100 range

---

## Two-Tier Perception Model

### Decision Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    NPC Checks for Allies in Distress            │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  1. Check VISION RANGE for distress calls                       │
│     → Can SEE who needs help                                    │
│     → Use GetHighestPriorityDistressCall()                      │
│     → Prioritize: Weakest ally first, then lowest health        │
└─────────────────────────────────────────────────────────────────┘
                                │
                    Found call in vision range?
                                │
              ┌─────────────────┴─────────────────┐
              │ YES                               │ NO
              ▼                                   ▼
┌──────────────────────────┐    ┌─────────────────────────────────┐
│ Respond to highest       │    │  2. Check HEARING RANGE         │
│ priority target          │    │     → Can only HEAR calls       │
│ (protect weakest ally)   │    │     → Use GetLoudestDistressCall│
└──────────────────────────┘    │     → Respond to loudest call   │
                                │     (lowest health = loudest)   │
                                └─────────────────────────────────┘
```

### Vision Range vs Hearing Range

| Aspect | Vision Range | Hearing Range |
|--------|--------------|---------------|
| **Detection** | Line-of-sight | Sound-based |
| **Default** | 5 hex cells | 10 hex cells |
| **Decision** | Assess priority (protect weakest) | Respond to loudest (most desperate) |
| **Method** | `GetHighestPriorityDistressCall()` | `GetLoudestDistressCall()` |
| **Sorting** | 1. Threat level (low=weak) 2. Health % 3. Recency | 1. Sound level 2. Distance 3. Recency |

### Priority Sorting (Vision Range)

When NPC can SEE multiple allies in distress, they prioritize:

1. **Threat Level** (ascending) - Weakest ally first
   - Villager (low threat) > Guard (high threat)
   - Based on: AttackDamage + Health + Crit potential
   
2. **Health %** (ascending) - Most injured first
   - 10% health > 50% health
   
3. **Timestamp** (descending) - Most recent first

### Sound Sorting (Hearing Range)

When NPC can only HEAR distress calls, they respond to:

1. **Sound Level** (descending) - Loudest first
   - Low health = louder screams
   
2. **Distance** (ascending) - Closer is easier to hear
   
3. **Timestamp** (descending) - Most recent first

---

## Protection Priority Formula

When assessing how much protection an NPC needs:

```csharp
Priority = (100 - ThreatLevel) + HealthBonus + TypeBonus

where:
  ThreatLevel = AttackDamage * 2 + MaxHealth * 0.3 + CritDPS
  HealthBonus = (1 - CurrentHealthPercent) * 50
  TypeBonus   = 20 if NPCType.Friendly, else 0
```

### Example Protection Priorities

| NPC | Attack | Health | Health% | Type | Threat Level | Priority |
|-----|--------|--------|---------|------|--------------|----------|
| Villager | 5 | 50 | 80% | Friendly | ~25 | ~85 |
| Villager | 5 | 50 | 20% | Friendly | ~25 | ~125 |
| Guard | 15 | 100 | 80% | Friendly | ~60 | ~70 |
| Guard | 15 | 100 | 20% | Friendly | ~60 | ~110 |
| Bandit | 12 | 80 | 50% | Hostile | ~50 | ~75 |

**Result**: Guards will prioritize protecting wounded Villagers over wounded Guards.

---

## PerceptionComponent Settings

### Configuration Parameters

```csharp
[Header("Vision Settings")]
[SerializeField] private int visionRange = 5;           // Hex cells

[Header("Allied Protection")]
[SerializeField] private bool respondToDistressCalls = true;
[SerializeField] private int hearingRange = 10;         // Hex cells (0 = use visionRange)
[SerializeField] private float protectWeakBonus = 50f;  // Priority bonus (0-100)
```

### Methods

| Method | Description |
|--------|-------------|
| `GetVisionRange()` | Returns vision range in hex cells |
| `GetHearingRange()` | Returns hearing range (or vision if not set) |
| `SetHearingRange(int)` | Sets hearing range |
| `RegisterAllyAttacker(attacker, ally, damage, threatLevel)` | Registers attacker as threat |
| `IsAttackingAlly(target)` | Returns true if target is attacking an ally |

---

## FactionAlertManager API

### Broadcasting

```csharp
// Called automatically by HealthComponent.TakeDamage()
FactionAlertManager.Instance.BroadcastDistressCall(
    GameObject victim,
    GameObject attacker,
    int damage
);
```

### Querying (Vision Range)

```csharp
// Get highest priority call within vision range
// For when NPC can SEE the situation
var call = FactionAlertManager.Instance.GetHighestPriorityDistressCall(
    Faction myFaction,
    Vector3 myPosition,
    FactionSettings settings,
    float visionRangeWorld,
    GameObject excludeSelf = null
);
```

### Querying (Hearing Range)

```csharp
// Get loudest call within hearing range
// For when NPC can only HEAR the situation
var call = FactionAlertManager.Instance.GetLoudestDistressCall(
    Faction myFaction,
    Vector3 myPosition,
    FactionSettings settings,
    float hearingRangeWorld,
    GameObject excludeSelf = null
);
```

### Utility Methods

```csharp
bool IsUnderAttack(GameObject npc);
GameObject GetAttacker(GameObject victim);
void ClearAllDistressCalls();
void ClearDistressCallsForAttacker(GameObject attacker);
```

---

## NPC Asset Configuration

Each NPC type has been assigned a faction:

| NPC Asset | Type | Faction |
|-----------|------|---------|
| Guard.asset | Friendly | Guards |
| Farmer.asset | Friendly | Villagers |
| Villager.asset | Friendly | Villagers |
| Merchant.asset | Neutral | Villagers |
| Bandit.asset | Hostile | Bandits |
| Goblin.asset | Hostile | Goblins |

---

## Unit Test Coverage

### FactionSystemTests.cs

| Test | Coverage |
|------|----------|
| Same faction relationships | ✅ Allied, IsEnemy=false, IsFriendly=true |
| Player faction relationships | ✅ vs Villagers, Guards, Bandits, Goblins, Wildlife |
| Guards faction relationships | ✅ vs Player, Villagers, Bandits, Mercenaries |
| Villagers faction relationships | ✅ vs Player, Guards, Bandits |
| Hostile faction relationships | ✅ Bandits vs Guards, Goblins vs Player |
| Neutral faction relationships | ✅ Wildlife, Mercenaries |
| Scenario tests | ✅ Town defense, Bandit ambush, Cultist lair |

### FactionAlertManagerTests.cs

| Test | Coverage |
|------|----------|
| `CalculateProtectionPriority_WeakNPC_GetsHigherPriority` | ✅ |
| `CalculateProtectionPriority_LowHealth_IncreasesProtectionPriority` | ✅ |
| `CalculateProtectionPriority_FriendlyNPC_GetsBonusPriority` | ✅ |
| `GetRelevantDistressCalls_ReturnsEmpty_WhenNoDistressCalls` | ✅ |
| `GetHighestPriorityDistressCall_ReturnsNull_WhenNoDistressCalls` | ✅ |
| `GetLoudestDistressCall_ReturnsNull_WhenNoDistressCalls` | ✅ |
| `IsUnderAttack_ReturnsFalse_WhenNoDistressCalls` | ✅ |
| `ClearAllDistressCalls_ClearsAllCalls` | ✅ |

### Sound Level Tests

| Test | Coverage |
|------|----------|
| `DistressCall_SoundLevel_FullHealth_IsLow` | ✅ Full health ≤ 30 |
| `DistressCall_SoundLevel_LowHealth_IsHigh` | ✅ 10% health ≥ 80 |
| `DistressCall_SoundLevel_IncreasesWithDamage` | ✅ Damage adds loudness |
| `DistressCall_SoundLevel_LowerHealth_IsLouderThanHigherHealth` | ✅ |
| `DistressCall_SoundLevel_MaximumIsCapped` | ✅ Max 100 |

### ThreatAssessmentTests.cs (Ally Protection)

| Test | Coverage |
|------|----------|
| `RegisterAllyAttacker_AddsAttackerToVisibleTargets` | ✅ |
| `RegisterAllyAttacker_MarksAttackerAsAttackingAlly` | ✅ |
| `RegisterAllyAttacker_SetsProtectionPriority` | ✅ |
| `RegisterAllyAttacker_NullAttacker_DoesNotThrow` | ✅ |
| `RegisterAllyAttacker_SelfAsAttacker_Ignored` | ✅ |
| `IsAttackingAlly_UnknownTarget_ReturnsFalse` | ✅ |
| `GetHearingRange_DefaultValue_ReturnsPositiveValue` | ✅ |
| `SetHearingRange_ValidValue_UpdatesRange` | ✅ |
| `SetHearingRange_Zero_FallsBackToVisionRange` | ✅ |

---

## Files Modified/Created

### New Files
- `Assets/_Project/Scripts/Entities/FactionSystem.cs` - Faction enum and FactionSettings
- `Assets/_Project/Scripts/Entities/FactionAlertManager.cs` - Distress call management
- `PHASE_5_FACTION_ALLY_PROTECTION.md` - This documentation

### Modified Files
- `Assets/_Project/Scripts/Entities/NPCDefinition.cs` - Added Faction field
- `Assets/_Project/Scripts/AI/PerceptionComponent.cs` - Added ally protection logic
- `Assets/_Project/Scripts/Combat/HealthComponent.cs` - Broadcasts distress calls
- `Assets/_Project/Scripts/NPCController.cs` - Updated TakeDamage with attacker param
- `Assets/_Project/ScriptableObjects/NPCDefinitions/*.asset` - Added faction assignments

### Test Files
- `Assets/_Project/Tests/EditMode/FactionSystemTests.cs` - 45+ faction tests
- `Assets/_Project/Tests/EditMode/ThreatAssessmentTests.cs` - Ally protection tests

---

## Usage Example

### Setting Up a Guard to Protect Villagers

1. **Guard NPCDefinition**:
   ```
   Type: Friendly
   Faction: Guards
   VisionRange: 6
   ```

2. **Guard PerceptionComponent**:
   ```
   respondToDistressCalls: true
   hearingRange: 12
   protectWeakBonus: 50
   ```

3. **FactionSettings**:
   - Guards → Villagers: Allied
   - Guards → Player: Friendly
   - Guards → Bandits: Hostile

4. **Behavior**:
   - Bandit attacks Villager
   - HealthComponent broadcasts distress call
   - Guard receives call (within hearing range)
   - Guard checks: Is Villager an ally? Yes (Allied faction)
   - Guard registers Bandit as threat
   - Guard moves to intercept Bandit
   - Guard prioritizes protecting Villager over other Guards (Villager is weaker)

---

*Documentation created: November 26, 2025*
