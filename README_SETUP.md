# Bean Blaster Setup

Bean Blaster targets **Android for its first release**, with iOS deferred until Android is complete and accepted. The pinned development editor is **Unity 2022.3.62f3**, the final public 2022 LTS patch compatible with Unity Personal. Later 2022.3 Extended LTS releases require a paid Unity Industry or Enterprise license and must not be used for this project. `ANDROID_FIRST_RELEASE.md` overrides conflicting desktop-platform and input statements in the original specification.

> Current workspace status: the project is being constructed from `GAME_SPEC.md`. The full setup tool and playable scenes do not exist yet. This README will be expanded as each implementation phase passes its verification gate.

## Required packages

The project uses only the packages permitted by the specification:

- Input System: `com.unity.inputsystem` 1.7.x
- TextMesh Pro: `com.unity.textmeshpro` 3.0.x
- Unity Test Framework: `com.unity.test-framework` 1.1.x

UGUI is supplied by Unity and is used by the generated runtime UI. No third-party packages or Asset Store assets are permitted.

The manifest also enables Unity's built-in Audio, Physics 2D, and UI engine modules. These are parts of the Unity engine, not additional third-party packages.

## Manual Unity settings

These steps cannot be assumed from source generation alone:

1. Open the project with Unity 2022.3.62f3.
2. In **Edit → Project Settings → Player**, set **Active Input Handling** to **Input System Package (New)**.
3. Import **TMP Essentials** when TextMesh Pro prompts for them, or use **Window → TextMeshPro → Import TMP Essential Resources**.

## Android editor modules

The Unity 2022.3.62f3 installation includes the Android toolchain required by this project:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

The modules live under `Editor/Data/PlaybackEngines/AndroidPlayer`. Do not install another Unity editor version for Android work on this project.

## Android development build

Use **Tools → Game Setup → Android → Build Development APK**, or run this command from PowerShell with the Unity Editor closed:

```powershell
& 'C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -executeMethod BeanBlaster.Editor.AndroidBuildTools.BuildDevelopmentApk -logFile "$PWD\Logs\android-build.log" -quit
```

The APK is written to `Builds/Android/BeanBlaster-dev.apk`. Install it on a specifically selected device rather than relying on ADB's default when an emulator is also running:

```powershell
adb devices -l
adb -s <device-serial> install -r .\Builds\Android\BeanBlaster-dev.apk
```

## Full setup

When the Editor setup tooling is implemented, run:

**Tools → Game Setup → Run Full Setup**

The command will generate project settings, sprites, data assets, prefabs, UI, scenes, and Build Settings. It must be safe to run repeatedly. Until the setup-tool phases are complete, this menu command will not be available.

## Controls

The Android release uses landscape touch controls: a left movement stick, a right aim/fire stick, Jump, previous/next weapon, and Pause. The right stick fires while held and also activates airborne hover. This layout is designed for simultaneous movement, aiming/firing, and jumping.

The following bindings remain development controls for testing in the Unity Editor:

| Action | Keyboard and mouse | Gamepad |
|---|---|---|
| Move | A/D or Left/Right arrows | Left stick or D-pad |
| Jump | Space, W, or Up arrow | South button (A/Cross) |
| Fire / Hover while airborne | Left mouse button | Right trigger |
| Aim | Mouse | Right stick |
| Next weapon | E or mouse wheel up | Right shoulder |
| Previous weapon | Q or mouse wheel down | Left shoulder |
| Select weapon slots 1–5 | Number keys 1–5 | Not assigned |
| Pause | Escape or P | Start |

## Implementation assumptions

These decisions resolve details that are contradictory or not fully determined in `GAME_SPEC.md`. Section 1 of the specification remains authoritative.

