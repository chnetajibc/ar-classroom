#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RealisticClassroom.Publishing
{
    /// <summary>Renders the store artwork: edit-mode stills, prefab sheets, a scripted play-mode sequence and a frame sequence for the GIF.</summary>
    public static class MarketingCapture
    {
        const string Root = ModelConverter.Root;
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, Color? bg = null)
        {
            ScreenshotTool.Capture(dir + "/" + name + "_3x2.png", pos, look, fov, 2400, 1600, bg);
            ScreenshotTool.Capture(dir + "/" + name + "_16x9.png", pos, look, fov, 1920, 1080, bg);
        }

        // ------------------------------------------------------------------ demo scene, edit mode
        public static string Stills(string dir)
        {
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(ClassroomSceneBuilderPath(), OpenSceneMode.Single);
            Shot(dir, "hero", V(-3.35f, 2.35f, -3.4f), V(0.3f, 1.15f, 1.2f), 62f);
            Shot(dir, "front_right", V(3.3f, 2.2f, 3.3f), V(-0.3f, 0.95f, -0.9f), 64f);
            Shot(dir, "overview", V(0f, 3.0f, -3.4f), V(0f, 0.9f, 1.3f), 75f);
            Shot(dir, "teacher_view", V(0.2f, 1.6f, 2.2f), V(0f, 1.05f, -1.6f), 70f);
            Shot(dir, "student_view", V(0f, 1.25f, -0.95f), V(0f, 1.6f, 3.6f), 66f);
            Shot(dir, "desk_detail", V(0.9f, 1.55f, -0.15f), V(0f, 0.8f, 1.3f), 45f);
            Shot(dir, "ceiling", V(-2.3f, 1.9f, -0.6f), V(0f, 3.0f, 0.6f), 75f);
            Shot(dir, "windows", V(3.2f, 1.7f, -1.5f), V(-3.75f, 1.4f, 0f), 60f);
            Shot(dir, "back_wall", V(1.5f, 1.7f, 1.6f), V(-0.4f, 1.1f, -3.75f), 62f);
            Shot(dir, "door_corner", V(-2.0f, 1.6f, -1.2f), V(3.75f, 1.1f, -2.4f), 62f);
            Shot(dir, "teacher_desk", V(0.6f, 1.6f, 0.9f), V(2.4f, 0.9f, 3.1f), 55f);
            return "stills done";
        }

        static string ClassroomSceneBuilderPath() { return "Assets/RealisticClassroom/Scenes/Classroom_Demo.unity"; }

        // ------------------------------------------------------------------ prefab sheets
        static void SheetLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.6f);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 2.2f; key.transform.rotation = Quaternion.Euler(45f, -35f, 0f); key.shadows = LightShadows.Soft;
            key.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.7f; fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
            fill.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        }

        static GameObject Prefab(string rel, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + rel + ".prefab"));
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0f, yaw, 0f); go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>Places prefabs in a row along X (auto-spaced by their widths), centred on x = 0; returns the total width.</summary>
        static float Row(string[] rels, float z, float yaw, float gap, float scale = 1f, Action<GameObject, int> after = null, float fixedPitch = 0f)
        {
            var gos = new List<GameObject>();
            float total = 0f;
            foreach (var r in rels)
            {
                var g = Prefab(r, Vector3.zero, yaw, scale);
                gos.Add(g);
                total += Mathf.Max(0.3f, ModelConverter.Bounds(g).size.x) + gap;
            }
            total -= gap;
            if (fixedPitch > 0f) total = fixedPitch * gos.Count;
            float x = -total * 0.5f;
            for (int i = 0; i < gos.Count; i++)
            {
                float w = fixedPitch > 0f ? fixedPitch - gap : Mathf.Max(0.3f, ModelConverter.Bounds(gos[i]).size.x);
                gos[i].transform.position = new Vector3(x + w * 0.5f, 0f, z);
                after?.Invoke(gos[i], i);
                x += w + gap;
            }
            return total;
        }

        public static string Sheets(string dir)
        {
            Directory.CreateDirectory(dir);
            var bg = new Color(0.86f, 0.88f, 0.91f);

            // characters (front view): one teacher + nine students
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SheetLighting();
            var names = new[] { "WomanDress", "WomanA", "ManA", "HoodieCharacter", "WomanTank", "CasualCharacter", "ManLongSleeves", "WomanCasual", "ManB", "BeachCharacter" };
            var idle = AnimationSetupClip("Idle_Loop");
            var talk = AnimationSetupClip("Idle_Talking_Loop");
            var front = names.Take(5).Select(n => "Characters/" + n).ToArray();
            var back = names.Skip(5).Select(n => "Characters/" + n).ToArray();
            Row(front, 0f, 0f, 0f, 1f, (g, i) => ClassroomSceneBuilderPose(g.GetComponentInChildren<Animator>().gameObject, i == 0 ? talk : idle, 0.3f + 0.13f * i), 0.9f);
            Row(back, -1.3f, 0f, 0f, 1f, (g, i) => { g.transform.position += V(0.45f, 0f, 0f); ClassroomSceneBuilderPose(g.GetComponentInChildren<Animator>().gameObject, idle, 0.5f + 0.11f * i); }, 0.9f);
            Shot(dir, "characters", V(0f, 2.3f, 6.4f), V(0f, 0.85f, -0.6f), 34f, bg);

            // furniture and props on a clean backdrop
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SheetLighting();
            Row(new[] { "Furniture/SchoolDesk", "Furniture/SchoolChair", "Furniture/TeacherDesk", "Furniture/TeacherArmchair", "Furniture/BookshelfWooden", "Furniture/CabinetDrawers", "Furniture/ShelfWhite", "Props/PottedPlant" }, 3.2f, 0f, 0.3f);
            Row(new[] { "Props/Laptop", "Props/BookEncyclopediaA", "Props/BookEncyclopediaB", "Props/BinderNotebook", "Props/Clipboard", "Props/PenBlue", "Props/PenRed", "Props/PottedPlantSmall" }, 1.0f, 0f, 0.25f, 2.0f);
            Row(new[] { "Props/FireExtinguisher", "Props/BackpackGreen", "Props/BackpackBlue", "Props/ShoulderBag", "Props/TrashBin", "Props/WallClock", "Props/Corkboard", "Lighting/FluorescentBatten" }, -1.3f, 0f, 0.25f, 1.8f);
            Shot(dir, "props", V(0f, 8.8f, 9.6f), V(0f, 0.6f, 0.4f), 36f, bg);

            // architecture kit
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SheetLighting();
            Row(new[] { "Architecture/WallPlain", "Architecture/WallWindow", "Architecture/WallDoorway", "Architecture/DoorLeafFrame", "Architecture/Blackboard" }, 0f, 0f, 0.35f);
            Row(new[] { "Architecture/FloorTile", "Architecture/FloorTile", "Architecture/CeilingTile" }, 3.3f, 0f, 0.2f);
            Shot(dir, "kit", V(0f, 6.2f, 11.5f), V(0f, 0.9f, 1.0f), 36f, bg);
            return "sheets done";
        }

        static AnimationClip AnimationSetupClip(string name) { return AnimationSetup.Find(name); }
        static void ClassroomSceneBuilderPose(GameObject go, AnimationClip clip, float t) { RealisticClassroom.EditorTools.ClassroomSceneBuilder.Pose(go, clip, t); }

        // ------------------------------------------------------------------ play mode sequence
        class Step { public string name; public Func<bool> ready; public Action capture; }
        static List<Step> steps; static int idx; static float stepStart; static string playDir;

        static string Caption()
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Text>())
                if (t.gameObject.activeInHierarchy && t.text.Contains("<b>")) return t.text;
            return "";
        }

        public static string StartPlaySequence(string dir)
        {
            playDir = dir; Directory.CreateDirectory(dir); idx = 0; stepStart = 0f;
            steps = new List<Step>
            {
                new Step { name = "board_writing", ready = () => Caption().Contains("Let's get started") && Time.time - stepStart > 9.5f,
                    capture = () => Shot(dir, "play_board_writing", V(0.3f, 1.55f, 0.9f), V(-0.3f, 1.75f, 3.75f), 52f) },
                new Step { name = "board_full", ready = () => Caption().Contains("An array keeps its items"),
                    capture = () => Shot(dir, "play_board_lesson", V(-0.4f, 1.6f, 0.3f), V(0.1f, 1.7f, 3.75f), 55f) },
                new Step { name = "explaining", ready = () => Caption().Contains("So the computer finds") || Caption().Contains("Inserting in the middle"),
                    capture = () => Shot(dir, "play_explaining", V(-3.2f, 2.2f, -3.0f), V(0.4f, 1.15f, 1.6f), 60f) },
                new Step { name = "hands", ready = () => Caption().Contains("Why is reading") && Time.time - stepStart > 4.5f,
                    capture = () => Shot(dir, "play_hands_up", V(-3.35f, 2.3f, -3.3f), V(0.3f, 1.1f, 1.0f), 60f) },
                new Step { name = "answer", ready = () => Caption().Contains("Because the address"),
                    capture = () => Shot(dir, "play_student_answers", V(1.3f, 1.55f, 2.5f), V(-0.4f, 1.1f, -0.8f), 62f) },
            };
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            File.WriteAllText(dir + "/_play.txt", "RUNNING");
            return "play sequence armed";
        }

        static void Tick()
        {
            if (!Application.isPlaying || steps == null) return;
            if (stepStart == 0f) stepStart = Time.time;
            var s = steps[idx];
            bool ready;
            try { ready = s.ready(); } catch { ready = false; }
            if (!ready) return;
            s.capture();
            idx++; stepStart = Time.time;
            if (idx >= steps.Count) { EditorApplication.update -= Tick; File.WriteAllText(playDir + "/_play.txt", "DONE"); }
        }

        /// <summary>One set of play-mode stills (call several times while the lesson runs); returns the current caption.</summary>
        public static string PlayShots(string dir, string tag)
        {
            Directory.CreateDirectory(dir);
            Shot(dir, "play_board_" + tag, V(0.3f, 1.55f, 0.9f), V(-0.2f, 1.7f, 3.75f), 52f);
            Shot(dir, "play_wide_" + tag, V(-3.35f, 2.3f, -3.3f), V(0.3f, 1.1f, 1.0f), 60f);
            return Caption();
        }

        // ------------------------------------------------------------------ frame sequence for the GIF
        static int gifFrame, gifTotal; static string gifDir; static int lastFrameCount;

        public static string StartGif(string dir, int frames, int fps)
        {
            gifDir = dir; Directory.CreateDirectory(dir); gifFrame = 0; gifTotal = frames; lastFrameCount = -1;
            foreach (var f in Directory.GetFiles(dir, "f_*.png")) File.Delete(f);
            Time.captureFramerate = fps;
            EditorApplication.update -= GifTick;
            EditorApplication.update += GifTick;
            File.WriteAllText(dir + "/_gif.txt", "RUNNING");
            return "gif armed";
        }

        static void GifTick()
        {
            if (!Application.isPlaying) return;
            if (Time.frameCount == lastFrameCount) return;
            lastFrameCount = Time.frameCount;
            var cam = Camera.main;
            if (cam == null) return;
            // skip a few warm-up frames so the lesson has started
            if (Time.frameCount % 1 == 0)
            {
                ScreenshotTool.Capture(gifDir + "/f_" + gifFrame.ToString("D4") + ".png", cam.transform.position, cam.transform.position + cam.transform.forward, cam.fieldOfView, 960, 540);
                gifFrame++;
            }
            if (gifFrame >= gifTotal)
            {
                EditorApplication.update -= GifTick;
                Time.captureFramerate = 0;
                File.WriteAllText(gifDir + "/_gif.txt", "DONE");
            }
        }
    }
}
#endif
