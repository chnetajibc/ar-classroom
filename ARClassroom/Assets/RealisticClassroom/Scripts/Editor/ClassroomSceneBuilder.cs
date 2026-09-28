using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealisticClassroom.EditorTools
{
    /// <summary>
    /// Rebuilds the demo scene from the prefabs in this package: room shell, furniture, props, lights, the tutor, nine seated students,
    /// camera rig, UI and post-processing. Everything is placed from the included models - nothing is built from primitives.
    /// </summary>
    public static class ClassroomSceneBuilder
    {
        public const string Root = "Assets/RealisticClassroom";
        public const string ScenePath = Root + "/Scenes/Classroom_Demo.unity";
        const string Prefabs = Root + "/Prefabs/";

        // ---- room (metres). Students face +Z; the blackboard is on the +Z wall.
        const float Half = 3.75f;                         // interior half extent in X and Z
        const float Module = 2.5f;                        // width of one wall/floor module
        const float WallHeight = 3.225f;                  // module height
        const float Span = 2.5833f;                       // floor/ceiling/side-wall pitch (3.3% wider so slabs overlap the walls and seal light leaks)
        const float SeatTop = 0.50f;                      // SchoolChair seat surface
        const float HipRise = 0.085f;                     // pelvis centre above the seat surface
        const float ChairBack = 0.75f;                    // desk centre -> chair centre
        const float DeskScale = 0.92f;
        const float DeskTop = 0.81f;
        static readonly float[] RowZ = { 1.3f, -0.2f, -1.7f };
        static readonly float[] ColX = { -1.9f, 0f, 1.9f };

        class Seat { public string prefab, name; public float height; public int row, col; }

        static readonly Seat[] Students =
        {
            new Seat { prefab = "WomanA",          name = "Mia",   height = 1.56f, row = 0, col = 0 },
            new Seat { prefab = "ManA",            name = "Aarav", height = 1.64f, row = 0, col = 1 },
            new Seat { prefab = "HoodieCharacter", name = "Leo",   height = 1.60f, row = 0, col = 2 },
            new Seat { prefab = "CasualCharacter", name = "Kabir", height = 1.62f, row = 1, col = 0 },
            new Seat { prefab = "WomanTank",       name = "Zara",  height = 1.54f, row = 1, col = 1 },
            new Seat { prefab = "ManLongSleeves",  name = "Arjun", height = 1.66f, row = 1, col = 2 },
            new Seat { prefab = "WomanCasual",     name = "Nina",  height = 1.55f, row = 2, col = 0 },
            new Seat { prefab = "ManB",            name = "Dev",   height = 1.60f, row = 2, col = 1 },
            new Seat { prefab = "BeachCharacter",  name = "Sara",  height = 1.57f, row = 2, col = 2 },
        };

        static Transform shell, furniture, props, lighting, people, systems;
        static readonly List<StudentBehaviour> studentComponents = new List<StudentBehaviour>();
        static Transform boardWritePoint, boardStandPoint, stageCenter, classFocus;
        static BoardText boardText;
        static TutorBehaviour tutorComponent;

        [MenuItem("Tools/Realistic Classroom/Rebuild Demo Scene")]
        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            studentComponents.Clear();
            var root = new GameObject("Classroom").transform;
            shell = Group(root, "Shell"); furniture = Group(root, "Furniture"); props = Group(root, "Props");
            lighting = Group(root, "Lighting"); people = Group(root, "People"); systems = Group(root, "Systems");

            BuildShell();
            BuildFurniture();
            BuildProps();
            BuildDeskItems();
            BuildLighting();
            BuildPeople();
            BuildSystems();
            BuildEnvironment();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "Built " + ScenePath;
        }

        /// <summary>Saves the whole room (people, lights, camera rig, UI) as one prefab for drag-and-drop use.</summary>
        public static string SaveCompletePrefab()
        {
            var root = GameObject.Find("Classroom");
            var path = Prefabs + "Classroom_Complete.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // ------------------------------------------------------------------ helpers
        static GameObject Put(Transform parent, string prefab, string name, Vector3 pos, float yaw = 0f, Vector3? scale = null, Quaternion? extra = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + prefab + ".prefab");
            if (asset == null) throw new FileNotFoundException("Missing prefab " + prefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * (extra ?? Quaternion.identity);
            go.transform.localScale = scale ?? Vector3.one;
            return go;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Moves a wall-mounted object so its back face touches the wall plane (axis 0 = X, 2 = Z).</summary>
        static void SnapBack(GameObject go, int axis, float plane, bool backIsMax)
        {
            var b = WorldBounds(go);
            float back = backIsMax ? b.max[axis] : b.min[axis];
            var d = Vector3.zero;
            d[axis] = plane - back;
            go.transform.position += d;
        }

        // ------------------------------------------------------------------ shell
        static void BuildShell()
        {
            int idx = 0;
            for (int ix = 0; ix < 3; ix++)
            for (int iz = 0; iz < 3; iz++)
            {
                var scale = new Vector3(Span / Module, 1f, Span / Module);
                Put(shell, "Architecture/FloorTile", "Floor_" + idx, new Vector3(-Span + ix * Span, 0f, -Span + iz * Span), 0f, scale);
                Put(shell, "Architecture/CeilingTile", "Ceiling_" + idx, new Vector3(-Span + ix * Span, WallHeight - 0.03f, -Span + iz * Span), 0f, scale, Quaternion.Euler(180f, 0f, 0f));
                idx++;
            }
            // yaw: back 0, left 90, front 180, right 270 (module +Z faces the room)
            for (int i = 0; i < 3; i++)
            {
                float t = -Module + i * Module;
                float ts = -Span + i * Span;
                var side = new Vector3(Span / Module, 1f, 1f);
                Put(shell, "Architecture/WallPlain", "Wall_Back_" + i, new Vector3(t, 0f, -Half - 0.0625f), 0f);
                Put(shell, "Architecture/WallPlain", "Wall_Front_" + i, new Vector3(t, 0f, Half + 0.0625f), 180f);
                Put(shell, "Architecture/WallWindow", "Wall_Left_" + i, new Vector3(-Half - 0.0625f, 0f, ts), 90f, side);
                Put(shell, i == 0 ? "Architecture/WallDoorway" : "Architecture/WallPlain", "Wall_Right_" + i, new Vector3(Half + 0.0625f, 0f, ts), 270f, side);
            }
            Put(shell, "Architecture/DoorLeafFrame", "Door", new Vector3(Half + 0.0625f, 0f, -Span), 270f);
        }

        // ------------------------------------------------------------------ furniture
        static void BuildFurniture()
        {
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float x = ColX[c], z = RowZ[r];
                Put(furniture, "Furniture/SchoolDesk", $"Desk_{r}_{c}", new Vector3(x, 0f, z), 180f, Vector3.one * DeskScale);
                Put(furniture, "Furniture/SchoolChair", $"Chair_{r}_{c}", new Vector3(x, 0f, z - ChairBack), 0f);
            }

            Put(furniture, "Furniture/TeacherDesk", "TeacherDesk", new Vector3(2.4f, 0f, 3.0f), 0f);
            Put(furniture, "Furniture/TeacherArmchair", "TeacherChair", new Vector3(2.4f, 0f, 3.35f), 180f, Vector3.one * 0.85f);

            Put(furniture, "Furniture/BookshelfWooden", "Bookshelf", new Vector3(0.5f, 0f, -Half + 0.31f), 0f);
            Put(furniture, "Furniture/CabinetDrawers", "CabinetDrawers", new Vector3(-1.4f, 0f, -Half + 0.27f), 0f);
            var shelf = Put(furniture, "Furniture/ShelfWhite", "ShelfWhite", new Vector3(Half - 0.14f, 0f, 0.6f), 270f);
            SnapBack(shelf, 0, Half, true);
        }

        // ------------------------------------------------------------------ props
        static void BuildProps()
        {
            var board = Put(props, "Architecture/Blackboard", "Blackboard", new Vector3(0f, 0.95f, Half - 0.05f), 180f, Vector3.one * 1.1f);
            SnapBack(board, 2, Half, true);
            var bb = WorldBounds(board);
            var box = board.AddComponent<BoxCollider>();
            box.center = board.transform.InverseTransformPoint(bb.center);
            box.size = new Vector3(bb.size.x, bb.size.y, bb.size.z) / 1.1f;

            // chalk writing surface: forward = the direction students look (into the wall)
            var surface = new GameObject("BoardSurface").transform;
            surface.SetParent(board.transform, true);
            surface.position = new Vector3(bb.center.x, bb.center.y + 0.02f, bb.min.z - 0.004f);
            surface.rotation = Quaternion.identity;
            surface.localScale = Vector3.one / 1.1f;
            boardText = surface.gameObject.AddComponent<BoardText>();
            boardText.width = 2.9f;
            boardText.height = 1.45f;
            boardWritePoint = new GameObject("BoardWritePoint").transform;
            boardWritePoint.SetParent(props, true);
            boardWritePoint.position = new Vector3(-0.6f, 1.75f, bb.min.z);
            boardWritePoint.rotation = Quaternion.identity;

            var cork = Put(props, "Props/Corkboard", "NoticeBoard", new Vector3(Half - 0.02f, 1.55f, 1.9f), 270f);
            SnapBack(cork, 0, Half, true);
            var clock = Put(props, "Props/WallClock", "WallClock", new Vector3(2.55f, 2.6f, Half - 0.03f), 180f);
            SnapBack(clock, 2, Half, true);

            // teacher desk items (desk top 0.79)
            const float td = 0.79f;
            Put(props, "Props/Laptop", "TeacherLaptop", new Vector3(2.0f, td, 3.0f), 0f);
            Put(props, "Props/BookEncyclopediaA", "TeacherBookA", new Vector3(2.85f, td, 2.95f), 15f);
            Put(props, "Props/BookEncyclopediaB", "TeacherBookB", new Vector3(2.85f, td + 0.04f, 2.95f), -8f);
            Put(props, "Props/BinderNotebook", "TeacherBinder", new Vector3(3.1f, td, 3.1f), 25f);
            Put(props, "Props/Clipboard", "Clipboard", new Vector3(2.4f, td, 2.75f), 160f);
            Put(props, "Props/PenBlue", "TeacherPenBlue", new Vector3(2.62f, td, 2.78f), 70f);
            Put(props, "Props/PottedPlantSmall", "DeskPlant", new Vector3(1.75f, td, 3.15f), 30f);

            // room dressing
            Put(props, "Props/PottedPlant", "Plant", new Vector3(-3.25f, 0f, 3.25f), 0f, Vector3.one * 1.15f);
            var ext = Put(props, "Props/FireExtinguisher", "FireExtinguisher", new Vector3(Half - 0.18f, 0.8f, -0.6f), 270f);
            SnapBack(ext, 0, Half, true);
            Put(props, "Props/TrashBin", "Bin", new Vector3(-3.3f, 0f, 2.2f));
            Put(props, "Props/BookEncyclopediaA", "CabinetBook", new Vector3(-1.4f, 1.88f, -Half + 0.3f), 5f);

            // bags beside a few desks
            Put(props, "Props/BackpackGreen", "Backpack_A", new Vector3(ColX[0] + 0.55f, 0f, RowZ[0] - 0.55f), 20f);
            Put(props, "Props/BackpackBlue", "Backpack_B", new Vector3(ColX[2] - 0.55f, 0f, RowZ[1] - 0.6f), -30f);
            Put(props, "Props/ShoulderBag", "Bag_C", new Vector3(ColX[1] + 0.6f, 0f, RowZ[2] - 0.6f), 60f);
        }

        static void BuildDeskItems()
        {
            var items = new GameObject("DeskItems").transform;
            items.SetParent(props, false);
            var rnd = new System.Random(7);
            float J(float a) { return (float)(rnd.NextDouble() * 2 - 1) * a; }
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float x = ColX[c], z = RowZ[r];
                string id = r + "_" + c;
                Put(items, "Props/Laptop", "Laptop_" + id, new Vector3(x + 0.05f + J(0.01f), DeskTop, z + 0.03f + J(0.01f)), 180f + J(5f));
                Put(items, "Props/BookEncyclopediaA", "BookA_" + id, new Vector3(x - 0.19f + J(0.01f), DeskTop, z + 0.02f + J(0.02f)), 90f + J(8f));
                Put(items, "Props/BookEncyclopediaB", "BookB_" + id, new Vector3(x - 0.19f + J(0.01f), DeskTop + 0.04f, z + 0.02f + J(0.02f)), 90f + J(20f));
                Put(items, "Props/PenBlue", "PenBlue_" + id, new Vector3(x + 0.19f + J(0.01f), DeskTop, z - 0.10f + J(0.02f)), 70f + J(25f));
                Put(items, "Props/PenRed", "PenRed_" + id, new Vector3(x + 0.13f + J(0.01f), DeskTop, z - 0.14f + J(0.02f)), 100f + J(25f));
            }
        }

        // ------------------------------------------------------------------ lighting
        static void BuildLighting()
        {
            var sun = new GameObject("Sun");
            sun.transform.SetParent(lighting, false);
            sun.transform.rotation = Quaternion.Euler(38f, 78f, 0f);
            var sl = sun.AddComponent<Light>();
            sl.type = LightType.Directional;
            sl.color = new Color(1f, 0.95f, 0.86f);
            sl.intensity = 2.4f;
            sl.shadows = LightShadows.Soft;
            sl.shadowStrength = 0.95f;
            sun.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sl;

            float[] fx = { -2.3f, 0f, 2.3f };
            float[] fz = { 0.8f, -1.0f };
            int n = 0;
            foreach (float z in fz)
            foreach (float x in fx)
            {
                Put(lighting, "Lighting/FluorescentBatten", "Fixture_" + n, new Vector3(x, WallHeight - 0.035f, z), 90f);
                var lg = new GameObject("FixtureLight_" + n);
                lg.transform.SetParent(lighting, false);
                lg.transform.position = new Vector3(x, WallHeight - 0.5f, z);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.97f, 0.92f);
                l.intensity = 3.2f;
                l.range = 6.5f;
                l.shadows = n == 1 || n == 4 ? LightShadows.Soft : LightShadows.None;
                lg.AddComponent<UniversalAdditionalLightData>();
                n++;
            }

            var wash = new GameObject("BoardWash");
            wash.transform.SetParent(lighting, false);
            wash.transform.position = new Vector3(0f, WallHeight - 0.25f, Half - 1.6f);
            wash.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            var wl = wash.AddComponent<Light>();
            wl.type = LightType.Spot; wl.spotAngle = 105f; wl.innerSpotAngle = 60f; wl.range = 6f; wl.intensity = 6f;
            wl.color = new Color(1f, 0.97f, 0.9f);
            wash.AddComponent<UniversalAdditionalLightData>();

            for (int i = 0; i < 3; i++)
            {
                var f = new GameObject("WindowFill_" + i);
                f.transform.SetParent(lighting, false);
                f.transform.position = new Vector3(-Half + 0.4f, 1.9f, -Module + i * Module);
                f.transform.rotation = Quaternion.Euler(20f, 90f, 0f);
                var fl = f.AddComponent<Light>();
                fl.type = LightType.Spot; fl.spotAngle = 130f; fl.innerSpotAngle = 70f; fl.range = 7f; fl.intensity = 5f;
                fl.color = new Color(0.78f, 0.87f, 1f);
                f.AddComponent<UniversalAdditionalLightData>();
            }

            int fan = 0;
            foreach (float x in new[] { -1.15f, 1.15f })
            {
                var g = Put(lighting, "Lighting/CeilingFan", "CeilingFan_" + fan, new Vector3(x, WallHeight - 0.035f, -0.45f));
                var spin = g.AddComponent<CeilingFanSpin>();
                foreach (var t in g.GetComponentsInChildren<Transform>()) if (t.name.Contains("blades")) spin.blades = t;
                spin.degreesPerSecond = fan == 0 ? 210f : -190f;
                fan++;
            }
        }

        // ------------------------------------------------------------------ people
        static void BuildPeople()
        {
            var tutor = BuildPerson("WomanDress", "Prof. Ada", 1.74f, "Tutor");
            tutor.transform.position = new Vector3(0.4f, 0f, 2.45f);
            tutor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            tutorComponent = tutor.AddComponent<TutorBehaviour>();
            tutorComponent.walkSpeed = 1.1f;
            tutor.GetComponentInChildren<Animator>().runtimeAnimatorController = Controller("Tutor");
            Pose(tutor.GetComponentInChildren<Animator>().gameObject, Clip("Idle_Loop"), 0f);
            AddBounds(tutor);

            stageCenter = new GameObject("StageCenter").transform; stageCenter.SetParent(props, false); stageCenter.position = new Vector3(0.2f, 0f, 2.45f);
            boardStandPoint = new GameObject("BoardStand").transform; boardStandPoint.SetParent(props, false); boardStandPoint.position = new Vector3(-0.5f, 0f, 3.05f);
            classFocus = new GameObject("ClassFocus").transform; classFocus.SetParent(props, false); classFocus.position = new Vector3(0f, 1.15f, -0.4f);

            var ctrl = Controller("Student");
            foreach (var s in Students)
            {
                var go = BuildPerson(s.prefab, s.name, s.height, "Student_" + s.name);
                go.GetComponentInChildren<Animator>().runtimeAnimatorController = ctrl;
                Seatify(go, new Vector3(ColX[s.col], SeatTop + HipRise, RowZ[s.row] - ChairBack + 0.11f));
                studentComponents.Add(go.AddComponent<StudentBehaviour>());
                AddBounds(go);
            }

            var tutorHead = tutor.GetComponent<ActorRig>().LookPoint;
            foreach (var sb in studentComponents)
            {
                sb.tutorFocus = tutorHead;
                sb.boardFocus = boardWritePoint;
                var desk = new GameObject("DeskPoint").transform;
                desk.SetParent(sb.transform, false);
                desk.position = sb.transform.position + new Vector3(0f, 0.8f, 0.5f);
                sb.deskPoint = desk;
                var near = new List<Transform>();
                foreach (var other in studentComponents)
                    if (other != sb && Vector3.Distance(other.transform.position, sb.transform.position) < 2.2f) near.Add(other.GetComponent<ActorRig>().LookPoint);
                sb.neighbours = near.ToArray();
            }
        }

        static RuntimeAnimatorController Controller(string name)
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "/Animations/Controllers/" + name + ".controller");
        }

        static AnimationClip Clip(string name)
        {
            foreach (var f in new[] { "UAL1_Standard.fbx", "UAL2_Standard.fbx" })
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Root + "/Animations/UniversalAnimationLibrary/" + f))
                    if (o is AnimationClip c && c.name == name) return c;
            return null;
        }

        static GameObject BuildPerson(string prefabName, string display, float height, string objectName)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Characters/" + prefabName + ".prefab"), people);
            go.name = objectName;
            var anim = go.GetComponentInChildren<Animator>();

            var rs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.localScale = Vector3.one * (height / b.size.y);

            var ik = anim.gameObject.AddComponent<HumanoidIK>();
            var rig = go.AddComponent<ActorRig>();
            rig.displayName = display;
            rig.height = height;
            rig.animator = anim;
            rig.ik = ik;
            rig.head = anim.GetBoneTransform(HumanBodyBones.Head);
            return go;
        }

        /// <summary>Evaluates the sitting clip in edit mode and shifts the character so the pelvis rests on the seat.</summary>
        static void Seatify(GameObject person, Vector3 hipsTarget)
        {
            var anim = person.GetComponentInChildren<Animator>();
            person.transform.position = Vector3.zero;
            Pose(anim.gameObject, Clip("Sitting_Idle_Loop"), 0.4f);
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            person.transform.position += hipsTarget - hips.position;
            var ik = anim.GetComponent<HumanoidIK>();
            ik.pinFeetToFloor = true;
            ik.floorY = 0f;
            var origin = person.transform.position;
            person.transform.position = Vector3.zero;
            Pose(anim.gameObject, Clip("Idle_Loop"), 0.2f);
            ik.ankleHeight = Mathf.Max(0.01f, anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y);
            person.transform.position = origin;
            Pose(anim.gameObject, Clip("Sitting_Idle_Loop"), 0.4f);
        }

        /// <summary>Evaluate an animation clip on a humanoid in edit mode (leaves the pose applied to the transforms).</summary>
        public static void Pose(GameObject character, AnimationClip clip, float time)
        {
            var animator = character.GetComponent<Animator>();
            var graph = PlayableGraph.Create("pose");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "out", animator);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            output.SetSourcePlayable(playable);
            playable.SetTime(time);
            graph.Evaluate(0f);
            graph.Destroy();
        }

        static void AddBounds(GameObject person)
        {
            var b = WorldBounds(person);
            var box = person.AddComponent<BoxCollider>();
            box.center = person.transform.InverseTransformPoint(b.center);
            box.size = b.size / person.transform.lossyScale.x;
        }

        // ------------------------------------------------------------------ camera, ui, director
        static void BuildSystems()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(systems, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 120f; cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();
            var cd = camGo.AddComponent<UniversalAdditionalCameraData>();
            cd.renderPostProcessing = true;
            cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var rig = camGo.AddComponent<ClassroomCameraRig>();
            rig.views = new[]
            {
                new ClassroomCameraRig.View { name = "Back Left",   position = new Vector3(-3.35f, 2.35f, -3.4f), target = new Vector3(0.3f, 1.15f, 1.2f), fov = 62f },
                new ClassroomCameraRig.View { name = "Front Right", position = new Vector3(3.3f, 2.2f, 3.3f),     target = new Vector3(-0.3f, 0.95f, -0.9f), fov = 64f },
                new ClassroomCameraRig.View { name = "Teacher",     position = new Vector3(0.2f, 1.6f, 2.2f),     target = new Vector3(0f, 1.05f, -1.6f), fov = 70f },
                new ClassroomCameraRig.View { name = "Student",     position = new Vector3(0f, 1.25f, -0.95f),    target = new Vector3(0f, 1.6f, 3.6f), fov = 66f },
                new ClassroomCameraRig.View { name = "Board",       position = new Vector3(0.2f, 1.5f, 0.5f),     target = new Vector3(0f, 1.7f, 3.75f), fov = 50f },
                new ClassroomCameraRig.View { name = "Overview",    position = new Vector3(0f, 3.0f, -3.4f),      target = new Vector3(0f, 0.9f, 1.3f), fov = 75f },
            };
            rig.startView = 0;

            var sys = new GameObject("ClassroomSystems");
            sys.transform.SetParent(systems, false);
            var ui = sys.AddComponent<ClassroomUI>();
            ui.cameraRig = rig;
            var director = sys.AddComponent<LessonDirector>();
            director.tutor = tutorComponent;
            director.students = studentComponents.ToArray();
            director.board = boardText;
            director.boardWritePoint = boardWritePoint;
            director.boardStandPoint = boardStandPoint;
            director.stageCenter = stageCenter;
            director.classFocus = classFocus;
            director.ui = ui;
            ui.director = director;
            var inter = sys.AddComponent<ClassroomInteraction>();
            inter.cam = cam; inter.rig = rig; inter.director = director; inter.ui = ui;
        }

        // ------------------------------------------------------------------ environment / post processing
        static void BuildEnvironment()
        {
            Directory.CreateDirectory(Root + "/Materials/Environment");
            var skyPath = Root + "/Materials/Environment/Sky_Playground.mat";
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(skyMat, skyPath);
            }
            var hdrPath = Root + "/Textures/Environment/air_museum_playground_2k.hdr";
            var hdrImporter = (TextureImporter)AssetImporter.GetAtPath(hdrPath);
            if (hdrImporter != null && (hdrImporter.sRGBTexture || hdrImporter.textureShape != TextureImporterShape.Texture2D))
            {
                hdrImporter.textureShape = TextureImporterShape.Texture2D;
                hdrImporter.sRGBTexture = false;
                hdrImporter.maxTextureSize = 2048;
                hdrImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                hdrImporter.SaveAndReimport();
            }
            skyMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(hdrPath));
            skyMat.SetFloat("_Mapping", 1f);
            skyMat.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            skyMat.SetFloat("_Exposure", 1.0f);
            skyMat.SetFloat("_Rotation", 200f);
            EditorUtility.SetDirty(skyMat);

            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.48f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.26f, 0.22f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
            RenderSettings.fog = false;

            Directory.CreateDirectory(Root + "/Settings");
            var profilePath = Root + "/Settings/Classroom_PostProcessing.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null) AssetDatabase.DeleteAsset(profilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(1.0f); bloom.intensity.Override(0.28f); bloom.scatter.Override(0.65f);
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.2f); vig.smoothness.Override(0.45f);
            var col = profile.Add<ColorAdjustments>(true);
            col.postExposure.Override(0.15f); col.contrast.Override(10f); col.saturation.Override(6f);
            EditorUtility.SetDirty(profile);

            var vol = new GameObject("PostProcess").AddComponent<Volume>();
            vol.transform.SetParent(systems, false);
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(lighting, false);
            probeGo.transform.position = new Vector3(0f, 1.5f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(7.5f, 3.2f, 7.5f);
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.intensity = 1f;
        }
    }
}
