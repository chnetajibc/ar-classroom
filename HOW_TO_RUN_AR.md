# AR Classroom — DSA Tutor · How to Run (from zero AR knowledge)

You do **not** need to know AR. There are exactly **2 modes**:

| Mode | What it is | What you need |
|---|---|---|
| **A. Desktop Preview (default)** | Classroom floats in front of the camera inside Unity's Play window. Full interactions with mouse. **This is what "click Play" runs.** | Just Unity Hub + Unity 6000. No phone. |
| **B. Real phone AR** | Classroom anchors onto your real floor/table through the phone camera. | An AR-capable Android phone (or iPhone) + USB cable. Same project, one extra build step. |

Both modes use the **same scene and same code**. Mode A works even if AR packages haven't finished installing.

---

## 0. What was built for you

```
ARClassroom/
  Assets/Scenes/ARClassroom.unity   ← open this, press Play
  Assets/Scripts/
    ClassroomBuilder.cs    classroom, desks, 9 seats (6 students + 3 empty), laptops, board wall
    BoardController.cs     5 DSA topics: Arrays, Linked List, Stack/Queue, Big-O, BST
    TutorController.cs     Prof. Ada: patrols the front lane, pauses to teach, wave, point-at-board, speech bubble
    StudentController.cs   typing animation, glowing laptop screens, name tags
    ARClassroomPlacer.cs   AR tap-to-place + Desktop auto-place fallback (why Play "just works")
    InteractionManager.cs  click/tap raycasting
    ClassroomUI.cs         top bar, buttons, info popup, status line (all built in code)
  Assets/Editor/AutoSetup.cs  menu: AR Classroom / 1-2-3
  Packages/manifest.json      AR Foundation 5.1.5 + ARCore + ARKit + XR Management
```

**Scene contents:** floor + rug + front wall with banner, 4.8 m DSA board teaching 5 topics,
3×3 desks = **9 seats, 6 occupied (each with an open glowing laptop), 3 empty** (translucent chair + "— empty —" tag),
tutor on a blue disc with pointer stick + name tag + speech bubble, directional light.

**Basic interactions (both modes):**
- Click/tap **board** → next DSA topic (or use `Next Topic ▶` button, `N` key)
- Click/tap **tutor** → wave + explains current topic in speech bubble
- Click/tap **student** → highlight (laptop glows yellow) + info popup + hop
- Buttons: `◀ Topic`, `Next Topic ▶`, `🎓 Tutor Explains`, `⟳ Rotate`, `＋/－ Size`, `Reset`
- Desktop extras: **right-drag** orbits classroom, **mouse wheel** zooms, `R` resets placement

---

## 1. First-time setup (5 minutes, once)

1. **Install Unity Hub** → https://unity.com/download
2. In Hub → Installs → install **Unity 6000.0.39f1 (LTS)** with modules:
   - **Android Build Support** (includes SDK/NDK tools + OpenJDK) — needed only for phone builds; harmless to include now
   - **iOS Build Support** — only if you will build to iPhone (requires a Mac — you have one)
3. In Hub → Open → **Open** → select the `ARClassroom` folder (the one containing `Assets/`).
   - First open takes 3–10 min: Unity imports + auto-installs AR Foundation from `Packages/manifest.json`. Wait for the spinner bottom-right to stop.
   - If you see "Unsafe code" or API update prompts → accept defaults.
4. In Project window open **Assets → Scenes → ARClassroom** (double-click). You should see `Main Camera`, `ClassroomRoot`, `ARPlacer`, `InteractionManager`, `ClassroomUI` in Hierarchy.
5. (Optional safety net) Menu: **AR Classroom → 1. Setup Scene (click me first)**. It self-heals any missing object.

> If the Console shows a red error about `ARRaycastHit` / `TrackableType`: ignore it in Desktop mode — real-AR code paths are reflection-guarded and only activate on device. Or run step 2 below to add real AR objects after packages finish installing.

---

## 2. Mode A — Just press Play (Desktop Preview, no phone)

1. Open scene `Assets/Scenes/ARClassroom.unity`.
2. Press **▶ Play** at the top.
3. After ~1 second the classroom **auto-spawns, staged at the origin**, framed by a
   fixed front-left camera: tutor on the right, students across the middle.
4. Try: click the **board** (topics cycle), click the **tutor** (she waves + explains), click any **student** (popup), use bottom-bar buttons.
5. Press Play again to stop.

**Why this "just works":** `ARClassroomPlacer` looks for AR planes via reflection.
In the Editor there are no planes, so after 1 s it falls back to `PlaceDesktop()` —
classroom at the origin, matched to the pre-framed scene camera. No headset, no phone, no build needed.
This is the mode to demo to a client on a laptop / screen-share.

