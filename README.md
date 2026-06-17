# Android Shooter — USPSA Stage

A small 3D **practical-shooting (USPSA-style) stage** for Android, built in
**Unity 6 (6000.0.77f1)**. Everything — the bay, targets, HUD, and the scene
itself — is generated from C# using primitives, so the project ships with
**no imported art or audio assets** (even the start buzzer is a synthesized tone).

## The stage

A single freestyle course of fire, **Comstock** scoring, **Minor** power factor:

- **Paper targets** (USPSA cardboard) with **A / C / D** zones — **two hits each**, best two count. A = 5, C = 3, D = 1.
- **Steel poppers** — one hit knocks them down (5 pts).
- **No-shoot targets** (white, red border) — **do not hit them**: −10 each.
- **Misses** (unfilled required hits): −10 each.
- On the buzzer a **timer** starts; engage every target, and your run is scored as a **hit factor = points ÷ time**.

Flow: `MAKE READY → STANDBY → ` **BEEP** ` → run the stage → STAGE COMPLETE` (time, points, A/C/D breakdown, penalties, hit factor) → **RUN AGAIN**.

### Controls

| | Desktop | Android |
|---|---|---|
| Move | `WASD` | left-half on-screen joystick |
| Look | mouse | right-half drag |
| Fire | left mouse | **FIRE** button |
| Reload | `R` | **RELOAD** button |

The pistol runs a 10-round magazine — reload as you move between positions.

## Project layout

```
Assets/
  Scripts/                 gameplay (runtime)
    GameInput.cs             unified desktop + touch input (move/look/fire/reload)
    PlayerController.cs      CharacterController FPS movement + look
    Gun.cs                   hitscan, magazine + reload, reports hits to targets
    PaperTarget.cs           A/C/D zone scoring, 2 hits, no-shoot, bullet holes
    SteelTarget.cs           knock-down popper
    MatchManager.cs          buzzer, timer, hit-factor scoring, results, audio
    TouchFireButton.cs       uGUI handler for the mobile fire button
    TouchReloadButton.cs     uGUI handler for the mobile reload button
  Editor/                  tooling
    SceneBuilder.cs          builds the whole stage + HUD in code  (Tools ▸ Shooter ▸ Build Scene)
    BuildScript.cs           configures Android + packages the APK (Tools ▸ Shooter ▸ Build Android APK)
```

## Building

Open in Unity 6 and use the **Tools ▸ Shooter** menu, or build headless:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.0.77f1/Unity.app/Contents/MacOS/Unity"
# (re)generate the stage
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod SceneBuilder.Build -logFile build_scene.log
# package an installable APK -> Builds/AndroidShooter.apk
"$UNITY" -batchmode -quit -nographics -projectPath . -buildTarget android -executeMethod BuildScript.BuildApk -logFile build_apk.log
```

Output: `Builds/AndroidShooter.apk` — IL2CPP / ARM64, debug-signed, landscape, `com.vagabond.androidshooter`.
Install with `adb install -r Builds/AndroidShooter.apk`.