1. **Project location:** this repository folder is the Unity project root. `Assets`, `Packages`, and `ProjectSettings` live directly beside `GAME_SPEC.md`.
2. **Editor patch:** development is pinned to Unity 2022.3.62f3 rather than an unspecified 2022.3 patch. Unity 2022.3.76f1 was rejected after live verification showed that it belongs to the paid Extended LTS channel.
3. **Economy service shape:** `EconomyService` is a static service over `SaveSystem.Current`. This avoids scene-lifetime ambiguity and gives tests a single state boundary.
4. **Killed enemies after death:** enemies killed during the current level attempt remain dead after checkpoint respawn. Restarting or reloading the level recreates them. Enemy death is not persisted between sessions.
5. **Brute coin splitting:** exact coin value and the eight-pickup cap take precedence over every pickup having value 10. Drops use value-10 pickups until the final allowed pickup, which carries the entire remaining value. Therefore a Brute drops seven pickups worth 10 and one worth 50. No reward value is discarded.
6. **Projectile pool exhaustion:** the 200-object limit applies per projectile prefab. If every pooled instance is active at the cap, spawning fails cleanly. The firing code does not consume ammunition or begin its cooldown until it has obtained a projectile.
7. **Input Action lifetime:** actions are created once in `Awake`, enabled in `OnEnable`, disabled in `OnDisable`, and disposed once in `OnDestroy`. Disposing in `OnDisable` would make a subsequently re-enabled component unusable.
8. **Save validation dependencies:** `SaveSystem` uses the loaded `WeaponDatabase` and `GameConfig` in normal runtime. Tests may inject or construct validation data so they do not depend on generated Unity assets.
9. **Save replacement fallback:** `File.Replace` is the preferred existing-save path. If the platform does not support it, the implementation uses a guarded backup/move fallback while retaining the last known-good backup.
10. **Level completion re-entry:** a `LevelManager` accepts completion only once per scene instance so overlapping exit callbacks cannot duplicate rewards.
11. **Attempt coin statistics:** “coins collected this attempt” includes enemy coin pickups and placed coin pickups actually collected during the attempt. Completion awards are displayed separately in the results breakdown.
12. **Generated content ownership:** the setup tool owns assets at its documented stable generated paths. It updates those assets on rerun; it does not delete unrelated user-created assets.
13. **Player respawn:** respawning restores the player object in place rather than reloading the scene, which is required to keep already-killed enemies dead.
14. **Direct scene play:** `Bootstrap` creates persistent services when a level is opened directly. Scene-specific references remain serialized on `LevelManager` and are not discovered every frame.
15. **Platform correction:** `ANDROID_FIRST_RELEASE.md` is authoritative wherever the original specification names desktop release targets or desktop-first input. Android is first; iOS follows. Desktop builds are not release deliverables.

## Manual acceptance checklist

- [ ] Fresh project → Run Full Setup → build an Android development APK → install it on the connected phone → game runs with no errors.
- [ ] Landscape UI respects the device safe area and display cutout.
- [ ] Multitouch supports moving, aiming/firing, and jumping at the same time.
- [ ] Force-stop and relaunch preserves all already-saved progression.
- [ ] Player can triple jump; a fourth jump is impossible until landing.
- [ ] Holding Fire in the air slows the fall to a gentle drift; releasing restores normal gravity.
- [ ] Aiming behind the player flips the body; the gun is never upside down.
- [ ] Each of the five weapons behaves as described in Section 6.2 of `GAME_SPEC.md`.
- [ ] Killing enemies drops coins that fly to the player; the coin counter updates.
- [ ] Buying a weapon persists after quitting and relaunching the game.
- [ ] Owned weapons and ammo remain after completing a level, dying, and restarting.
- [ ] Deleting `save.json` starts a fresh save with only Dual Pistols.
- [ ] No code path removes an owned weapon or resets ammo except **Reset Save**.

## Verification status

- Specification encoding: verified as valid UTF-8; apparent mojibake in earlier terminal output was a console-decoding problem.
- Exact editor version: pinned to 2022.3.62f3.
- Unity license: Unity Personal activated successfully.
- Local Unity editor: Unity 2022.3.62f3 is installed at `C:\Program Files\Unity 2022.3.62f3`.
- Local Unity import/compile: verified successfully with Unity 2022.3.62f3 on October 26, 2026; package resolution and script compilation exited cleanly with no C# errors or warnings.
- Android toolchain: Android Build Support, SDK, NDK, and OpenJDK verified under Unity 2022.3.62f3 on October 26, 2026.
- EditMode tests: 71 passed, 0 failed, 0 skipped on October 26, 2026. Coverage includes save recovery, permanent economy, shared health, projectile pooling, mobile input, safe areas, all five weapon mechanics, cross-roster ammunition, Ground Fire Zone timing, visual combat-target feedback, and save-safe development roster access.
- Android development build: `Builds/Android/BeanBlaster-dev.apk` built successfully on October 26, 2026 for ARMv7 and ARM64 using IL2CPP. The current five-weapon APK is 88,436,975 bytes.
- Phase 5 test scene: generated at `Assets/Scenes/Phase5_PlayerTest.unity` with a safe-area-aware multitouch HUD.
- Device installation and launch: verified on Samsung `SM_N986U1` through ADB on October 26, 2026. Package `com.beanblaster.game` installed for Android user 0, launched as `UnityPlayerActivity`, remained alive, and produced no Unity or Android runtime error log entries during the startup check.
- Android touch smoke: verified after correcting Active Input Handling from the legacy Input Manager to the Input System Package. ADB gestures moved the player off the center platform and produced a visibly airborne jump frame on the physical device.
- Runtime behavior: Android movement and jumping respond on the physical device. The development arena exposes all five weapons through PREV/NEXT with unlimited test ammunition without changing persistent ownership. A physical-device cycle-to-Napalm and aim/fire gesture resolved its burst against the first two targets with no Unity or Android runtime errors. Heavy Magnum piercing, Grenade Launcher falloff and feedback, Laser Splitter reflection, and Napalm burst/fire-zone mechanics pass their automated gates; enemy stun integration awaits the Phase 8 state machine.
