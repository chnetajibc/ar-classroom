# Classroom Interior Pack - Animated Teacher & Students (URP)

Version 1.0.0 - Documentation

## 1. Overview

A complete, ready-to-run classroom for the Universal Render Pipeline: a 7.5 x 7.5 x 3.2 m room with a blackboard, nine student desks and chairs, a teacher's desk, windows, a door, ceiling fans and lights, shelves, plants, and small props. A teacher and nine students are placed in the room as Humanoid characters. The demo scene runs an interactive lesson on data structures and algorithms:

- the teacher (Prof. Ada) walks to the blackboard, writes the lesson notes in chalk, turns to the class and explains them;
- she asks a question, three students raise a hand, one is picked and answers, and the teacher reacts;
- the students follow the teacher with head and eyes and glance around;
- clicking a student makes the teacher call on them; clicking the blackboard skips to the next lesson.

Seven lessons are included (Arrays, Linked List, Stack & Queue, Big-O, Binary Search Tree, Quick Reference, Motto). Every lesson is plain data and easy to replace with your own.

**What is included**

- 7 architecture modules (wall, wall with window, wall with doorway, door with frame, floor tile, ceiling tile, blackboard) as FBX + prefabs
- 7 furniture prefabs (student desk and chair, teacher desk and armchair, wooden bookshelf, drawer cabinet, white shelf)
- 18 prop and lighting prefabs (laptop, books, pens, notebook, clipboard, clock, corkboard, plants, fire extinguisher, bags, bin, ceiling fan, fluorescent battens)
- 10 rigged Humanoid characters (1 teacher + 9 students) and the Universal Animation Library clips they use
- C# scripts (namespaced, assembly definitions, works with the old Input Manager and with the Input System package)
- One demo scene and one "complete classroom" prefab
- Editor tools to rebuild the demo scene and to apply the recommended URP settings

## 2. Requirements

- Unity 6 (6000.x). Developed and verified with Unity 6000.6.1f1 and URP 17.6.0 in the Editor on macOS.
- Universal Render Pipeline. All materials use URP/Lit. The package does not support the Built-in Render Pipeline or HDRP.
- Linear colour space is recommended (the demo lighting was tuned for it).
- Input: the legacy Input Manager, the Input System package, or both. The demo detects which one is active.

Not tested: Windows, Linux, mobile, consoles, WebGL, XR. The scene is not baked (real-time lighting only).

## 3. Quick start

1. Import the package. Make sure the project uses URP (create a project from the "Universal 3D" template, or assign a URP asset in Project Settings > Graphics).
2. Optional: **Tools > Realistic Classroom > Apply Recommended URP Settings** switches the project to Linear colour space and assigns the included URP asset (Forward+ renderer, screen-space ambient occlusion, soft shadows, HDR). It only changes settings when you confirm the dialog.
3. Open `Assets/RealisticClassroom/Scenes/Classroom_Demo.unity` and press Play.

| Control | Action |
|---|---|
| V / Tab, or the View button | next camera view |
| 1 - 6 | jump to a camera view (Back Left, Front Right, Teacher, Student, Board, Overview) |
| drag (left or right mouse) | orbit the camera around the current focus |
| mouse wheel | zoom |
| click a student | the teacher calls on that student, who answers |
| click the blackboard, or the "Next lesson" button | next lesson |

## 4. How the demo works

The demo root object is called `Classroom` and has six groups: `Shell` (walls, floor, ceiling, door), `Furniture`, `Props`, `Lighting`, `People` and `Systems`.

`Systems/ClassroomSystems` carries three components:

- **LessonDirector** runs the lesson loop as a coroutine. Per lesson it walks the teacher to the board, writes the notes (`BoardText`), walks to the centre of the room, explains, asks a question, lets three students raise their hands, picks one to answer and finishes with a reaction.
- **ClassroomUI** builds the screen overlay in code: lesson title, live captions, View and Next lesson buttons.
- **ClassroomInteraction** turns clicks into actions.

`Systems/Main Camera` has **ClassroomCameraRig** (view presets, orbit and zoom).

Each person has an **ActorRig** (name, height, head bone, Animator, IK component) and either **TutorBehaviour** or **StudentBehaviour**. The Animator Controllers (`Animations/Controllers/Tutor` and `Student`) contain one state per animation clip; the scripts cross-fade by state name. **HumanoidIK** sits on the Animator object and drives head and eye look-at, pins seated feet to the floor and lifts the right hand (raising a hand, writing on the board). The controllers have the IK pass enabled.

## 5. Writing your own lessons

Select `Systems/ClassroomSystems` and open the **LessonDirector** component. The `Lessons` list is empty by default, which makes the director use the built-in data-structures lessons. Add elements to the list to replace them. A lesson has:

| Field | Meaning | Limits |
|---|---|---|
| title | chalk heading | one line |
| accent | colour of the heading and code text | - |
| bullets | white bullet lines on the board | 4 lines |
| code | monospaced lines below the bullets | 6 lines |
| explanation | sentences the teacher says, one caption each | any number |
| question / answer | the question and the chosen student's reply | - |

The same can be done from code:

```csharp
using RealisticClassroom;

var director = FindAnyObjectByType<LessonDirector>();
director.lessons.Add(new Lesson {
    title = "Hash Maps",
    bullets = new[] { "- Key -> value lookup", "- Average O(1) get/set" },
    code = new[] { "var d = new Dictionary<string,int>();", "d[\"a\"] = 1;" },
    explanation = new[] { "A hash map turns a key into an array index." },
    question = "What is the average cost of a lookup?",
    answer = "Constant time, O(1)."
});
```

