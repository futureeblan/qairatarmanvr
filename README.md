# QairatArmanVR

VR game project developed by Qairat and Arman in Unity.

The project was created as an experiment with VR interaction, player movement
and gameplay mechanics in a virtual environment.

## Features

- VR support using OpenXR
- XR Interaction Toolkit
- VR hand interaction and animations
- Player movement and body tracking
- Interactive doors and hacking mechanics
- Enemy drone AI
- Patrol, search and alert states for the drone
- Player detection and chase system
- Glitch and visual effects
- Cinemachine camera system
- Sound effects and environment assets

## Technologies

- Unity 2023.1.22f1
- C#
- OpenXR
- XR Interaction Toolkit 2.5.4
- XR Hands
- Universal Render Pipeline (URP)
- Unity Input System
- Cinemachine

## Drone AI

The project contains a drone enemy with three states:

`Patrol → Searching → Alert`

The drone patrols between points, detects the player, remembers the last known
position and starts chasing the player when alerted.

## Project structure

- `Assets/` — game assets, scenes, scripts, sounds and VR content
- `Packages/` — Unity package configuration
- `ProjectSettings/` — Unity project settings

## Authors

- Arman
- Qairat
