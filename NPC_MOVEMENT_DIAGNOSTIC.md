# NPC Movement Diagnostic Guide

## Issue
NPCs are not visually moving during their turns, despite the turn system executing.

## Debug Logging Added
Comprehensive debug logging has been added to track the entire movement execution flow:

### What to Check in Unity Console

When you press Play and NPCs take their turns, you should see logs like:

```
[NPCController] ===== Villager TAKING TURN =====
[NPCController] State: Wander, AP: 5/5, Alive: True
[NPCController] Villager calling stateMachine.Update()...
[WanderState] OnUpdate for Villager. Waiting: False, AP: 5
[WanderState] Villager requesting path from (10,10) to (12,8)
[WanderState] Villager received path with 3 steps. Starting movement.
[WanderState] FollowPath returned: True
```

## Diagnostic Steps

### 1. **Is TakeTurn() Being Called?**

**Look for:** `[NPCController] ===== [NPCName] TAKING TURN =====`

**If NOT appearing:**
- Turn system isn't executing NPCs
- Check TurnManager is in scene and initialized
- Check NPCs are registered with TurnManager
- Verify game isn't paused

**If appearing:** ✅ Continue to step 2

---

### 2. **Is the State Machine Working?**

**Look for:** `[NPCController] [NPCName] calling stateMachine.Update()...`

**If error:** `[NPCController] [NPCName] has no state machine!`
- NPC's state machine wasn't initialized
- Check NPCController.Awake() is creating state machine
- Check NPC definition has valid initial state

**If appearing:** ✅ Continue to step 3

---

### 3. **Is OnUpdate() Being Called?**

**Look for:** `[PatrolState] OnUpdate called for [NPCName]` or `[WanderState] OnUpdate for [NPCName]`

**If NOT appearing:**
- State machine isn't calling OnUpdate()
- Check StateMachine.Update() implementation
- Check current state is registered

**If appearing:** ✅ Continue to step 4

---

### 4. **Do NPCs Have Action Points?**

**Look for:** `AP: X/Y` in logs where X > 0

**If AP: 0/X:**
- NPCs have no action points
- Check `MaxActionPoints` in NPC ScriptableObject definitions
- Check `OnTurnStart()` is restoring AP
- Verify turn system calls `OnTurnStart()` before `TakeTurn()`

**If AP > 0:** ✅ Continue to step 5

---

### 5. **Does MovementController Exist?**

**Look for:** `[PatrolState] [NPCName] has no MovementController!` (error)

**If error appears:**
- NPC GameObject is missing MovementController component
- Add MovementController component to NPC prefab
- Verify EntityFactory adds MovementController when spawning NPCs

**If NO error:** ✅ Continue to step 6

---

### 6. **Is PathfindingManager in Scene?**

**Look for:** `[PatrolState] PathfindingManager.Instance is null!` (error)

**If error appears:**
- PathfindingManager GameObject not in scene
- Add PathfindingManager to scene
- Ensure it's not disabled

**If NO error:** ✅ Continue to step 7

---

### 7. **Is HexGrid in Scene?**

**Look for:** `[PatrolState] Could not find HexGrid in scene!` (error)

**If error appears:**
- HexGrid GameObject not in scene
- Ensure HexGrid is created and active
- Check FindFirstObjectByType can locate it

**If NO error:** ✅ Continue to step 8

---

### 8. **Is Path Being Requested?**

**Look for:** `[PatrolState] [NPCName] requesting path from (X,Y) to (A,B)`

**If NOT appearing but no errors:**
- Check logs for: `skipping path request (isMoving: ..., hasRequested: ...)`
- If `isMoving: True` → NPC thinks it's already moving (check MovementController state)
- If `hasRequested: True` → Pathfinding request sent but not completed

**If appearing:** ✅ Continue to step 9

---

### 9. **Is Path Being Received?**

**Look for:** `[PatrolState] [NPCName] received path with X steps. Starting movement.`

**If NOT appearing:**
- Pathfinding failed (no valid path exists)
- Check for: `[PatrolState] [NPCName] cannot find path to waypoint`
- Verify waypoints are on walkable terrain
- Check pathfinding search limit (default: 100)

**If appearing:** ✅ Continue to step 10

---

### 10. **Is FollowPath() Succeeding?**

**Look for:** `[PatrolState] FollowPath returned: True`

**If returns False:**
- Path was null or empty when passed to MovementController
- Check MovementController.FollowPath() implementation
- Verify path is valid

**If returns True:** ✅ Continue to step 11

---

