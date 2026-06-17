# Android Shooter

A small 3D first-person wave-survival shooter for Android, built in **Unity 6 (6000.0.77f1)**. Everything — geometry, enemies, HUD, and the scene itself — is generated from C# using primitives, so the project ships with **no imported art assets**.

## Gameplay

- Survive waves of enemies that spawn around a walled arena and chase you.
- Hitscan gun (2 shots per enemy). Score goes up per kill; a Game Over → Restart flow handles death.

### Controls

| | Desktop | Android |
|---|---|---|
| Move | `WASD` | left-half on-screen joystick |
| Look | mouse | right-half drag |
| Fire | left mouse | on-screen **FIRE** button |

## Project layout

```
Assets/
  Scripts/        gameplay (runtime)
    Health.cs           HP container with Changed/Died events
    GameInput.cs        unified desktop + touch input
    PlayerController.cs  CharacterController FPS movement + look
    Gun.cs              hitscan raycast, tracer + muzzle flash
    Enemy.cs            chase AI, contact damage, death effect
    EnemySpawner.cs     ramping waves, builds enemies at runtime
    GameManager.cs      score, HP readout, game-over/restart
    TouchFireButton.cs  uGUI handler for the mobile fire button
  Editor/         tooling
    SceneBuilder.cs     builds the whole scene + HUD in code  (Tools ▸ Shooter ▸ Build Scene)
    BuildScript.cs      configures Android + packages the APK (Tools ▸ Shooter ▸ Build Android APK)
```

## Building

Open the project in Unity 6 and use the **Tools ▸ Shooter** menu, or build headless:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.0.77f1/Unity.app/Contents/MacOS/Unity"
# (re)generate the scene
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod SceneBuilder.Build -logFile build_scene.log
# package an installable APK -> Builds/AndroidShooter.apk
"$UNITY" -batchmode -quit -nographics -projectPath . -buildTarget android -executeMethod BuildScript.BuildApk -logFile build_apk.log
```

Output: `Builds/AndroidShooter.apk` — IL2CPP / ARM64, debug-signed, landscape, `com.vagabond.androidshooter`.
Install with `adb install -r Builds/AndroidShooter.apk`.