**Camera not seeing the classroom?**
- Make sure the **Game** tab (not only Scene tab) is visible when you press Play.
- Press `R` or the `Reset` button to re-stage it at the origin.
- Check Console for `[Preview] No AR planes — classroom auto-placed…`.

---

## 3. Mode B — Real AR on a phone

### 3a. Enable real-AR objects (once, after packages installed)

1. Wait until Package Manager shows `AR Foundation 5.1.5`, `ARCore`, `ARKit` installed (Window → Package Manager → Packages: In Project).
2. Menu: **AR Classroom → 2. Add Real-AR Objects**.
   - Creates `AR Session` + `XR Origin` (camera + `ARPlaneManager` + `ARRaycastManager`).
   - From then on, on-device taps place the classroom on detected planes; in-Editor behavior is unchanged (still auto-places).

### 3b. Android build (most common)

1. **File → Build Profiles** (Unity 6) → select **Android** → Switch Platform (one-time, ~2 min).
2. Still in Build Profiles → Player Settings:
   - **Minimum API Level: Android 7.0 (24)+**, Target: Automatic
   - **XR Plug-in Management → Android tab → check ARCore** (enabled by `com.unity.xr.management`)
   - Uncheck **Auto Graphics API**, keep GLES3 + Vulkan is fine; enable **ARM64**, disable ARMv7 for speed
   - **Package Name**: e.g. `com.yourcompany.arclassroom`
   - Camera permission text is auto-added by AR Foundation.
3. Enable **Developer Mode + USB debugging** on the phone, plug in USB → accept the RSA prompt.
4. **File → Build And Run**. Unity builds the APK (~1–3 min first time) and launches it.
5. On phone: **slowly pan across the floor/table** until a plane is found → **tap the floor** → classroom appears → tap board/tutor/students exactly like desktop.

### 3c. iPhone build (needs Xcode, you have macOS)

1. Build Profiles → **iOS** → Switch Platform.
2. Player Settings → **XR Plug-in Management → iOS → check ARKit**; set **Camera Usage Description** (`NSCameraUsageDescription`: "AR classroom needs camera").
3. **Build** (produces an Xcode project folder) → open `.xcworkspace` in Xcode → set Team/certificates → **Run** on device.

### Phone troubleshooting

| Symptom | Fix |
|---|---|
| Black camera / no tracking | Accept camera permission; relaunch; ARCore/ARKit plug-in must be ticked for that platform |
| No plane, can't place | Move phone slowly, good lighting, textured floor (blank white floors fail); tap only after you see plane visualization |
| Classroom huge/tiny | Use `＋/－ Size` buttons; it persists per session |
| Build fails "SDK/NDK missing" | Hub → Installs → ⋮ on 6000.0.39f1 → Add Modules → Android SDK/NDK |

---

## 4. Customizing (for you / the client)

- **Empty vs occupied seats:** select `ClassroomRoot` → Inspector → `Empty Seats` array. Default `{2,5,7}` = 3 empty of 9. Set to `{}` for full class, add indices 0–8 for more empties.
- **DSA content:** select `ClassroomRoot → Board` → Inspector → `Topics` (5 entries, each Title/Body/Code/Accent color). Edits apply live in Play mode.
- **Tutor name/lines:** `Tutor` object → `Tutor Controller` → `tutorName`; explanation lines live in `TutorController.ExplainCurrentTopic()`.
- **Class size/scale:** `ARPlacer` → `Spawn Distance` (desktop) / `Spawn Scale`.
- **Adding your own 3D models later:** replace the primitive-built methods in `ClassroomBuilder` with prefab instantiations — interaction code keys off the `BoardController` / `TutorController` / `StudentController` components, not the meshes.

---

## 5. "It doesn't work" checklist (run top→down)

1. Correct folder opened? (`ARClassroom` containing `Assets/`, not its parent `try-2`.)
2. Correct scene open? (`Assets/Scenes/ARClassroom.unity` — Hierarchy shows 6 root objects.)
3. Pressed Play with **Game** tab visible?
4. Console red errors? Double-click them — 99% are "AR Foundation not yet installed → wait + retry menu step 2". Desktop preview ignores them.
5. Still stuck? Menu **AR Classroom → 1. Setup Scene**, then Play again.

---

## 6. Demo script for the client (60 seconds)

1. Press Play. *"This is our AR classroom — right now in desktop preview; on a phone it sits on your real table."*
2. Click board twice. *"The board teaches DSA — arrays, linked lists, stacks, Big-O, BSTs."*
3. Click tutor. *"Prof. Ada explains whatever is on the board."*
4. Click two students. *"Every student has a laptop; empty seats are marked — three are free here."*
5. Drag/zoom + `＋ Size`. *"And it's fully interactive — rotate, resize, reset for any room."*
6. Close: *"Phone build is the same project — Build And Run, tap the floor, done. Steps are in section 3."*
