# AR Classroom - DSA Tutor

Open `ARClassroom/` in Unity 6000.6 and open `Assets/Classroom/Scenes/Classroom.unity`, then press Play.

* 9 students sit at desks and follow the tutor with head and eyes; students raise a hand when a question is asked.
* Prof. Ada walks the front of the room, writes each DSA topic on the blackboard (arrays, linked lists, stack & queue, Big-O, BST, quick reference, motto), explains it to the class, asks a question and a student answers.
* Controls: `V` / `1-6` change the view, drag to orbit, wheel to zoom, click a student to have the tutor call on them, click the board for the next lesson.

Rebuild the scene from the imported models: menu **AR Classroom > Rebuild Classroom Scene** (`Assets/Classroom/Editor/ClassroomSceneBuilder.cs`).
All 3D assets are third-party, freely licensed - see `Assets/Classroom/LICENSES.md`. Nothing is modelled from primitives.
Rendering: URP, linear colour space, SSAO, ACES tonemapping, HDRI sky through the windows.
