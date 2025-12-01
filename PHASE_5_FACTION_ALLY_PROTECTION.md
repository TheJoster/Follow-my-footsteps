# Phase 5: Faction System & Ally Protection

> **Status**: ✅ Implemented  
> **Last Updated**: December 1, 2025

---

## Overview

This document describes the **Faction System** and **Ally Protection System** implemented as part of Phase 5 (Combat & Interaction). These systems enable NPCs to:

1. Determine friend/foe relationships based on faction membership
2. Respond to allied NPCs in distress
3. Prioritize protecting weaker allies ("Protect the Weak")
4. Use realistic perception (vision vs. hearing) for decision-making
5. **Display visual feedback** for distress calls and responses

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

### Important: Same Faction = Always Allied

The `FactionSystem.GetStanding()` method enforces that **entities of the same faction are always Allied**, regardless of the relationship matrix:

```csharp
if (source == target && source != Faction.None)
    return FactionStanding.Allied;
```

This means:
- Bandit ↔ Bandit = Allied ✅
- Goblin ↔ Goblin = Allied ✅
- Guard ↔ Guard = Allied ✅

### Cross-Hostile Faction Relationships

**Important**: Not all "hostile" factions cooperate with each other. Distress calls only work between **Allied** or **Friendly** factions:

| Faction Pair | Relationship | Will Respond to Distress? |
|-------------|--------------|---------------------------|
| Bandit ↔ Bandit | Allied (same) | ✅ Yes |
| Goblin ↔ Goblin | Allied (same) | ✅ Yes |
| Undead ↔ Undead | Allied (same) | ✅ Yes |
| Cultist ↔ Cultist | Allied (same) | ✅ Yes |
| **Bandits ↔ Mercenaries** | Friendly | ✅ Yes |
| **Cultists ↔ Undead** | Allied | ✅ Yes |
| Bandits ↔ Goblins | Unfriendly | ❌ No |
| Goblins ↔ Undead | Neutral | ❌ No |
| Bandits ↔ Undead | Neutral | ❌ No |

