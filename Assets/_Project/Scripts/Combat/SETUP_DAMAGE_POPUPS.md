# Damage Popup Setup Instructions

## Overview
The damage popup system is complete. You now need to create the prefab and pool GameObject in Unity.

## Step 1: Create DamagePopup Prefab

1. **Create Empty GameObject**
   - Right-click in Hierarchy → Create Empty
   - Name: `DamagePopup`

2. **Add Canvas (if not auto-created)**
   - If Unity doesn't auto-create a Canvas, add one manually
   - **Important Canvas Settings:**
     - Render Mode: **World Space**
     - Sorting Layer: **UI** (create this layer in Tags & Layers if it doesn't exist)
     - Order in Layer: **1000** (high value to render on top)
     - Rect Transform Position Z: **-10** (closer to camera than game objects)
     - Scale: **0.01, 0.01, 0.01** (for world space)

3. **Add TextMeshPro Component**
   - Select `DamagePopup` GameObject
   - Add Component → UI → Text - TextMeshPro (UGUI)
   - Configure:
     - Text: "99!"
     - Font Size: 36
     - Alignment: Center + Middle
     - Color: White
     - Rect Transform Width: 200, Height: 100

4. **Add DamagePopup Script**
   - Select `DamagePopup` GameObject
   - Add Component → Scripts → Combat → `DamagePopup`
   - The script will automatically find the TextMeshProUGUI component

5. **Create Prefab**
   - Drag `DamagePopup` from Hierarchy to `Assets/_Project/Prefabs/UI/` folder
   - Delete the GameObject from the Hierarchy (we only need the prefab)

## Step 2: Create DamagePopupPool GameObject

1. **Create Empty GameObject in Scene**
   - Right-click in Hierarchy → Create Empty
   - Name: `DamagePopupPool`
   - Position: (0, 0, 0) - doesn't matter, it's just a pool manager

2. **Add DamagePopupPool Script**
   - Select `DamagePopupPool` GameObject
   - Add Component → Scripts → Combat → `DamagePopupPool`

3. **Configure Pool Settings**
   - **Damage Popup Prefab**: Drag the `DamagePopup` prefab you created into this field
   - **Initial Pool Size**: 20 (default)
   - **Max Pool Size**: 50 (default)

4. **Save Scene**
   - The pool will now be ready when you enter Play Mode

## Step 3: Test in Play Mode

1. **Enter Play Mode**
2. **Right-click an NPC to attack**
3. **You should see:**
   - Floating damage number appear above the NPC
   - Number floats upward and fades out
   - White text for normal damage
   - Orange text with "!" for critical hits (15% chance)
   - Larger text for critical hits

## Optional: Create Prefabs Folder

If `Assets/_Project/Prefabs/UI/` doesn't exist:
1. Right-click in Project → Create → Folder
2. Create folder structure: `Assets/_Project/Prefabs/UI/`
3. Move the `DamagePopup` prefab into this folder

## Troubleshooting

### No popups appear:
- Check that `DamagePopupPool` exists in the scene
- Check that the prefab is assigned to the pool
- Check Console for errors
- Verify `HealthComponent.spawnDamagePopups` is enabled (default: true)

### Popups appear but look wrong:
- Check TextMeshPro font size (recommended: 24)
- Verify sorting layer is set to UI or higher
- Check `DamagePopup.normalColor/criticalColor/healingColor` settings

### Pool exhausted warnings:
- Increase `maxPoolSize` in DamagePopupPool
- This happens if many entities take damage at once

## Next Steps

After testing damage popups:
1. Phase 5.5.2 - Health Bar UI
2. Phase 5.5.3 - Attack Visual Feedback (hex highlights, line renderer)
3. Phase 5.5.4 - Polish (camera shake, particles, sound)
