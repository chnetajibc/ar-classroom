#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>
    /// One-off (not shipped) converter: turns the downloaded glTF/GLB/OBJ/FBX sources into the FBX + URP material + prefab
    /// layout required by the Unity Asset Store (mesh files must be FBX/DAE/ABC/OBJ, 1 unit = 1 m, pivots at the bottom centre).
    /// </summary>
    public static class ModelConverter
    {
        public const string Root = "Assets/RealisticClassroom";
        const string Old = "Assets/_Publishing/Sources/";
        const string PH = Old + "Models/PolyHaven/";

        public enum Pivot { BottomCenter, TopCenter, Center }

        public class Spec
        {
            public string name, category, source;
            public string[] keep;           // keep only child objects whose name ends with one of these
            public Pivot pivot = Pivot.BottomCenter;
            public int maxTex = 1024;
            public Quaternion preRotation = Quaternion.identity;     // baked into the FBX hierarchy (e.g. lay a book flat)
            public float fitSize;                                     // 0 = keep native size; otherwise scale (via importer) so this extent is fitSize metres
            public char fitAxis = 'y';
            public string emissive;                                   // short material name that becomes an emissive light tube
        }

        public static readonly Spec[] Statics =
        {
            new Spec { name = "SchoolDesk",         category = "Furniture", source = PH + "SchoolDesk_01/SchoolDesk_01.gltf", maxTex = 2048 },
            new Spec { name = "SchoolChair",        category = "Furniture", source = PH + "SchoolChair_01/SchoolChair_01.gltf", maxTex = 2048 },
            new Spec { name = "TeacherDesk",        category = "Furniture", source = PH + "metal_office_desk/metal_office_desk.gltf", maxTex = 2048 },
            new Spec { name = "TeacherArmchair",    category = "Furniture", source = PH + "modern_arm_chair_01/modern_arm_chair_01.gltf" },
            new Spec { name = "BookshelfWooden",    category = "Furniture", source = PH + "wooden_bookshelf_worn/wooden_bookshelf_worn.gltf" },
            new Spec { name = "CabinetDrawers",     category = "Furniture", source = PH + "drawer_cabinet/drawer_cabinet.gltf" },
            new Spec { name = "ShelfWhite",         category = "Furniture", source = PH + "Shelf_01/Shelf_01.gltf" },
            new Spec { name = "Laptop",             category = "Props", source = PH + "classic_laptop/classic_laptop.gltf", fitAxis = 'x', fitSize = 0.27f },
            new Spec { name = "BookEncyclopediaA",  category = "Props", source = PH + "book_encyclopedia_set_01/book_encyclopedia_set_01.gltf", keep = new[] { "book01" }, preRotation = Quaternion.Euler(0f, 0f, 90f) },
            new Spec { name = "BookEncyclopediaB",  category = "Props", source = PH + "book_encyclopedia_set_01/book_encyclopedia_set_01.gltf", keep = new[] { "book13" }, preRotation = Quaternion.Euler(0f, 0f, 90f) },
            new Spec { name = "BinderNotebook",     category = "Props", source = PH + "binder_notebook/binder_notebook.gltf", keep = new[] { "closed" } },
            new Spec { name = "PenBlue",            category = "Props", source = PH + "stationery_supplies/stationery_supplies.gltf", keep = new[] { "pen_blue" }, maxTex = 512 },
            new Spec { name = "PenRed",             category = "Props", source = PH + "stationery_supplies/stationery_supplies.gltf", keep = new[] { "pen_red" }, maxTex = 512 },
            new Spec { name = "Clipboard",          category = "Props", source = PH + "clipboard/clipboard.gltf" },
            new Spec { name = "FireExtinguisher",   category = "Props", source = PH + "korean_fire_extinguisher_01/korean_fire_extinguisher_01.gltf" },
            new Spec { name = "WallClock",          category = "Props", source = PH + "wall_clock/wall_clock.gltf", pivot = Pivot.Center, fitAxis = 'x', fitSize = 0.45f },
            new Spec { name = "PottedPlant",        category = "Props", source = PH + "potted_plant_01/potted_plant_01.gltf", keep = new[] { "stem", "pot", "leaves" } },
            new Spec { name = "PottedPlantSmall",   category = "Props", source = PH + "potted_plant_04/potted_plant_04.gltf" },
            new Spec { name = "Corkboard",          category = "Props", source = Old + "Models/Props/Corkboard.glb", pivot = Pivot.Center, fitAxis = 'x', fitSize = 1.0f },
            new Spec { name = "BackpackGreen",      category = "Props", source = Old + "Models/Props/BackpackA.glb", fitSize = 0.40f },
            new Spec { name = "BackpackBlue",       category = "Props", source = Old + "Models/Props/BackpackB.glb", fitSize = 0.40f },
            new Spec { name = "ShoulderBag",        category = "Props", source = Old + "Models/Props/Bag.glb", fitSize = 0.30f },
            new Spec { name = "TrashBin",           category = "Props", source = Old + "Models/Props/TrashcanSmall.glb", fitSize = 0.50f },
            new Spec { name = "CeilingFan",         category = "Lighting", source = PH + "ceiling_fan/ceiling_fan.gltf", pivot = Pivot.TopCenter, maxTex = 2048 },
            new Spec { name = "FluorescentBatten",  category = "Lighting", source = PH + "mounted_fluorescent_lights/mounted_fluorescent_lights.gltf", keep = new[] { "_b", "_e" }, pivot = Pivot.TopCenter, fitAxis = 'x', fitSize = 1.22f, emissive = "glass" },
        };

        public static readonly string[] Characters =
        {
            "WomanA", "WomanCasual", "WomanTank", "WomanDress", "ManA", "ManB", "ManLongSleeves",
            "CasualCharacter", "HoodieCharacter", "BeachCharacter",
        };

        // ------------------------------------------------------------------ entry points
        public static string ConvertAllStatics()
        {
            var log = new System.Text.StringBuilder();
            foreach (var s in Statics)
            {
                try { log.AppendLine(ConvertStatic(s)); }
                catch (System.Exception e) { log.AppendLine(s.name + ": FAILED " + e.Message); }
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        public static string ConvertAllCharacters()
        {
            var log = new System.Text.StringBuilder();
            foreach (var c in Characters)
            {
                try { log.AppendLine(ConvertCharacter(c)); }
                catch (System.Exception e) { log.AppendLine(c + ": FAILED " + e.Message + "\n" + e.StackTrace); }
            }
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        // ------------------------------------------------------------------ static models
        public static string ConvertStatic(Spec s)
        {
            var gi = new GltfInfo(s.source);
            string srcBase = Path.GetFileNameWithoutExtension(s.source);
            string texDir = Root + "/Textures/" + s.category + "/" + s.name;
            var mats = new Dictionary<string, Material>();
            foreach (var gm in gi.Materials)
            {
                string shortName = Short(gm.name, srcBase);
                string matPath = Root + "/Materials/" + s.category + "/" + s.name + "_" + shortName + ".mat";
                mats[gm.name] = UrpMaterialFactory.Create(matPath, s.name + "_" + shortName, gi, gm, texDir, s.maxTex);
                if (s.emissive != null && shortName == s.emissive) MakeEmissiveTube(mats[gm.name]);
            }

            var srcAsset = AssetDatabase.LoadAssetAtPath<GameObject>(s.source);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(srcAsset);
            if (s.keep != null)
            {
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var kids = inst.GetComponentsInChildren<Transform>().Where(t => t != inst.transform).ToArray();
                foreach (var t in kids)
                {
                    if (t == null) continue;
                    if (!s.keep.Any(k => t.name.EndsWith(k)) && !t.GetComponentsInChildren<Transform>().Any(c => s.keep.Any(k => c.name.EndsWith(k))))
                        Object.DestroyImmediate(t.gameObject);
                }
            }
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;

            var rendMats = new List<KeyValuePair<string, Material[]>>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
                rendMats.Add(new KeyValuePair<string, Material[]>(r.name, r.sharedMaterials.Select(m => mats[m.name]).ToArray()));

            var wrapper = new GameObject(s.name);
            inst.transform.SetParent(wrapper.transform, false);
            inst.transform.localRotation = s.preRotation;
            float importScale = 1f;
            if (s.fitSize > 0f)
            {
                var size = Bounds(wrapper).size;
                importScale = s.fitSize / (s.fitAxis == 'x' ? size.x : size.y);
            }
            ExportAndBuild(wrapper, s.category, s.name, rendMats, s.pivot, importScale);
            return s.name + ": OK (" + gi.Materials.Length + " materials)";
        }

        /// <summary>Exports the instance to FBX (destroying it) and builds the prefab with pivot and materials.</summary>
        public static void ExportAndBuild(GameObject inst, string category, string name, List<KeyValuePair<string, Material[]>> rendMats, Pivot pivot, float importScale = 1f)
        {
            string fbxPath = Root + "/Models/" + category + "/" + name + ".fbx";
            Directory.CreateDirectory(Path.GetDirectoryName(fbxPath));
            ModelExporter.ExportObject(fbxPath, inst, new ExportModelOptions { ExportFormat = ExportFormat.Binary });
            Object.DestroyImmediate(inst);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport);
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importCameras = false; imp.importLights = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.globalScale = importScale;
            imp.SaveAndReimport();
            BuildPrefab(fbxPath, Root + "/Prefabs/" + category + "/" + name + ".prefab", name, rendMats, pivot, 0f);
        }

        // ------------------------------------------------------------------ characters
        public static string ConvertCharacter(string name)
        {
            string glb = Old + "Models/People/" + name + ".glb";
            string oldPrefab = Old + "Prefabs/Characters/" + name + ".prefab";
            var gi = new GltfInfo(glb);
            var mats = new Dictionary<string, Material>();
            foreach (var gm in gi.Materials)
                mats[gm.name] = UrpMaterialFactory.Create(Root + "/Materials/Characters/" + name + "_" + Short(gm.name, name) + ".mat", name + "_" + Short(gm.name, name), gi, gm, Root + "/Textures/Characters/" + name, 1024);

            var wrapper = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(oldPrefab));
            PrefabUtility.UnpackPrefabInstance(wrapper, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var model = wrapper.transform.GetChild(0).gameObject; // T-posed humanoid instance
            model.transform.SetParent(null, false);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            var animator = model.GetComponent<Animator>();
            if (animator != null) Object.DestroyImmediate(animator);
            Object.DestroyImmediate(wrapper);
            model.name = name;

            // These rigs skin the shoes to IK-target bones (Foot.L/R) that sit beside the leg chain, so they do not follow retargeted
            // animation. Parent each of them to its ankle leaf (same world pose) so the shoes travel with the legs.
            foreach (var side in new[] { "L", "R" })
            {
                var ankle = FindByName(model.transform, "LowerLeg." + side + "_end");
                var foot = FindByName(model.transform, "Foot." + side);
                if (ankle != null && foot != null) foot.SetParent(ankle, true);
            }

            var rendMats = new List<KeyValuePair<string, Material[]>>();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                rendMats.Add(new KeyValuePair<string, Material[]>(r.name, r.sharedMaterials.Select(m => mats[m.name]).ToArray()));

            string fbxPath = Root + "/Models/Characters/" + name + ".fbx";
            Directory.CreateDirectory(Path.GetDirectoryName(fbxPath));
            ModelExporter.ExportObject(fbxPath, model, new ExportModelOptions { ExportFormat = ExportFormat.Binary });
            Object.DestroyImmediate(model);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceSynchronousImport);

            // humanoid avatar generated by the importer
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.importCameras = false; imp.importLights = false;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            var desc = HumanoidSetup.Describe(probe.transform);
            Object.DestroyImmediate(probe);
            if (desc == null) return name + ": no humanoid bone map";
            imp.humanDescription = desc.Value;
            imp.SaveAndReimport();

            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath)) if (o is Avatar a) avatar = a;
            if (avatar == null || !avatar.isValid || !avatar.isHuman) return name + ": avatar INVALID";

            string prefabPath = Root + "/Prefabs/Characters/" + name + ".prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            var root = new GameObject(name);
            var child = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            child.transform.SetParent(root.transform, false);
            child.name = "Model";
            child.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // animated humanoids face -Z after retargeting
            AssignMaterials(child.transform, rendMats);
            var anim = child.GetComponent<Animator>();
            if (anim == null) anim = child.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return name + ": OK (avatar valid, " + gi.Materials.Length + " materials)";
        }

        // ------------------------------------------------------------------ helpers
        static void BuildPrefab(string fbxPath, string prefabPath, string name, List<KeyValuePair<string, Material[]>> rendMats, Pivot pivot, float unused)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject(name);
            var child = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            child.transform.SetParent(root.transform, false);
            AssignMaterials(child.transform, rendMats);
            var b = Bounds(child);
            Vector3 pv = pivot == Pivot.BottomCenter ? new Vector3(b.center.x, b.min.y, b.center.z)
                       : pivot == Pivot.TopCenter ? new Vector3(b.center.x, b.max.y, b.center.z) : b.center;
            child.transform.localPosition = -pv;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>The exporter can reorder siblings and suffixes duplicate names (_1), so materials are matched by renderer name first, then by position.</summary>
        static void AssignMaterials(Transform instRoot, List<KeyValuePair<string, Material[]>> rendMats)
        {
            var renderers = instRoot.GetComponentsInChildren<Renderer>();
            var used = new bool[rendMats.Count];
            var unresolved = new List<Renderer>();
            foreach (var r in renderers)
            {
                int hit = FindEntry(rendMats, used, r.name);
                if (hit < 0)
                {
                    var stripped = System.Text.RegularExpressions.Regex.Replace(r.name, "_\\d+$", "");
                    hit = FindEntry(rendMats, used, stripped);
                }
                if (hit >= 0) { used[hit] = true; r.sharedMaterials = rendMats[hit].Value; }
                else unresolved.Add(r);
            }
            int next = 0;
            foreach (var r in unresolved)
            {
                while (next < used.Length && used[next]) next++;
                if (next >= used.Length) { Debug.LogWarning("[Converter] no material entry left for " + r.name); continue; }
                used[next] = true;
                r.sharedMaterials = rendMats[next].Value;
            }
        }

        static int FindEntry(List<KeyValuePair<string, Material[]>> list, bool[] used, string name)
        {
            for (int i = 0; i < list.Count; i++) if (!used[i] && list[i].Key == name) return i;
            return -1;
        }

        static string RelPath(Transform root, Transform t)
        {
            if (t == root) return "";
            var parts = new List<string>();
            for (var c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void MakeEmissiveTube(Material m)
        {
            m.SetColor("_BaseColor", new Color(0.95f, 0.95f, 0.93f, 1f));
            m.SetFloat("_Surface", 0f); m.SetFloat("_ZWrite", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Opaque");
            m.renderQueue = -1;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(2.2f, 2.15f, 2.0f));
            EditorUtility.SetDirty(m);
        }

        static Transform FindByName(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static string Short(string matName, string modelBase)
        {
            if (matName == modelBase) return "Main";
            if (matName.StartsWith(modelBase + "_")) return matName.Substring(modelBase.Length + 1);
            return matName.Replace(' ', '_');
        }
    }
}
#endif
