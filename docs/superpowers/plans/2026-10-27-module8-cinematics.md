# Implementation Plan: Module 8 Cinematography and Cutscene Animation

**Date**: 2026-10-27  
**Spec Reference**: [docs/superpowers/specs/2026-10-27-module8-cinematics-design.md](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/docs/superpowers/specs/2026-10-27-module8-cinematics-design.md)  
**Target Scene**: [Assets/Scenes/Module8_Cinematics.unity](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Scenes/Module8_Cinematics.unity)  

---

## Task 1: Install Cinemachine 2.10.3 in `Packages/manifest.json`
- **File**: [Packages/manifest.json](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Packages/manifest.json)
- **Goal**: Add `"com.unity.cinemachine": "2.10.3"` to dependencies to enable Cinemachine Virtual Cameras and Timeline track extensions.
- **Verification**: Run Roslyn compiler check on project C# files to ensure no package conflicts.

---

## Task 2: Generate Procedural Audio Clip
- **File**: `Assets/Audio/CutsceneJingle.wav`
- **Goal**: Generate a clean, pleasant 16-bit PCM WAV chime jingle (44.1 kHz, stereo or mono) using a short Python wave generator.
- **Verification**: Ensure the file exists, has valid WAV header, and Unity `.meta` is generated with an `AudioClip` GUID.

---

## Task 3: Implement `CutsceneTrigger.cs`
- **File**: [Assets/Scripts/CutsceneTrigger.cs](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Scripts/CutsceneTrigger.cs)
- **Goal**: Simple, robust cutscene trigger script:
  - Detects player entering trigger zone (`OnTriggerEnter`).
  - Plays assigned `PlayableDirector`.
  - Disables player movement / controller script (or input) during playback.
  - Re-enables player movement upon `director.stopped`.
  - Ensures one-shot triggering (`playOnce = true`).
- **Verification**: Compile with Roslyn csc via `run_command` and confirm exit code 0.

---

## Task 4: Create Scene `Module8_Cinematics.unity`
- **Source**: [Assets/Develop 3D Mobile Games in Unity/Assets/GamingWorkshop/Scenes/Activity8Start.unity](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Develop%203D%20Mobile%20Games%20in%20Unity/Assets/GamingWorkshop/Scenes/Activity8Start.unity)
- **Destination**: [Assets/Scenes/Module8_Cinematics.unity](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20(1)/Assets/Scenes/Module8_Cinematics.unity)
- **Goal**: Create dedicated scene preserving existing scenes intact.
- **Verification**: Verify new scene file size and valid Unity YAML structure.

---

## Task 5: Place Waitress NPC in `Module8_Cinematics.unity`
- **Prefab**: `SimplePeople_Waitress_White.prefab`
- **Position**: Near Donut Shop counter `(x: -0.5, y: 0.0, z: -1.2)` facing outward toward the entrance.
- **Goal**: Introduce the character as the subject of the cutscene.
- **Verification**: Check YAML hierarchy in `Module8_Cinematics.unity`.

---

## Task 6: Setup CinemachineBrain and Virtual Cameras in Scene
- **GameObjects**:
  - `Main Camera`: Add `CinemachineBrain` component.
  - `CM_vcam_Wide`: Framing donut shop exterior from high diagonal angle.
  - `CM_vcam_Dolly`: Camera panning toward counter with keyframed animation.
  - `CM_vcam_Waitress`: Close/medium portrait framing the Waitress NPC.
- **Goal**: Complete Virtual Camera setup for blending.
- **Verification**: Confirm component GUIDs match Cinemachine 2.x and transform values are positioned accurately.

---

## Task 7: Create `Module8_CutsceneTimeline.playable`
- **File**: `Assets/Timelines/Module8_CutsceneTimeline.playable`
- **Tracks**:
  1. `CinemachineTrack`: Cut/blend between `CM_vcam_Wide`, `CM_vcam_Dolly`, and `CM_vcam_Waitress`.
  2. `AnimationTrack` (Bonus): Keyframed camera pan curves for `CM_vcam_Dolly`.
  3. `ActivationTrack`: UI Intro Banner GameObject activation.
  4. `AudioTrack`: Cutscene chime jingle audio clip.
- **Verification**: Verify playable file YAML syntax, track IDs, and clip start/duration timestamps.

---

## Task 8: Wire up `Cutscene_Manager`, Trigger Collider, and UI Banner
- **In Scene**:
  - `Cutscene_Manager`: Contains `PlayableDirector` pointing to `Module8_CutsceneTimeline.playable`.
  - `CutsceneTriggerZone`: BoxCollider `isTrigger=true` placed in front of donut shop entrance with `CutsceneTrigger` script bound.
  - `UI_CutsceneBanner`: Canvas element displaying character intro text ("Sarah - Donut Shop Host"), initially disabled, bound to Timeline Activation Track.
- **Bindings**: Set scene bindings in `PlayableDirector` for all 4 tracks.
- **Verification**: Check binding array and target object references in scene YAML.

---

## Task 9: Full Build & Verification
- **Goal**: Verify all scripts compile with 0 warnings/errors, scene passes structural validation, and all challenges 1-5 + Bonus are fulfilled.
