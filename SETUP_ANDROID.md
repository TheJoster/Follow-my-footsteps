# Android Build Setup (Unity 6000.2.12f1)

This guide covers the tooling and configuration required to build **Follow My Footsteps** for Android devices alongside the existing Windows target.

## Recommended Android Toolchain

| Component | Version | Notes |
|-----------|---------|-------|
| **Android SDK Platform** | 34 (Android 14) | Unity 6000.2.12f1 ships with platform 34 support and Play Store currently requires target API level 34. |
| **Android Build Tools** | 34.0.0 | Installed automatically with the SDK Platform 34 module. |
| **Android NDK** | r23c (23.2.8568313) | Unity 6/6000 defaults to r23c; use the bundled version for IL2CPP compatibility. |
| **JDK** | OpenJDK 17.0.8.1 | Included with Unity 6000.2.12f1; no external install required. |
| **Gradle** | 7.5 (bundled) | Unity manages Gradle internally—keep "Gradle Installed with Unity" enabled. |

> ✅ **Best practice:** Install the "Android Build Support" module via Unity Hub so the correct SDK, NDK, and JDK versions are provisioned automatically.

## Installation Steps

1. **Unity Hub → Installs → (⋯) on Unity 6000.2.12f1 → Add Modules.**
   - Enable **Android Build Support** and make sure all three sub-options are selected: `Android SDK & NDK Tools`, `OpenJDK`, and `Android Logcat`.
2. If you prefer manual management, download the exact versions listed above and point Unity to them via **Edit → Preferences → External Tools**. Keep `Gradle Installed with Unity` checked.
3. After installation, restart Unity and confirm the configured paths in **Preferences → External Tools**. They should reference the Unity Hub cache (e.g., `C:\Program Files\Unity\Hub\Editor\6000.2.12f1\Editor\Data\PlaybackEngines\AndroidPlayer`).

## Player Settings for Android

1. Open **File → Build Settings → Android** and click **Switch Platform**.
2. In **Player Settings → Other Settings**:
   - **Scripting Backend:** `IL2CPP`
   - **Target Architectures:** `ARM64` (and `ARMv7` if you need legacy 32-bit support).
   - **Minimum API Level:** `Android 8.0 (API 26)` (matches project’s mobile scope).
   - **Target API Level:** `Android 14 (API 34)`.
   - **Graphics API:** `OpenGLES3` (URP profile already tuned for mobile).
   - **Multithreaded Rendering:** Enabled.
3. In **Player Settings → Publishing Settings** configure a keystore:
   - Development builds can use the default debug keystore.
   - For release builds create a new keystore (`Create New Keystore` button) and store the credentials securely. Document alias/passwords in your private CI secrets vault.
4. Check **Resolution and Presentation**:
   - Default orientation: `Landscape Left` (matches current UI layout).
   - Enable **Use 32-bit Display Buffer** for better color depth on modern devices.

## Input & UX Notes

- **Tap-to-preview, tap-to-confirm:** The first tap highlights a hex, previews movement/tooltips, and the second tap on the same tile confirms the action.
- **Drag gestures:** Two-finger drag pans the camera; single-finger drag outside of interaction sequences is ignored.
- **Zoom:** Pinch to zoom is mapped to the existing camera zoom system.

Ensure QA verifies these gestures on real hardware (pixel density can impact tooltip placement—see below).

## Build & Deployment Checklist

1. Switch platform to Android and let Unity re-import assets.
2. Set **Build System** to `Gradle` (default) in **Build Settings**.
3. Choose either **Build** (generates an `.apk`/`.aab`) or **Build And Run** (deploys to connected device).
4. Before distributing an `.aab`, run **Build → App Bundle (Google Play)** to satisfy Play Store requirements.
5. Validate touch interactions and performance on a range of devices:
   - **Low-end (3 GB RAM)** to confirm pooling keeps the simulation stable.
   - **Mid/high-end (6 GB+ RAM)** for reference performance targets.
6. Record any device-specific tweaks in `NPC_MOVEMENT_DIAGNOSTIC.md` or a new QA note if required.

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|--------------|-----|
| Unity cannot find the SDK/NDK after switching platform | Paths not assigned in External Tools | Re-run Unity Hub module installation or point to the cached directory manually. |
| Build fails with `NDK r21/22 unsupported` | Legacy NDK on PATH is being picked up | Ensure **NDK Installed with Unity** is active and remove custom `ANDROID_NDK_HOME` env vars. |
| Gradle build timeout | Antivirus blocking Gradle cache | Exclude `%USERPROFILE%\.gradle` and the project folder from realtime scanning on Windows. |
| Touch gestures offset | Canvas scaling mismatch | Verify `TooltipUI` canvas uses `Screen Space - Overlay` and match reference resolution of `1920×1080` with `Match = 0.5`.

For automated CI builds, mirror these versions in your pipeline container or provisioning scripts to avoid version drift.
