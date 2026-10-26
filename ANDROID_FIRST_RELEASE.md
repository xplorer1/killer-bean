# Android-First Release Contract

This document overrides every platform or input statement in `GAME_SPEC.md` that conflicts with an Android-first mobile release.

## Release targets

1. The first supported release platform is **Android**.
2. **iOS follows after the Android release is complete and accepted.** iOS is not part of the first-release completion gate.
3. Windows, macOS, and Linux standalone builds are development conveniences only. They are not release targets.
4. The game is landscape-only on phones and tablets.
5. Gameplay code must remain platform-neutral so the later iOS port does not require rewriting movement, combat, saving, or economy systems.

## Input contract

Touch is the primary release input. The Android HUD will provide:

- A left-side virtual stick for horizontal movement.
- A right-side aim/fire stick. Dragging sets the aim direction; holding it engages automatic fire and airborne hover.
- A jump button.
- Previous-weapon and next-weapon buttons.
- A pause button.

Keyboard, mouse, and gamepad bindings remain available for Editor development and accessibility testing, but they are not the Android release control contract. Direct number-key weapon selection is development-only.

Touch controls must support simultaneous movement, aiming/firing, and jumping. Gameplay systems consume a platform-neutral input state; they must not call Android APIs directly.

## Android build contract

- Unity version remains pinned to **2022.3.62f3**.
- Required editor modules are Android Build Support, Android SDK & NDK Tools, and OpenJDK.
- The first distributable is a development APK for device acceptance. A release AAB and signing configuration are later release-engineering gates.
- The application must handle pause, focus loss, resume, and process restart without losing already-persisted ownership, coins, ammo, or settings.
- UI must respect display cutouts and safe areas.
- A physical Android device is the primary gameplay acceptance target. An emulator is useful for installation, lifecycle, resolution, and basic UI checks, but it is not evidence that multitouch controls feel correct.

The exact minimum Android API, target API, package identifier, signing identity, and store metadata remain release configuration decisions. They must be resolved before the release-build phase, not guessed inside gameplay code.

## Revised verification gates

Any specification requirement that says "standalone build" now means an Android device build unless it explicitly describes an Editor-only development check.

Android acceptance requires:

- A clean APK build and `adb` installation.
- Correct landscape orientation and safe-area layout.
- Simultaneous move, aim/fire, and jump input on a physical device.
- Correct pause/resume behavior when the app loses focus.
- Save persistence after force-stop and relaunch.
- No gameplay dependency on keyboard, mouse, or hover-capable pointer input.