The captions and names come from the scripts; there is no audio in this package.

## 6. Using the prefabs

All prefabs are under `Assets/RealisticClassroom/Prefabs`. Every prefab root sits at position 0, rotation 0, scale 1 and every model is authored at 1 unit = 1 metre with the pivot at the bottom centre (ceiling items at the top centre, wall items at the centre), so they drop into any scene at the right size.

- `Architecture`: `WallPlain`, `WallWindow`, `WallDoorway`, `DoorLeafFrame`, `FloorTile`, `CeilingTile`, `Blackboard`. Wall modules are 2.5 m wide and 3.22 m high and use box-projected UVs (one plaster/floor texture repeat per module), so neighbouring modules tile without seams. Modules face +Z into the room; rotate them 0/90/180/270 degrees to build a room of any size. Ceiling tiles are floor tiles rotated 180 degrees around X.
- `Furniture`, `Props`, `Lighting`: single objects. `SchoolDesk` and `SchoolChair` are the real size; the demo scales the desk to 0.92.
- `Characters`: rigged prefabs (root + `Model` child with the Animator and Humanoid avatar). The child is rotated 180 degrees because the retargeted animations face -Z; the root faces +Z.
- `Classroom_Complete`: the whole demo room (people, lights, camera rig, UI systems) as one prefab. Drop it into an empty scene. The demo scene's sky, ambient light and post-processing volume are scene settings and are not part of a prefab.

**Adding another student**

1. Drag a character prefab into the scene.
2. Add `ActorRig` (fill in Name, Height, Animator, Head bone), `HumanoidIK` on the object that has the Animator, and `StudentBehaviour` on the root.
3. Assign the `Student` controller to the Animator.
4. Position the pelvis on the chair seat (the demo uses seat height 0.50 m + 0.085 m) and enable `Pin Feet To Floor` on `HumanoidIK`. Easiest: duplicate an existing student in the demo scene and swap the character prefab.
5. Add the new `StudentBehaviour` to the `Students` array of `LessonDirector`.

## 7. Characters and animation

The ten characters are low-poly, flat-shaded Humanoid characters (about 1,800 to 6,600 triangles each). Their skeletons were prepared for this package: arms are in a T-pose, the shoe bones follow the legs, and each FBX carries a valid Humanoid avatar generated by the importer. The animation clips come from the Universal Animation Library (Standard) and are imported as Humanoid clips with root motion baked into the pose, so they retarget to any Humanoid character.

Clips used by the demo: `Sitting_Idle_Loop`, `Sitting_Talking_Loop` (students); `Idle_Loop`, `Idle_Talking_Loop`, `Walk_Formal_Loop`, `Interact`, `Idle_FoldArms_Loop`, `Idle_No_Loop`, `Yes` (teacher). The library FBX files contain about 86 clips in total and can be used with the Humanoid rigs in your own controllers.

There is no facial animation, lip-sync or audio.

## 8. Rendering notes

- Lights: one soft-shadow directional "sun" through the windows, six ceiling point lights, a board wash spot and three window fill spots. The ceiling battens have emissive tubes. The demo relies on the Forward+ renderer for many per-pixel lights.
- Post-processing: a global Volume (`Settings/Classroom_PostProcessing`) with ACES tonemapping, bloom, vignette and colour adjustments. SSAO is added by the included URP renderer.
- Sky and reflections: a Poly Haven HDRI (2048 x 1024) shown through the windows; a real-time box-projected reflection probe (128 px) inside the room.
- Nothing is baked. If you bake lighting, mark the architecture and furniture static and generate lightmap UVs for your own workflow.
- The demo's post-processing, ambient light and skybox are stored in the scene; use `Tools > Realistic Classroom > Rebuild Demo Scene` to recreate them.

**Performance.** The demo is meant for desktop. The potted plant is a photogrammetry-quality model of about 119,000 triangles; delete or replace it (and the ceiling fan and drawer cabinet, 16,000 and 26,000 triangles) for lighter scenes. No LODs are included.

## 9. Rebuilding the demo scene

**Tools > Realistic Classroom > Rebuild Demo Scene** creates a new scene from the prefabs (room shell, furniture, props, lights, characters seated with the sitting animation evaluated in the Editor, camera rig, UI, post-processing) and overwrites `Scenes/Classroom_Demo.unity`. The layout constants are at the top of `Scripts/Editor/ClassroomSceneBuilder.cs`. The editor scripts are only compiled when URP is installed.

## 10. Technical details

<!--STATS-->

## 11. Third-party content

All third-party models, textures, animations and the HDRI are CC0 1.0 Universal (no attribution needed). See `Third-Party Notices.txt` in the package root for authors, source links and the list of what was used and modified.

## 12. AI disclosure

The C# scripts, the Editor tools (including the script that assembles the demo scene) and this documentation were written with the assistance of an AI coding assistant (Claude, by Anthropic), working under the publisher's direction and tested by compiling and running the package in the Unity Editor. No 3D models, textures, animations, audio or images in this package were generated by AI: the third-party models, textures, HDRI and animations were made by human artists and are released as CC0 (see Third-Party Notices.txt).

## 13. Known limitations

- The characters are stylised and low-poly with faceted shading; they are not photorealistic.
- Only ten distinct characters are included; students share the same two sitting animations with random offsets.
- No lip-sync, facial animation, audio or localisation. UI text uses Unity's built-in legacy font.
- URP only; tested on Unity 6000.6.1f1 only.
- No LODs, no baked lighting, not optimised for mobile.

## 14. Support

[PUBLISHER NAME] - [SUPPORT EMAIL] - [WEBSITE]
