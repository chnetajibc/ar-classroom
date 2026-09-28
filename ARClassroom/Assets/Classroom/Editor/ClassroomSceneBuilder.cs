#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARClassroom.EditorTools
{
    /// <summary>
    /// Assembles the classroom scene purely from imported models (Poly Haven, Kenney, Quaternius, Anthon, jeremy) plus
    /// lights, camera, post-processing and gameplay scripts. Nothing is modelled from primitives.
    /// </summary>
    public static class ClassroomSceneBuilder
    {
        public const string ScenePath = "Assets/Classroom/Scenes/Classroom.unity";

        // ---- room (metres). Students face +Z, the blackboard is on the +Z wall.
        const float Half = 3.75f;              // interior half-extent in X and Z
        const float Module = 2.5f;             // scale applied to Kenney wall/floor modules (1 m -> 2.5 m)
        const float WallHeight = 1.29f * Module;
        const float SeatTop = 0.50f;           // SchoolChair_01 seat surface
        const float HipRise = 0.085f;          // pelvis centre above the seat surface
        const float ChairBack = 0.75f;         // desk centre -> chair centre
        static readonly float[] RowZ = { 1.3f, -0.2f, -1.7f };
        static readonly float[] ColX = { -1.9f, 0f, 1.9f };

        const string PH = "Assets/Classroom/Models/PolyHaven/";
        const string KN = "Assets/Classroom/Models/Kenney/";
        const string SP = "Assets/Classroom/Prefabs/Props/";
        const string PP = "Assets/Classroom/Models/Props/";

        enum Pivot { BottomCenter, TopCenter, Center }

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
        static Material wallMat, floorMat, ceilingMat;
        static readonly List<StudentBehaviour> studentComponents = new List<StudentBehaviour>();
        static Transform boardWritePoint, boardStandPoint, stageCenter, classFocus;
        static BoardText boardText;
        static TutorBehaviour tutorComponent;

        [MenuItem("AR Classroom/Rebuild Classroom Scene")]
        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            studentComponents.Clear();
            var root = new GameObject("Classroom").transform;
            shell = Group(root, "Shell"); furniture = Group(root, "Furniture"); props = Group(root, "Props");
            lighting = Group(root, "Lighting"); people = Group(root, "People"); systems = Group(root, "Systems");

            BuildMaterials();
            BuildShell();
            BuildFurniture();
            BuildProps();
            BuildLighting();
            BuildPeople();
            BuildSystems();
            BuildEnvironment();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            return "Built " + ScenePath;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // ------------------------------------------------------------------ placement helpers
        static GameObject Inst(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new FileNotFoundException("Missing model " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(asset);
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Like Place, but scales the model so its height (Y extent) equals targetHeight.</summary>
        static GameObject PlaceFit(Transform parent, string path, string name, Vector3 pos, float yaw, float targetHeight, Pivot pivot = Pivot.BottomCenter)
        {
            var probe = Inst(path);
            float h = WorldBounds(probe).size.y;
            Object.DestroyImmediate(probe);
            return Place(parent, path, name, pos, yaw, targetHeight / h, pivot);
        }

        static GameObject Place(Transform parent, string path, string name, Vector3 pos, float yaw, float scale = 1f, Pivot pivot = Pivot.BottomCenter, Quaternion? extra = null, float lengthScale = 1f, float depthScale = 1f)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            var child = Inst(path).transform;
            child.SetParent(anchor, false);
            var b = WorldBounds(child.gameObject);
            Vector3 pv = pivot == Pivot.BottomCenter ? new Vector3(b.center.x, b.min.y, b.center.z)
                       : pivot == Pivot.TopCenter ? new Vector3(b.center.x, b.max.y, b.center.z) : b.center;
            child.position = child.position - pv;
            anchor.position = pos;
            anchor.rotation = Quaternion.Euler(0f, yaw, 0f) * (extra ?? Quaternion.identity);
            anchor.localScale = new Vector3(scale * lengthScale, scale, scale * depthScale);
            return anchor.gameObject;
        }

        /// <summary>Two fluorescent battens taken from the Poly Haven fixture set (the model ships 7 side by side), lit with an emissive diffuser.</summary>
        static GameObject PlaceBattens(Transform parent, string name, Vector3 pos)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            var model = Inst(PH + "mounted_fluorescent_lights/mounted_fluorescent_lights.gltf");
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            for (int i = model.transform.childCount - 1; i >= 0; i--)
            {
                var c = model.transform.GetChild(i);
                if (!c.name.EndsWith("_b") && !c.name.EndsWith("_e")) Object.DestroyImmediate(c.gameObject);
            }
            model.transform.SetParent(anchor, false);
            var b = WorldBounds(model);
            model.transform.position -= new Vector3(b.center.x, b.max.y, b.center.z);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i].name.EndsWith("_glass")) mats[i] = EmissiveGlass(mats[i]);
                r.sharedMaterials = mats;
            }
            anchor.position = pos;
            anchor.rotation = Quaternion.Euler(0f, 90f, 0f);
            anchor.localScale = Vector3.one * 1.35f;
            return anchor.gameObject;
        }

        static Material emissiveGlass;
        static Material EmissiveGlass(Material src)
        {
            if (emissiveGlass != null) return emissiveGlass;
            const string path = "Assets/Classroom/Materials/FixtureGlassEmissive.mat";
            AssetDatabase.DeleteAsset(path);
            emissiveGlass = new Material(src) { name = "FixtureGlassEmissive" };
            emissiveGlass.SetColor("emissiveFactor", new Color(2.2f, 2.15f, 2.0f));
            AssetDatabase.CreateAsset(emissiveGlass, path);
            return emissiveGlass;
        }

        // ------------------------------------------------------------------ materials
        static void BuildMaterials()
        {
            wallMat = PbrMaterials.FromPolyHaven("plastered_wall_04", "Wall_Plaster", new Color(0.88f, 0.93f, 0.86f), 0.6f, 0.5f);
            floorMat = PbrMaterials.FromPolyHaven("laminate_floor_02", "Floor_Laminate", new Color(0.92f, 0.88f, 0.84f), 0.8f, 1f);
            ceilingMat = PbrMaterials.FromPolyHaven("plastered_wall_04", "Ceiling_Plaster", new Color(1f, 1f, 0.98f), 0.4f, 0.3f);
        }

        // ------------------------------------------------------------------ shell (Kenney modular pieces, re-textured in world space)
        static void BuildShell()
        {
            int idx = 0;
            const float span = 2.5833f; // ceiling overlaps the tops of the walls so no daylight leaks through the seams
            for (int ix = 0; ix < 3; ix++)
            for (int iz = 0; iz < 3; iz++)
            {
                float x = -Module + ix * Module, z = -Module + iz * Module;
                var floor = Place(shell, KN + "floorFull.glb", "Floor_" + idx, new Vector3(-span + ix * span, 0f, -span + iz * span), 0f, Module, Pivot.TopCenter, null, span / Module, span / Module);
                ShellMesh.ApplyWorldUV(floor, "Floor_" + idx, null, floorMat, 1.6f);
                var ceil = Place(shell, KN + "floorFull.glb", "Ceiling_" + idx, new Vector3(-span + ix * span, WallHeight - 0.03f, -span + iz * span), 0f, Module, Pivot.TopCenter, Quaternion.Euler(180f, 0f, 0f), span / Module, span / Module);
                ShellMesh.ApplyWorldUV(ceil, "Ceiling_" + idx, null, ceilingMat, 1.6f);
                idx++;
            }

            // walls: inward = +Z of the module. yaw: back 0, left 90, front 180, right 270.
            // Side walls are stretched 3.3% so they overlap the front/back walls and close the corner cracks.
            const float side = 2.5833f;
            for (int i = 0; i < 3; i++)
            {
                float t = -Module + i * Module;
                float ts = -side + i * side;
                Wall("Wall_Back_" + i, "wall", new Vector3(t, 0f, -Half - 0.0625f), 0f, 1f);
                Wall("Wall_Front_" + i, "wall", new Vector3(t, 0f, Half + 0.0625f), 180f, 1f);
                Wall("Wall_Left_" + i, "wallWindow", new Vector3(-Half - 0.0625f, 0f, ts), 90f, side / Module);
                bool doorModule = i == 0;
                Wall("Wall_Right_" + i, doorModule ? "wallDoorway" : "wall", new Vector3(Half + 0.0625f, 0f, ts), 270f, side / Module);
            }
            // door leaf + frame in the rear module of the right wall
            Place(shell, KN + "doorwayFront.glb", "Door", new Vector3(Half + 0.0625f, 0f, -side), 270f, Module);
        }

        static void Wall(string name, string kind, Vector3 pos, float yaw, float lengthScale)
        {
            var w = Place(shell, KN + kind + ".glb", name, pos, yaw, Module, Pivot.BottomCenter, null, lengthScale);
            ShellMesh.ApplyWorldUV(w, name, "_defaultMat", wallMat, 2.0f);
        }

        // ------------------------------------------------------------------ furniture
        static void BuildFurniture()
        {
            // 3 x 3 student desks and chairs
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float x = ColX[c], z = RowZ[r];
                Place(furniture, PH + "SchoolDesk_01/SchoolDesk_01.gltf", $"Desk_{r}_{c}", new Vector3(x, 0f, z), 180f, 0.92f);
                Place(furniture, PH + "SchoolChair_01/SchoolChair_01.gltf", $"Chair_{r}_{c}", new Vector3(x, 0f, z - ChairBack), 0f);
            }

            // teacher desk + chair front-right
            Place(furniture, SP + "Teacher_Desk.prefab", "TeacherDesk", new Vector3(2.55f, 0f, 3.0f), 180f, 1.0f);
            Place(furniture, SP + "Teacher_Chair.prefab", "TeacherChair", new Vector3(2.55f, 0f, 3.5f), 200f, 1.0f);

            // storage along the back wall and right wall
            Place(furniture, PH + "wooden_bookshelf_worn/wooden_bookshelf_worn.gltf", "Bookshelf", new Vector3(0.5f, 0f, -Half + 0.31f), 0f, 1.0f);
            Place(furniture, PH + "drawer_cabinet/drawer_cabinet.gltf", "DrawerCabinet", new Vector3(-1.4f, 0f, -Half + 0.27f), 0f, 1.0f);
            Place(furniture, SP + "Large_Shelf.prefab", "LargeShelf", new Vector3(Half - 0.27f, 0f, 0.6f), 0f, 0.85f);
        }

        // ------------------------------------------------------------------ props
        static void BuildProps()
        {
            // blackboard centred on the front wall, notice board beside it
            var board = Place(props, SP + "Blackboard.prefab", "Blackboard", new Vector3(0f, 0.95f, Half - 0.05f), 180f, 0.82f);
            var boardBounds = WorldBounds(board);
            AddBoxCollider(board, boardBounds);

            // chalk writing surface: forward = direction students look (into the wall)
            var surface = new GameObject("BoardSurface").transform;
            surface.SetParent(board.transform, true);
            surface.position = new Vector3(boardBounds.center.x, boardBounds.center.y + 0.05f, boardBounds.min.z - 0.005f);
            surface.rotation = Quaternion.identity;
            surface.localScale = Vector3.one / 0.82f;
            boardText = surface.gameObject.AddComponent<BoardText>();
            boardText.width = 2.8f;
            boardText.height = 1.4f;
            boardWritePoint = new GameObject("BoardWritePoint").transform;
            boardWritePoint.SetParent(props, true);
            boardWritePoint.position = new Vector3(-0.6f, 1.75f, boardBounds.min.z);
            boardWritePoint.rotation = Quaternion.identity;

            Place(props, SP + "Info_Board.prefab", "NoticeBoard", new Vector3(Half - 0.05f, 1.0f, 1.9f), 0f, 0.8f);
            Place(props, PH + "wall_clock/wall_clock.gltf", "WallClock", new Vector3(2.55f, 2.6f, Half - 0.03f), 180f, 1.6f, Pivot.Center);

            // teacher desk items
            Place(props, SP + "World_Globe.prefab", "Globe", new Vector3(3.05f, 0.82f, 3.05f), 30f, 1.0f);
            Place(props, SP + "Book.prefab", "Book_A", new Vector3(2.4f, 0.82f, 2.95f), 15f, 1.0f);
            Place(props, SP + "Book.prefab", "Book_B", new Vector3(2.42f, 0.85f, 2.96f), -10f, 0.9f);
            Place(props, SP + "Pencil_Box.prefab", "PencilBox", new Vector3(2.0f, 0.82f, 3.1f), 0f, 1.0f);
            Place(props, PH + "clipboard/clipboard.gltf", "Clipboard", new Vector3(2.75f, 0.82f, 2.85f), 160f, 1.0f);

            // room dressing
            Place(props, PH + "potted_plant_01/potted_plant_01.gltf", "Plant", new Vector3(-3.25f, 0f, 3.25f), 0f, 1.15f);
            Place(props, PH + "korean_fire_extinguisher_01/korean_fire_extinguisher_01.gltf", "FireExtinguisher", new Vector3(Half - 0.18f, 0.8f, -0.6f), 90f, 1.0f);
            PlaceFit(props, PP + "TrashcanSmall.glb", "Bin", new Vector3(-3.3f, 0f, 2.2f), 0f, 0.5f);
            Place(props, PH + "book_encyclopedia_set_01/book_encyclopedia_set_01.gltf", "Encyclopedia", new Vector3(-1.4f, 1.9f, -Half + 0.3f), 0f, 1.2f);

            // bags beside a few desks
            PlaceFit(props, PP + "BackpackA.glb", "Backpack_A", new Vector3(ColX[0] + 0.55f, 0f, RowZ[0] - 0.55f), 20f, 0.40f);
            PlaceFit(props, PP + "BackpackB.glb", "Backpack_B", new Vector3(ColX[2] - 0.55f, 0f, RowZ[1] - 0.6f), -30f, 0.40f);
            PlaceFit(props, PP + "Bag.glb", "Bag_C", new Vector3(ColX[1] + 0.6f, 0f, RowZ[2] - 0.6f), 60f, 0.30f);
        }

        static void AddBoxCollider(GameObject go, Bounds worldBounds)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(worldBounds.center);
            box.size = worldBounds.size / go.transform.lossyScale.x;
        }

        // ------------------------------------------------------------------ lighting
        static void BuildLighting()
        {
            // daylight from the window side
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

            // ceiling fixtures: twin fluorescent battens + point light
            float[] fx = { -2.3f, 0f, 2.3f };
            float[] fz = { 0.8f, -1.0f };
            int n = 0;
            foreach (float z in fz)
            foreach (float x in fx)
            {
                var fixture = PlaceBattens(lighting, "Fixture_" + n, new Vector3(x, WallHeight - 0.035f, z));
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

            // board wash + window fill
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

            // ceiling fans
            int fan = 0;
            foreach (float x in new[] { -1.15f, 1.15f })
            {
                var g = Place(lighting, PH + "ceiling_fan/ceiling_fan.gltf", "CeilingFan_" + fan, new Vector3(x, WallHeight - 0.035f, -0.45f), 0f, 1.0f, Pivot.TopCenter);
                var spin = g.AddComponent<CeilingFanSpin>();
                spin.blades = g.transform.Find("ceiling_fan/ceiling_fan_blades");
                spin.degreesPerSecond = fan == 0 ? 210f : -190f;
                fan++;
            }
        }

        // ------------------------------------------------------------------ people
        static void BuildPeople()
        {
            var tutor = BuildPerson("WomanDress", "Prof. Ada", 1.74f, people, "Tutor");
            tutor.transform.position = new Vector3(0.4f, 0f, 2.45f);
            tutor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            tutorComponent = tutor.AddComponent<TutorBehaviour>();
            tutorComponent.walkSpeed = 1.1f;
            tutor.GetComponent<ActorRig>().animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CharacterPrep.ControllerDir + "/Tutor.controller");
            ScreenshotTool.Pose(tutor.GetComponentInChildren<Animator>().gameObject, CharacterPrep.FindClip("Idle_Loop"), 0f);
            AddBounds(tutor);

            stageCenter = new GameObject("StageCenter").transform; stageCenter.SetParent(props, false); stageCenter.position = new Vector3(0.2f, 0f, 2.45f);
            boardStandPoint = new GameObject("BoardStand").transform; boardStandPoint.SetParent(props, false); boardStandPoint.position = new Vector3(-0.5f, 0f, 3.05f);
            classFocus = new GameObject("ClassFocus").transform; classFocus.SetParent(props, false); classFocus.position = new Vector3(0f, 1.15f, -0.4f);

            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CharacterPrep.ControllerDir + "/Student.controller");
            foreach (var s in Students)
            {
                var go = BuildPerson(s.prefab, s.name, s.height, people, "Student_" + s.name);
                var anim = go.GetComponentInChildren<Animator>();
                anim.runtimeAnimatorController = ctrl;
                float chairX = ColX[s.col], chairZ = RowZ[s.row] - ChairBack;
                Seatify(go, new Vector3(chairX, SeatTop + HipRise, chairZ + 0.11f), s.height);
                var sb = go.AddComponent<StudentBehaviour>();
                studentComponents.Add(sb);
                AddBounds(go);
            }

            // wire look targets after all students exist
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
                foreach (var other in studentComponents) if (other != sb && Vector3.Distance(other.transform.position, sb.transform.position) < 2.2f) near.Add(other.GetComponent<ActorRig>().LookPoint);
                sb.neighbours = near.ToArray();
            }
        }

        static GameObject BuildPerson(string prefabName, string display, float height, Transform parent, string objectName)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrep.PrefabDir + "/" + prefabName + ".prefab"));
            go.name = objectName;
            go.transform.SetParent(parent, false);
            var anim = go.GetComponentInChildren<Animator>();

            // normalise stature from the (T-pose) skinned bounds
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

        /// <summary>Pose the sitting clip in edit mode, then shift the character so the pelvis rests on the seat.</summary>
        static void Seatify(GameObject person, Vector3 hipsTarget, float height)
        {
            var anim = person.GetComponentInChildren<Animator>();
            person.transform.position = Vector3.zero;
            ScreenshotTool.Pose(anim.gameObject, CharacterPrep.FindClip("Sitting_Idle_Loop"), 0.4f);
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            person.transform.position += hipsTarget - hips.position;
            var ik = anim.GetComponent<HumanoidIK>();
            ik.pinFeetToFloor = true;
            ik.floorY = 0f;
            // ankle height from the standing pose
            var origin = person.transform.position;
            person.transform.position = Vector3.zero;
            ScreenshotTool.Pose(anim.gameObject, CharacterPrep.FindClip("Idle_Loop"), 0.2f);
            ik.ankleHeight = Mathf.Max(0.01f, anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y);
            person.transform.position = origin;
            ScreenshotTool.Pose(anim.gameObject, CharacterPrep.FindClip("Sitting_Idle_Loop"), 0.4f);
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
                new ClassroomCameraRig.View { name = "Back Left",  position = new Vector3(-3.35f, 2.35f, -3.4f), target = new Vector3(0.3f, 1.15f, 1.2f), fov = 62f },
                new ClassroomCameraRig.View { name = "Front Right", position = new Vector3(3.3f, 2.2f, 3.3f),   target = new Vector3(-0.3f, 0.95f, -0.9f), fov = 64f },
                new ClassroomCameraRig.View { name = "Teacher",    position = new Vector3(0.2f, 1.6f, 2.2f),   target = new Vector3(0f, 1.05f, -1.6f), fov = 70f },
                new ClassroomCameraRig.View { name = "Student",    position = new Vector3(0f, 1.25f, -0.95f),  target = new Vector3(0f, 1.6f, 3.6f), fov = 66f },
                new ClassroomCameraRig.View { name = "Board",      position = new Vector3(0.2f, 1.5f, 0.5f),   target = new Vector3(0f, 1.7f, 3.75f), fov = 50f },
                new ClassroomCameraRig.View { name = "Overview",   position = new Vector3(0f, 3.0f, -3.4f),    target = new Vector3(0f, 0.9f, 1.3f), fov = 75f },
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
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Classroom/Materials/Sky.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(skyMat, "Assets/Classroom/Materials/Sky.mat");
            }
            skyMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Classroom/Textures/Sky/air_museum_playground_2k.hdr"));
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

            var profilePath = "Assets/Classroom/Settings/ClassroomPost.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null) AssetDatabase.DeleteAsset(profilePath);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
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

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
