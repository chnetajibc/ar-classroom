#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>Kenney wall/floor/door modules + the CC0 chalkboard mesh: re-scaled to real-world metres, UV-mapped for tiling PBR textures, exported as FBX.</summary>
    public static class ArchitectureConverter
    {
        const string Root = ModelConverter.Root;
        const string Old = "Assets/_Publishing/Sources/";
        const string KenneyDir = "Assets/_Publishing/Sources/Kenney/";
        const float ModuleScale = 0.25f; // Kenney file: 10 units per module -> 2.5 m

        public static string ConvertAll()
        {
            UrpMaterialFactory.ResetCache();
            var log = new System.Text.StringBuilder();
            var tint = new Color(0.88f, 0.93f, 0.86f);
            string texDir = Root + "/Textures/Architecture";
            var wall = UrpMaterialFactory.CreateFromPolyHavenSet(Old + "Textures/plastered_wall_04", "plastered_wall_04", Root + "/Materials/Architecture/Wall_Plaster.mat", "Wall_Plaster", texDir, tint, 2048, 0.6f, 0.5f);
            var floor = UrpMaterialFactory.CreateFromPolyHavenSet(Old + "Textures/laminate_floor_02", "laminate_floor_02", Root + "/Materials/Architecture/Floor_Laminate.mat", "Floor_Laminate", texDir, new Color(0.92f, 0.88f, 0.84f), 2048, 0.8f, 1f);
            // the ceiling shares the wall plaster textures (own tint and finish), so no texture is duplicated
            var ceilingPath = Root + "/Materials/Architecture/Ceiling_Plaster.mat";
            AssetDatabase.DeleteAsset(ceilingPath);
            var ceiling = new Material(wall) { name = "Ceiling_Plaster" };
            ceiling.SetColor("_BaseColor", new Color(1f, 1f, 0.98f));
            ceiling.SetFloat("_BumpScale", 0.4f);
            ceiling.SetFloat("_Smoothness", 0.3f);
            AssetDatabase.CreateAsset(ceiling, ceilingPath);
            var slate = UrpMaterialFactory.CreateFromPolyHavenSet(Old + "Textures/slate_floor_03", "slate_floor_03", Root + "/Materials/Architecture/Blackboard_Surface.mat", "Blackboard_Surface", texDir, new Color(0.26f, 0.33f, 0.28f), 1024, 0.5f, 0.35f);
            var frame = UrpMaterialFactory.CreateFromPolyHavenSet(Old + "Textures/dark_wood", "dark_wood", Root + "/Materials/Architecture/Blackboard_Frame.mat", "Blackboard_Frame", texDir, Color.white, 1024, 1f, 0.8f);

            var trimWood = UrpMaterialFactory.CreateFlat(Root + "/Materials/Architecture/Trim_Wood.mat", new Color(0.953f, 0.799f, 0.660f), 0f, 0.35f);
            var trimDark = UrpMaterialFactory.CreateFlat(Root + "/Materials/Architecture/Trim_MetalDark.mat", new Color(0.589f, 0.656f, 0.656f), 0.6f, 0.45f);
            var metal = UrpMaterialFactory.CreateFlat(Root + "/Materials/Architecture/Door_Metal.mat", new Color(0.876f, 0.918f, 0.926f), 0.5f, 0.5f);
            var doorFrame = UrpMaterialFactory.CreateFlat(Root + "/Materials/Architecture/Door_Frame.mat", new Color(0.988f, 1f, 1f), 0f, 0.4f);
            var glass = UrpMaterialFactory.CreateFlat(Root + "/Materials/Architecture/Window_Glass.mat", new Color(0.853f, 0.920f, 0.890f, 0.16f), 0f, 0.7f);
            var byName = new Dictionary<string, Material>
            {
                { "wood", trimWood }, { "metalDark", trimDark }, { "metal", metal }, { "carpetWhite", doorFrame }, { "glass", glass },
            };

            log.AppendLine(Module("WallPlain", "wall", "_defaultMat", wall, 2.5f, byName, ModelConverter.Pivot.BottomCenter));
            log.AppendLine(Module("WallWindow", "wallWindow", "_defaultMat", wall, 2.5f, byName, ModelConverter.Pivot.BottomCenter));
            log.AppendLine(Module("WallDoorway", "wallDoorway", "_defaultMat", wall, 2.5f, byName, ModelConverter.Pivot.BottomCenter));
            log.AppendLine(Module("DoorLeafFrame", "doorwayFront", null, null, 2.5f, byName, ModelConverter.Pivot.BottomCenter));
            log.AppendLine(Module("FloorTile", "floorFull", "wood", floor, 1.25f, byName, ModelConverter.Pivot.TopCenter));
            log.AppendLine(Module("CeilingTile", "floorFull", "wood", ceiling, 2.5f, byName, ModelConverter.Pivot.TopCenter));
            log.AppendLine(Blackboard(slate, frame));
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        static string Module(string outName, string kenneyName, string replaceMat, Material replacement, float tile, Dictionary<string, Material> byName, ModelConverter.Pivot pivot)
        {
            string src = KenneyDir + kenneyName + ".fbx";
            var imp = (ModelImporter)AssetImporter.GetAtPath(src);
            if (!Mathf.Approximately(imp.globalScale, ModuleScale)) { imp.globalScale = ModuleScale; imp.SaveAndReimport(); }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(src));
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            var origin = ModelConverter.Bounds(inst).min;

            var rendMats = new List<KeyValuePair<string, Material[]>>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials.Select(m => (replaceMat != null && m.name == replaceMat) ? replacement : (byName.TryGetValue(m.name, out var mm) ? mm : null)).ToArray();
                var mf = r.GetComponent<MeshFilter>();
                if (replacement != null && r.sharedMaterials.Any(m => m.name == replaceMat))
                {
                    var newMesh = RetextureMesh(mf, r.sharedMaterials.Select(m => m.name).ToArray(), replaceMat, origin, tile, outName + "_" + mf.name);
                    mf.sharedMesh = newMesh;
                }
                for (int i = 0; i < mats.Length; i++) if (mats[i] == null) mats[i] = replacement != null ? replacement : mats[0];
                rendMats.Add(new KeyValuePair<string, Material[]>(r.name, mats));
                r.sharedMaterials = mats;
            }
            ModelConverter.ExportAndBuild(inst, "Architecture", outName, rendMats, pivot);
            return outName + ": OK";
        }

        /// <summary>Copy of the mesh where the triangles of one material get box-projected UVs (metres / tile) relative to the module corner.</summary>
        static Mesh RetextureMesh(MeshFilter mf, string[] slotNames, string replaceMat, Vector3 origin, float tile, string assetName)
        {
            var src = mf.sharedMesh;
            var mesh = Object.Instantiate(src);
            mesh.name = assetName;
            mesh.colors = null; mesh.tangents = null; mesh.SetUVs(1, (List<Vector2>)null);
            var verts = new List<Vector3>(); mesh.GetVertices(verts);
            var normals = new List<Vector3>(); mesh.GetNormals(normals);
            var uvs = new List<Vector2>(); mesh.GetUVs(0, uvs);
            if (uvs.Count < verts.Count) { uvs.Clear(); for (int i = 0; i < verts.Count; i++) uvs.Add(Vector2.zero); }
            var l2w = mf.transform.localToWorldMatrix;
            for (int sm = 0; sm < slotNames.Length; sm++)
            {
                if (slotNames[sm] != replaceMat) continue;
                var tris = new List<int>(); mesh.GetTriangles(tris, sm);
                var remap = new Dictionary<int, int>();
                for (int t = 0; t < tris.Count; t++)
                {
                    int old = tris[t];
                    if (!remap.TryGetValue(old, out int nv))
                    {
                        nv = verts.Count; remap[old] = nv;
                        verts.Add(verts[old]); normals.Add(normals[old]);
                        var wp = l2w.MultiplyPoint3x4(verts[old]) - origin;
                        var wn = l2w.MultiplyVector(normals[old]).normalized;
                        uvs.Add(Box(wp, wn) / tile);
                    }
                    tris[t] = nv;
                }
                mesh.SetVertices(verts); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, sm);
            }
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector2 Box(Vector3 p, Vector3 n)
        {
            var a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (a.y >= a.x && a.y >= a.z) return new Vector2(p.x, p.z);
            if (a.x >= a.z) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }

        static string Blackboard(Material slate, Material frame)
        {
            // stage the OBJ with a cleaned MTL (the original points at C:/ photo textures) so Unity keeps one submesh per material
            string stage = "Assets/_Publishing/Sources/BoardStage";
            Directory.CreateDirectory(stage);
            string objSrc = Old + "Models/Board/Pizarron.obj";
            File.Copy(objSrc, stage + "/Pizarron.obj", true);
            File.WriteAllText(stage + "/Pizarron.mtl", "newmtl chalkboard\nKd 0.2 0.3 0.25\n\nnewmtl wood\nKd 0.4 0.25 0.15\n");
            AssetDatabase.ImportAsset(stage + "/Pizarron.obj", ImportAssetOptions.ForceSynchronousImport);
            var imp = (ModelImporter)AssetImporter.GetAtPath(stage + "/Pizarron.obj");
            imp.globalScale = 0.25f; // 12.28 x 6.08 units -> 3.07 x 1.52 m
            imp.SaveAndReimport();

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(stage + "/Pizarron.obj"));
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            var rendMats = new List<KeyValuePair<string, Material[]>>();
            var log = new System.Text.StringBuilder("Blackboard: ");
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials.Select(m => m.name.ToLowerInvariant().Contains("chalk") ? slate : frame).ToArray();
                log.Append(string.Join(",", r.sharedMaterials.Select(m => m.name)) + " ");
                r.sharedMaterials = mats;
                rendMats.Add(new KeyValuePair<string, Material[]>(r.name, mats));
            }
            ModelConverter.ExportAndBuild(inst, "Architecture", "Blackboard", rendMats, ModelConverter.Pivot.BottomCenter);
            return log.ToString() + "OK";
        }
    }
}
#endif
