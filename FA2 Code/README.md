# AR Sports Training System: Unity + AR Foundation (FA2)

Six scripts that implement the FA1 design: scan, place, train, detect, analyze, score and feedback.

| Script | Role (FA1 concept) |
|---|---|
| `GameManager` | AR Training Engine, 8-step workflow, scoring, adaptive difficulty |
| `PlacementController` | Environment tracking and registration (plane detection, tap to anchor zone) |
| `TargetController` | Virtual target and timing indicator (shrinking ring) |
| `TrajectoryRenderer` | Ball trajectory visualization |
| `FeedbackManager` | Aural and haptic feedback |
| `UIController` | HUD, sport/mode select, results |
| `SportConfig` | Sports database (one asset per sport) |

## 1. Project setup
1. Unity **2022.3 LTS** or **Unity 6**, 3D (URP) template. Android module installed.
2. Package Manager: install **AR Foundation**, **Google ARCore XR Plugin** (Android) and/or **Apple ARKit XR Plugin** (iOS), and **TextMeshPro** (import essentials when prompted).
3. Project Settings > XR Plug-in Management > tick **ARCore** (Android tab) / **ARKit** (iOS tab).
4. Project Settings > Player (Android): Minimum API 24+, Scripting Backend **IL2CPP**, ARM64 only, remove Vulkan from Graphics APIs if the camera is black.
5. Project Settings > Player > **Active Input Handling = Both** (scripts use `Input.GetTouch`).
6. URP: select your Renderer asset, **Add Renderer Feature > AR Background Renderer Feature**.

## 2. Scene hierarchy
- **AR Session** (GameObject > XR > AR Session)
- **XR Origin (Mobile AR)** (GameObject > XR). Add components: `AR Plane Manager` (Detection Mode: Horizontal), `AR Raycast Manager`, `PlacementController`. Assign Plane Manager and `ZonePrefab`.
- **GameManager** (empty object): add `GameManager`, `FeedbackManager` (+ AudioSource). Assign Sports array, Placement, Trajectory, UI, Feedback.
- **TrajectoryLine**: empty object with `LineRenderer` (width 0.02, unlit material) and `TrajectoryRenderer`.
- **Canvas** (Screen Space - Overlay): three panels and `UIController` on the Canvas.
  - HomePanel: 4 sport buttons (OnClick: `GameManager.SelectSport(0..3)`), 2 mode buttons (`SelectMode(0/1)`), Start button (`StartSession`).
  - HudPanel: TMP texts for stage, score, count, hint, big centre feedback text.
  - ResultPanel: TMP result text and a "Train again" button (`GameManager.BackToHome`).

## 3. Prefabs and assets
- **TargetPrefab** (one per sport, or reuse): root = 1-unit quad or sphere with a coloured material + `TargetController`; child **Ring** = 1-unit ring (flat torus or ring texture on a quad), assigned to `ring`. Optional sport icon texture on the root.
- **ZonePrefab**: a flat 1.5 x 1.5 m grid plane (semi-transparent) so the player sees where the zone is anchored.
- **SportConfig** x4 (Assets > Create > ARSports > Sport Config). Suggested values:
  - Cricket: width 0.4, height 0.3 to 0.9 (stump-like column)
  - Football: width 2.0, height 0.2 to 1.0 (goal corners)
  - Basketball: width 0.8, height 1.0 to 1.5 (hoop height)
  - Tennis: width 1.5, height 0.3 to 0.8
- Three short audio clips for Perfect / Good / Miss.

## 4. Build and test
- Build and Run to an ARCore-supported Android phone (camera permission is requested automatically).
- Flow: scan the floor, tap a plane to place the zone, tap targets as the orange ring meets the target edge.
- In the Editor, use the **XR Simulation** environment (Window > XR > AR Foundation > XR Environment) for plane detection. Touch input needs a device or Unity Remote.

## 5. For your FA2 report/demo
- Map each script to the FA1 sections (table above), and show the 8-step stage text on screen during the demo.
- Mention the limits: this build uses plane tracking and tap detection; real body or ball tracking (pose estimation, a smart-glasses SDK) is listed as future scope.

## Troubleshooting
- Black camera: check the AR Background Renderer Feature and the URP asset assigned in Graphics settings.
- Stuck on "scan": point at a textured floor in good light; `AR Plane Manager` must be on XR Origin.
- Nothing happens on tap: make sure `Placement.Enabled` is not blocked by UI covering the screen (set panels' Raycast Target off on full-screen images).
