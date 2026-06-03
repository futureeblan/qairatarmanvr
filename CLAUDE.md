# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity VR project ("My project") using the Universal Render Pipeline (URP) and OpenXR. It targets VR headsets via OpenXR and includes first/third-person non-VR controllers as well.

## Unity Version & Rendering

- Unity with URP 15.0.7 (`com.unity.render-pipelines.universal`)
- OpenXR loader configured in `Assets/XR/Loaders/OpenXRLoader.asset`
- URP settings in `Assets/Settings/`

## Key Packages (from `Packages/manifest.json`)

| Package | Version | Purpose |
|---|---|---|
| `com.unity.xr.interaction.toolkit` | 2.5.4 | VR interactions (grab, ray, direct) |
| `com.unity.xr.openxr` | 1.9.1 | OpenXR runtime |
| `com.unity.xr.hands` | 1.3.0 | Hand tracking |
| `com.unity.inputsystem` | 1.11.0 | New Input System |
| `com.unity.cinemachine` | 2.10.1 | Camera control |

## Asset Structure

- `Assets/texture/` — active working directory: main scene `dss.unity`, textures, and the custom `AnimateHandOnlnput.cs` script
- `Assets/yikyig.unity` — secondary scene
- `Assets/Scenes/SampleScene.unity` — Unity default sample scene
- `Assets/humanoidcontrol4_free/` — third-party full-body IK/avatar system (Passer VR HumanoidControl)
- `Assets/Oculus Hands/` — hand models, animations, materials, prefabs
- `Assets/Starter Assets/` — Unity first/third-person controller starter kit
- `Assets/Samples/XR Interaction Toolkit/2.5.4/` — XRI demo scene and starter prefabs
- `Assets/XRI/` — XR Interaction settings and configuration
- `Assets/PBS Materials Variety Pack/` — PBR material library with randomizer scripts

## Custom Scripts

### `Assets/texture/AnimateHandOnlnput.cs`
Drives hand animator parameters from VR controller input actions. Reads `PinchAnimatinAction` and `gripAnimationAction` (both `InputActionProperty`) each frame and sets `"Trigger"` and `"Grip"` float parameters on the attached `Animator`.

Note: the filename has a typo (`lnput` instead of `Input`) — match this exactly when referencing the file.

## How to Open and Build

This is a Unity Editor project — there is no CLI build command for normal development:

1. Open the project folder in Unity Hub (select the folder root where `ProjectSettings/` lives)
2. Unity will import all assets on first open
3. Build via **File → Build Settings** in the Unity Editor

To build from command line (CI/headless):
```bash
# Windows example — adjust Unity path to your installation
"C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe" \
  -batchmode -quit \
  -projectPath "C:\Users\215-cab\Documents\GitHub\Unity.proigec" \
  -buildTarget <Target> \
  -executeMethod BuildScript.Build \
  -logFile build.log
```

## XR Interaction Toolkit Conventions

- Input actions are wired via `InputActionProperty` (serialize in Inspector, not hardcoded)
- XR Rig setup follows XRI Starter Assets conventions — see `Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/`
- Hand animations use `"Trigger"` and `"Grip"` Animator float parameters (see `AnimateHandOnlnput.cs`)

## HumanoidControl Integration

`Assets/humanoidcontrol4_free/` contains the Passer VR HumanoidControl 4 Free package. It provides full-body IK targeting (head, hands, feet, hips) through its own `HumanoidControl` component and tracker system. Its Editor scripts live under `Assets/humanoidcontrol4_free/Editor/` and are in the `PasserVR.HumanoidControl.Editor` assembly.

## Assembly Definitions

| `.asmdef` | Purpose |
|---|---|
| `PasserVR.HumanoidControl` | HumanoidControl runtime |
| `PasserVR.HumanoidControl.Editor` | HumanoidControl Editor tooling |
| `Unity.StarterAssets` | First/third-person controller runtime |
| `Unity.XR.Interaction.Toolkit.Samples.StarterAssets` | XRI demo prefabs |