### 11. **Is MovementController Actually Moving?**

Check MovementController component in Inspector during Play mode:
- `IsMoving` property should be `True`
- `CurrentPath` should have coordinates
- `CurrentPathIndex` should be incrementing

**If IsMoving = False:**
- Movement coroutine didn't start
- Check MovementController.StartMovement() is called
- Check coroutine isn't being cancelled immediately
- Verify `MovementSpeed` > 0

**If IsMoving = True but NPC not visually moving:**
- Visual position not updating
- Check MovementCoroutine is running
- Verify transform position is being updated
- Check camera can see NPC

---

## Common Issues & Solutions

### Issue: NPCs have 0 AP every turn
**Solution:** 
- Check NPC ScriptableObject `MaxActionPoints` > 0
- Verify TurnManager calls `OnTurnStart()` before `TakeTurn()`
- Add log to NPCController.OnTurnStart() to confirm it's called

### Issue: PathfindingManager.Instance is null
**Solution:**
- Add PathfindingManager GameObject to scene
- Ensure it's active and has PathfindingManager component
- Check Awake() is setting Instance singleton

### Issue: HexGrid not found
**Solution:**
- Ensure HexGrid exists in scene
- Check it's active (not disabled)
- Verify HexGrid.Awake() is called before NPCs try to move

### Issue: Path request never returns
**Solution:**
- PathfindingManager queue might be stuck
- Check PathfindingManager coroutine is running
- Verify pathfinding algorithm completes (no infinite loops)
- Check Console for pathfinding errors

### Issue: FollowPath returns True but NPC doesn't move
**Solution:**
- MovementController coroutine not starting
- Check MovementController.IsMoving in Inspector during play
- Verify MovementSpeed > 0 in inspector
- Check for errors in MovementCoroutine

### Issue: No waypoints configured (PatrolState)
**Solution:**
- Villager should use WanderState (default)
- Guard/Goblin/Bandit need waypoints configured:
  - Open NPC ScriptableObject in Inspector
  - Find "Patrol Waypoints" section
  - Add at least 2 waypoints: (x,y), (a,b)
  - Set Patrol Mode (Loop or PingPong)

---

## Quick Test Setup

### Minimal Working Configuration:

1. **Scene Requirements:**
   - HexGrid (active, initialized)
   - PathfindingManager (active, singleton working)
   - TurnManager (active, managing NPC turns)
   - At least 1 NPC spawned

2. **NPC Requirements (check in Inspector):**
   - MovementController component attached
   - MaxActionPoints > 0 (in ScriptableObject)
   - Valid initial state (Wander, Patrol, Idle, etc.)
   - RuntimeData.Position matches actual grid position

3. **For PatrolState NPCs:**
   - At least 2 waypoints configured in ScriptableObject
   - Waypoints are on valid, walkable grid cells
   - Patrol Mode is set (Loop or PingPong)

4. **For WanderState NPCs:**
   - Wander radius > 0
   - Valid home position set

---

## Testing Procedure

1. **Start Unity in Play Mode**
2. **Open Console** (Ctrl+Shift+C)
3. **Filter logs:** Search for `[NPCController]`, `[PatrolState]`, or `[WanderState]`
4. **Watch for errors:** Red messages indicate component/scene issues
5. **Follow diagnostic steps 1-11 above**
6. **Check Inspector:** Select NPC GameObject and watch MovementController during play

---

## Expected Behavior When Working

```
[NPCController] ===== Villager TAKING TURN =====
[NPCController] State: Wander, AP: 5/5, Alive: True
[NPCController] Villager calling stateMachine.Update()...
[WanderState] OnUpdate for Villager. Waiting: False, AP: 5
[WanderState] New wander target: (12,8)
[WanderState] Villager requesting path from (10,10) to (12,8)
[PathfindingManager] Calculating path... (internal)
[WanderState] Villager received path with 3 steps. Starting movement.
[WanderState] FollowPath returned: True
[MovementController] Starting movement along path (internal)
[NPCController] Villager stateMachine.Update() completed
```

**Visual Result:** Villager smoothly walks from (10,10) → (11,10) → (11,9) → (12,8)

---

## Next Steps After Diagnosis

Once you identify the issue using the logs above:

1. **Report findings:** Share console logs showing where the flow breaks
2. **Fix configuration:** Add missing components, set AP, configure waypoints
3. **Fix code:** If logic error found, implement fix
4. **Re-test:** Verify NPCs now move correctly

---

*Created: November 19, 2025*
*Phase: 4.7 - NPC Movement Integration Debugging*
