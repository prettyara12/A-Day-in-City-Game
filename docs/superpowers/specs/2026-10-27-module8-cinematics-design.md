# Design Specification: Module 8 Cinematography and Cutscene Animation

**Date**: 2026-10-27  
**Module**: Module 8 — Cinematography and Cutscene Animation  
**Scene**: [Assets/Scenes/Module8_Cinematics.unity](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Scenes/Module8_Cinematics.unity)  
**Reference Video**: [Module8-ITG(unity) by Jonald Filemon Kaparang](https://youtu.be/lUcakafVNBI?si=6k4-cUv0QFDrn9_5)  

---

## 1. Objectives & Challenges

This implementation covers all challenge requirements from Module 8 without exceeding scope:
1. **Storyboard Planning**: Planned cutscene structure before creation with visual shot progression.
2. **Virtual Cameras**: Multiple Cinemachine Virtual Cameras configured for cinematic framing and smooth blending.
3. **Timeline Cut & Blend**: Unity Timeline asset cutting and blending between Virtual Cameras matching the storyboard.
4. **Track Diversity**: Mix of Cinemachine Track, Animation Track, Activation Track, and Audio Track.
5. **Character Introduction Trigger**: Scripted trigger zone that plays the Timeline when the player discovers/introduces the new character into the game.
6. **Bonus Challenge**: Keyframed animation of camera movement directly in Timeline.

---

## 2. Storyboard Design

The cutscene runs for **6.0 seconds** in front of the Donut Shop:

| Shot | Time | Camera | Movement / Focus | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Shot 1** | 0.0s – 1.8s | `CM_vcam_Wide` | Static wide angle | Establishing shot showing Donut Shop exterior and street entrance. |
| **Shot 2** | 1.8s – 3.8s | `CM_vcam_Dolly` | Keyframed dynamic pan & tilt (Bonus Challenge) | Camera glides smoothly closer along an arc toward the entrance counter. |
| **Shot 3** | 3.8s – 5.5s | `CM_vcam_Waitress` | Medium portrait shot | Focuses on Waitress NPC (`SimplePeople_Waitress_White`) standing at the shop counter, waving/greeting. Banner activates: *"Donut Shop - Meet Sarah"*. |
| **Shot 4** | 5.5s – 6.0s | Blend to Gameplay | Cinemachine ease blend | Smooth blend back to the player's active gameplay camera. Controls resume. |

---

## 3. Architecture & Components

### 3.1 Cinemachine Virtual Cameras
- **Main Camera**: Equipped with `CinemachineBrain` (Default Blend: Ease In Out, 0.8s).
- **`CM_vcam_Wide`**: High-angle wide view facing `donut_shop` (Priority 10).
- **`CM_vcam_Dolly`**: Mid-height moving camera with keyframed Timeline animation (Priority 10).
- **`CM_vcam_Waitress`**: Medium shot framing the Waitress NPC at `donut_shop` front counter (Priority 10).

### 3.2 Timeline Structure (`Module8_CutsceneTimeline.playable`)
- **Cinemachine Track**:
  - `CinemachineShot` 1: `CM_vcam_Wide` (0.0s – 2.0s).
  - Blend zone (1.5s – 2.0s) into `CM_vcam_Dolly`.
  - `CinemachineShot` 2: `CM_vcam_Dolly` (1.5s – 4.0s).
  - Blend zone (3.5s – 4.0s) into `CM_vcam_Waitress`.
  - `CinemachineShot` 3: `CM_vcam_Waitress` (3.5s – 5.5s).
- **Animation Track (Bonus Challenge)**:
  - Bound to `CM_vcam_Dolly`. Keyframed position curve panning forward and upward toward the shop window.
- **Activation Track**:
  - Bound to `UI_CutsceneBanner` GameObject. Activates from 3.8s to 5.8s during the waitress reveal.
- **Audio Track**:
  - Bound to `AudioSource` on Cutscene Manager. Plays a chime jingle ([Assets/Audio/CutsceneJingle.wav](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Audio/CutsceneJingle.wav)) starting at 0.1s.

### 3.3 Trigger & Control Script (`CutsceneTrigger.cs`)
- Attached to an invisible trigger volume in front of the Donut Shop.
- **Behavior**:
  - When `Player` (`CompareTag("Player")`) enters trigger:
    1. Checks `hasPlayed` flag (plays once).
    2. Disables player input/movement controller so the player doesn't wander off during the cinematic.
    3. Plays `PlayableDirector`.
    4. Subscribes to `director.stopped` event: restores player controls upon cutscene completion.
- Under `/ponytail`: Zero unnecessary bloat; standard `PlayableDirector` and `MonoBehaviour` events.

---

## 4. Asset Dependencies
- `com.unity.cinemachine`: `2.10.3` (UPM package, existing in local cache).
- `com.unity.timeline`: `6.6.0` (already installed in `Packages/manifest.json`).
- Character Prefab: [Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Develop%203D%20Mobile%20Games%20in%20Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab).
- Audio Clip: Procedural high-fidelity chime jingle generated to [Assets/Audio/CutsceneJingle.wav](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Audio/CutsceneJingle.wav).