**Gameplay Implication**: If a Bandit calls for help, other Bandits and Mercenaries will respond, but Goblins will ignore it (they're unfriendly rivals, not allies).

---

## Ally Protection System

### Overview

When an NPC or player is attacked, nearby allied NPCs can respond to help. The system uses a two-tier perception model:

1. **Vision Range**: NPC can SEE who needs help → faction relationship checked immediately
2. **Hearing Range**: NPC can only HEAR screams → must investigate before knowing friend/foe

### Faction Relationship Filtering

**Vision range and hearing range handle faction relationships differently:**

#### Vision Range (Faction Filtered)

When an NPC can **see** the distress (within vision range), faction is checked immediately:

```csharp
// GetHighestPriorityDistressCall uses GetRelevantDistressCalls which filters:
bool isAlly = factionSettings.IsFriendly(myFaction, call.VictimFaction) ||
              myFaction == call.VictimFaction;
if (!isAlly) continue;  // Non-allies filtered out for vision
```

#### Hearing Range (NO Faction Filter)

When an NPC can only **hear** the distress (outside vision range), faction is NOT checked:

```csharp
// GetLoudestDistressCall uses GetAllDistressCallsInRange which does NOT filter by faction
// The NPC hears a scream but doesn't know if it's friend or foe!
var calls = GetAllDistressCallsInRange(myPosition, hearingRangeHexes, excludeSelf);
```

**Rationale**: If you can only hear a scream from far away, you don't know who's screaming. The NPC must investigate by moving toward the sound. Only when they get within vision range can they determine if the victim is an ally.

### Investigation Workflow

1. NPC hears distress call (outside vision range)
2. NPC sets `HasDistressToInvestigate = true` and stores `DistressInvestigationTarget`
3. AI state machine moves NPC toward the investigation target
4. When NPC arrives within vision range, call `CompleteDistressInvestigation()`
5. Returns `true` if victim is ally → begin protection
6. Returns `false` if victim is NOT ally → resume normal behavior

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
│     → Can SEE who needs help (FACTION FILTERED - allies only)   │
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
│ priority target          │    │     → Can only HEAR screams     │
│ (protect weakest ally)   │    │     → NO FACTION FILTER!        │
│ Register attacker now    │    │     → Don't know if friend/foe  │
└──────────────────────────┘    │     → Investigate the location  │
                                └─────────────────────────────────┘
                                                │
                                                ▼
                                ┌─────────────────────────────────┐
                                │  3. NPC moves toward sound      │
                                │     → Sets investigation target │
                                │     → Does NOT register threat  │
                                └─────────────────────────────────┘
                                                │
                                                ▼
                                ┌─────────────────────────────────┐
                                │  4. NPC arrives within VISION   │
                                │     → CompleteDistressInvestigation()
                                │     → Now can SEE victim        │
                                │     → Check faction relationship│
                                └─────────────────────────────────┘
                                                │
                              ┌─────────────────┴─────────────────┐
                              │ ALLY                              │ NOT ALLY
                              ▼                                   ▼
                ┌──────────────────────────┐    ┌──────────────────────────┐
                │ Begin protection!        │    │ Ignore and resume        │
                │ Register attacker threat │    │ normal patrol            │
                └──────────────────────────┘    └──────────────────────────┘
```

### Vision Range vs Hearing Range

| Aspect | Vision Range | Hearing Range |
|--------|--------------|---------------|
| **Detection** | Line-of-sight | Sound-based |
| **Default** | 5 hex cells | 10 hex cells |
| **Faction Filter** | ✅ Yes - allies only | ❌ No - hears ALL screams |
| **Decision** | Assess priority (protect weakest) | Respond to loudest (investigate) |
| **Immediate Action** | Register threat, begin protection | Move toward sound location |
| **Method** | `GetHighestPriorityDistressCall()` | `GetLoudestDistressCall()` |
| **Sorting** | 1. Threat level (low=weak) 2. Health % 3. Recency | 1. Sound level 2. Distance 3. Recency |

### Important: Hearing Does NOT Know Friend vs Foe

When an NPC can only **hear** a distress call (outside vision range), they don't know who's screaming:
- Could be an ally in danger → should help
- Could be an enemy fighting someone else → should ignore (or attack both!)

The NPC must **investigate** by moving toward the sound. Only when they get within **vision range** can they determine the faction relationship and decide whether to help.

**API for investigation:**
```csharp
// Check if NPC has a distress to investigate
perception.HasDistressToInvestigate  // true if heard but not seen

// Get the location to investigate
perception.DistressInvestigationTarget  // Vector3 world position

// When NPC arrives at location (within vision range)
bool isAlly = perception.CompleteDistressInvestigation();
// Returns true if victim is ally → begin protection
// Returns false if victim is NOT ally → resume normal behavior
```

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

---

## Visual Alert System

### Overview

The game provides visual feedback when NPCs make distress calls and when other NPCs respond. This helps players understand the faction dynamics and ally protection in action.

### Alert Types

| Alert Type | Symbol | Color | Description |
|------------|--------|-------|-------------|
| **Distress** | ⚠ | Red | NPC is calling for help (victim) |
| **Vision Response** | 👁 | Green | NPC responded by seeing the ally in distress |
| **Sound Response** | 👂 | Blue | NPC responded by hearing the distress call |

### Visual Appearance

- **Floating Popups**: Similar to damage numbers, alerts float upward and fade
- **Sound Level Scaling**: Distress calls scale larger based on sound level (more desperate = bigger icon)
- **Bounce Animation**: Popups use a scale curve for a subtle bounce effect
- **Fade Out**: Alerts fade out in the last 30% of their lifetime

### Components

#### AlertPopup.cs

The `AlertPopup` component handles individual alert visuals:

```csharp
// Initialize a distress alert
alertPopup.Initialize(AlertPopup.AlertType.Distress, soundLevel);

// Initialize a vision response
alertPopup.Initialize(AlertPopup.AlertType.ResponseVision);

// Initialize a sound response
alertPopup.Initialize(AlertPopup.AlertType.ResponseSound);

// Custom text/color
alertPopup.InitializeCustom("HELP!", Color.red, 40f);
```

#### AlertPopupPool.cs

Object pool for efficient popup management:

```csharp
// Spawn distress popup (when NPC is attacked)
AlertPopupPool.Instance.SpawnDistressPopup(position, soundLevel);

// Spawn vision response (when NPC sees ally in distress)
AlertPopupPool.Instance.SpawnVisionResponsePopup(position);

// Spawn sound response (when NPC hears distress call)
AlertPopupPool.Instance.SpawnSoundResponsePopup(position);

// Return to pool
AlertPopupPool.Instance.ReturnToPool(popup);
```

### Integration Points

1. **FactionAlertManager.BroadcastDistressCall()**
   - Spawns a red distress popup above the victim
   - Popup size scales with sound level (health-based)

2. **PerceptionComponent.OnAllyDistressCall()**
   - Spawns green popup if NPC can see the distress (vision range)
   - Spawns blue popup if NPC can only hear the distress (hearing range)

### Setup in Unity

1. Create an empty GameObject named "AlertPopupPool"
2. Add the `AlertPopupPool` component
3. (Optional) Assign an AlertPopup prefab for custom styling
4. If no prefab is assigned, basic popups are created automatically

### Prefab Configuration (Optional)

If creating a custom AlertPopup prefab:
1. Create a GameObject with a WorldSpace Canvas
2. Add TextMeshProUGUI for the text
3. Add AlertPopup component
4. Configure colors and animations in the inspector

### Test Coverage

#### AlertPopupTests.cs

| Test | Coverage |
|------|----------|
| `AlertType_Distress_Exists` | ✅ Enum value defined |
| `AlertType_ResponseVision_Exists` | ✅ Enum value defined |
| `AlertType_ResponseSound_Exists` | ✅ Enum value defined |
| `Initialize_Distress_SetsUpCorrectly` | ✅ Initialization works |
| `Initialize_ResponseVision_SetsUpCorrectly` | ✅ Initialization works |
| `Initialize_ResponseSound_SetsUpCorrectly` | ✅ Initialization works |
| `Initialize_Distress_HighSoundLevel_LargerSize` | ✅ Size scaling |
| `InitializeCustom_SetsCustomText` | ✅ Custom text works |
| `ResetPopup_ResetsState` | ✅ Pool reset works |

#### AlertPopupPoolTests.cs

| Test | Coverage |
|------|----------|
| `Instance_IsSetAfterAwake` | ✅ Singleton pattern |
| `SpawnDistressPopup_ReturnsPopup` | ✅ Spawning works |
| `SpawnDistressPopup_PopupIsActive` | ✅ Popup activates |
| `SpawnVisionResponsePopup_ReturnsPopup` | ✅ Vision response |
| `SpawnSoundResponsePopup_ReturnsPopup` | ✅ Sound response |
| `ReturnToPool_PopupBecomesInactive` | ✅ Pool return works |
| `ReturnToPool_PopupCanBeReused` | ✅ Reuse works |
| `GetPoolStats_ReturnsValidString` | ✅ Stats reporting |

---

## Files Modified/Created (Visual Alert System)

### New Files
- `Assets/_Project/Scripts/Combat/AlertPopup.cs` - Alert popup component
- `Assets/_Project/Scripts/Combat/AlertPopupPool.cs` - Object pool for alerts
- `Assets/_Project/Scripts/Entities/NPCPathVisualizer.cs` - NPC destination path lines
- `Assets/_Project/Tests/EditMode/AlertPopupTests.cs` - Alert popup unit tests
- `Assets/_Project/Tests/EditMode/NPCPathVisualizerTests.cs` - Path visualizer unit tests

### Modified Files
- `Assets/_Project/Scripts/Entities/FactionAlertManager.cs` - Added distress visual
- `Assets/_Project/Scripts/AI/PerceptionComponent.cs` - Added response visuals
- `Assets/_Project/Scripts/NPCController.cs` - Added path visualization integration
- `Assets/_Project/Tests/EditMode/FollowMyFootsteps.Tests.EditMode.asmdef` - Added TMPro reference

---

## NPC Path Visualization

### Overview

NPCs now display destination path lines when moving, similar to the player's path preview. This helps with debugging NPC movement and understanding ally protection responses. The system now supports:

- **Faction-based path colors** - Each faction has a distinct color
- **Multi-turn path visualization** - Colors fade for paths beyond movement range
- **Path type tinting** - Distress/protection paths have contextual tints

### Faction-Based Path Colors

Each faction has a unique base color for their paths:

| Faction | Color | RGB |
|---------|-------|-----|
| **Player** | Blue | `(0.2, 0.6, 1.0)` |
| **Villagers** | Green | `(0.4, 0.8, 0.4)` |
| **Guards** | Royal Blue | `(0.3, 0.5, 0.9)` |
| **Bandits** | Orange-Brown | `(0.8, 0.4, 0.2)` |
| **Goblins** | Olive Green | `(0.5, 0.7, 0.2)` |
| **Undead** | Purple | `(0.5, 0.3, 0.6)` |
| **Wildlife** | Brown | `(0.6, 0.5, 0.3)` |
| **Cultists** | Dark Red | `(0.6, 0.1, 0.3)` |
| **Mercenaries** | Gray | `(0.5, 0.5, 0.5)` |
| **Nobility** | Gold | `(0.9, 0.8, 0.2)` |
| **None/Default** | Gray | `(0.6, 0.6, 0.6)` |

### Multi-Turn Path Visualization

Paths are segmented by the NPC's `MovementRange` and color-coded by turn:

| Turn | Saturation | Description |
|------|------------|-------------|
| **Turn 1** | 100% | Full faction color (within movement range) |
| **Turn 2** | 70% | Slightly faded |
| **Turn 3** | 50% | Noticeably faded |
| **Turn 4+** | 35% | Most faded |

### Path Types (Contextual Tints)

Path types apply a subtle tint over the faction color:

| Path Type | Tint | Description |
|-----------|------|-------------|
| **Normal** | None | Regular NPC movement (patrol, wander) |
| **DistressResponse** | Red (30%) | NPC responding to distress call |
| **AllyProtection** | Green (30%) | NPC moving to protect an ally |

### Configuration

#### Per-NPC Toggle (Inspector)
```csharp
[Header("Debug")]
[SerializeField] private bool showPathVisualization = true;
```

#### Global Toggle (Runtime)
```csharp
// Toggle all NPC paths on/off
NPCPathVisualizer.GlobalShowPaths = false;  // Hide all
NPCPathVisualizer.GlobalShowPaths = true;   // Show all
```

### NPCPathVisualizer Component

Added automatically to NPCs, provides:

```csharp
// Set faction for color (auto-set by NPCController)
pathVisualizer.SetFaction(Faction.Guards);

// Set movement range for turn calculation (auto-set by NPCController)
pathVisualizer.SetMovementRange(5);

// Get faction color
Color factionColor = pathVisualizer.GetFactionColor(Faction.Bandits);

// Show path with specific type
pathVisualizer.ShowPathLine(path, NPCPathVisualizer.PathType.AllyProtection);

// Hide path manually
pathVisualizer.HidePath();

// Check visibility
bool isShowing = pathVisualizer.IsVisible;

// Per-instance toggle
pathVisualizer.ShowPath = false;
```

### Visual Features

- **Dashed line pattern** for differentiation from player path
- **Faction-based colors** with turn saturation fade
- **Path type tinting** (red urgency / green protection)
- **Multi-turn segments** using separate LineRenderers per turn
- **Auto-updates** as NPC moves (removes completed steps)
- **Auto-hides** when movement completes or cancels

### Integration with NPCController

Path visualization is automatically initialized:
1. **Awake/Initialize** - Sets faction and movement range from NPCDefinition
2. **OnMovementStart** - Shows path from current position to destination
3. **OnMovementStep** - Updates path (removes completed segment)
4. **OnMovementComplete** - Hides path
5. **OnMovementCancelled** - Hides path

### Test Coverage

| Test | Coverage |
|------|----------|
| `ShowPathLine_ValidPath_ShowsPath` | ✅ |
| `ShowPathLine_WithPathType_DoesNotThrow` | ✅ |
| `HidePath_HidesPath` | ✅ |
| `ShowPath_SetFalse_HidesPath` | ✅ |
| `GlobalShowPaths_SetFalse_PreventsNewPaths` | ✅ |
| `OnStepCompleted_RemovesCompletedStep` | ✅ |
| `OnStepCompleted_LastStep_HidesPath` | ✅ |
| `SetFaction_DoesNotThrow` | ✅ |
| `GetFactionColor_ReturnsDistinctColors` | ✅ |
| `GetFactionColor_AllFactions_ReturnValidColors` | ✅ |
| `SetMovementRange_ValidRange_DoesNotThrow` | ✅ |
| `SetMovementRange_ZeroRange_ClampsToMinimum` | ✅ |
| `ShowPathLine_WithFactionAndRange_ShowsPath` | ✅ |
| `ShowPathLine_LongPath_CreatesMultipleTurnSegments` | ✅ |
| `ShowPathLine_DifferentFactions_AllShowPath` | ✅ |

---
