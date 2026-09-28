#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealisticClassroom.Publishing
{
    /// <summary>
    /// Local pre-flight check against the Unity Asset Store submission guidelines (file types, paths, prefab transforms, materials, namespaces,
    /// documentation, duplicates, unused assets) plus the statistics needed for the listing. It does NOT replace the official
    /// Asset Store Publishing Tools validator (Tools > Asset Store > Validator), which must still be run before uploading.
    /// </summary>
    public static class PackageValidator
    {
        const string Root = ModelConverter.Root;

        class Line { public string status, check, detail; }

        public static string Run(string reportPath)
        {
            var lines = new List<Line>();
            void Add(string status, string check, string detail) { lines.Add(new Line { status = status, check = check, detail = detail }); }

            var all = AssetDatabase.FindAssets("", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p)).Distinct().ToList();
            Add("INFO", "Assets in package", all.Count + " files");

            // 1. path length
            var longPaths = all.Where(p => p.Length >= 150).ToList();
            Add(longPaths.Count == 0 ? "PASS" : "FAIL", "File paths under 150 characters", longPaths.Count == 0 ? "longest = " + all.Max(p => p.Length) + " chars" : string.Join("; ", longPaths.Take(5)));

            // 2. forbidden formats
            var badExt = all.Where(p => new[] { ".glb", ".gltf", ".blend", ".max", ".psd", ".exe", ".dll", ".zip", ".unitypackage", ".DS_Store" }.Any(e => p.EndsWith(e, StringComparison.OrdinalIgnoreCase))).ToList();
            Add(badExt.Count == 0 ? "PASS" : "FAIL", "Mesh files are FBX/OBJ (no glTF/GLB/blend), no binaries or archives", badExt.Count == 0 ? "ok" : string.Join("; ", badExt.Take(5)));

            // 3. prefabs
            var prefabs = all.Where(p => p.EndsWith(".prefab")).ToList();
            var badPrefabs = new List<string>();
            var missingScripts = new List<string>();
            var nullMats = new List<string>();
            foreach (var p in prefabs)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                var t = go.transform;
                if (t.position != Vector3.zero || t.rotation != Quaternion.identity || t.localScale != Vector3.one) badPrefabs.Add(p);
                foreach (var c in go.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(c.gameObject) > 0) { missingScripts.Add(p); break; }
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterials.Any(m => m == null)) { nullMats.Add(p); break; }
            }
            Add(badPrefabs.Count == 0 ? "PASS" : "FAIL", "Prefab roots at position 0, rotation 0, scale 1", badPrefabs.Count == 0 ? prefabs.Count + " prefabs" : string.Join("; ", badPrefabs));
            Add(missingScripts.Count == 0 ? "PASS" : "FAIL", "No missing scripts in prefabs", missingScripts.Count == 0 ? "ok" : string.Join("; ", missingScripts));
            Add(nullMats.Count == 0 ? "PASS" : "FAIL", "Every renderer slot has a material", nullMats.Count == 0 ? "ok" : string.Join("; ", nullMats));

            // 4. materials
            var mats = all.Where(p => p.EndsWith(".mat")).Select(AssetDatabase.LoadAssetAtPath<Material>).ToList();
            var badShader = mats.Where(m => m.shader == null || m.shader.name.Contains("InternalError") || !(m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name.StartsWith("Skybox/"))).ToList();
            Add(badShader.Count == 0 ? "PASS" : "FAIL", "All materials use URP/Lit (or the skybox shader)", badShader.Count == 0 ? mats.Count + " materials" : string.Join("; ", badShader.Select(m => m.name + "=" + m.shader.name)));
            var badNormal = new List<string>();
            foreach (var m in mats)
            {
                if (!m.HasProperty("_BumpMap")) continue;
                var tex = m.GetTexture("_BumpMap");
                if (tex == null) continue;
                var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) badNormal.Add(m.name);
            }
            Add(badNormal.Count == 0 ? "PASS" : "FAIL", "Normal maps are marked as Normal Map", badNormal.Count == 0 ? "ok" : string.Join("; ", badNormal));

            // 5. scripts
            var scripts = all.Where(p => p.EndsWith(".cs")).ToList();
            var noNamespace = new List<string>();
            foreach (var s in scripts)
            {
                var text = File.ReadAllText(s);
                if (!text.Contains("namespace ")) noNamespace.Add(s);
                if (text.Contains("namespace Unity") || text.Contains("namespace UnityEngine") && !s.Contains("InputSystem")) noNamespace.Add(s + " (Unity namespace)");
            }
            Add(noNamespace.Count == 0 ? "PASS" : "FAIL", "All code is in user-declared namespaces", noNamespace.Count == 0 ? scripts.Count + " scripts" : string.Join("; ", noNamespace));
            var asmdefs = all.Count(p => p.EndsWith(".asmdef"));
            Add(asmdefs >= 2 ? "PASS" : "WARN", "Assembly definitions present", asmdefs + " asmdef files");
            var domainReload = scripts.Where(p => !p.Contains("/Editor/")).Where(p => Regex(File.ReadAllText(p), @"^\s*(public\s+|private\s+|internal\s+)?static\s+(?!readonly|class|void|bool|int|float|string|Vector|Transform)[A-Za-z<>\[\], ]+\s+\w+\s*(=|;)")).ToList();
            Add("INFO", "Runtime static fields to review for Fast Enter Play Mode", domainReload.Count == 0 ? "none flagged by the pattern check (ClassroomInput resets its statics on SubsystemRegistration)" : string.Join("; ", domainReload));

            // 6. documentation
            bool docs = all.Any(p => p.Contains("/Documentation/") && (p.EndsWith(".pdf") || p.EndsWith(".html") || p.EndsWith(".md") || p.EndsWith(".txt")));
            Add(docs ? "PASS" : "FAIL", "Documentation included", docs ? "found" : "missing");
            bool notices = all.Any(p => p.EndsWith("Third-Party Notices.txt"));
            Add(notices ? "PASS" : "FAIL", "Third-Party Notices.txt included", notices ? "found" : "missing");
            bool changelog = all.Any(p => p.EndsWith("CHANGELOG.md"));
            Add(changelog ? "PASS" : "WARN", "CHANGELOG.md included", changelog ? "found" : "missing");
            bool demo = all.Any(p => p.EndsWith(".unity"));
            Add(demo ? "PASS" : "FAIL", "Demo scene included", demo ? "found" : "missing");

            // 7. duplicates (identical binary content)
            var dupGroups = all.Where(p => new[] { ".png", ".jpg", ".fbx", ".hdr", ".obj" }.Any(e => p.EndsWith(e))).GroupBy(HashOf).Where(g => g.Count() > 1).ToList();
            Add(dupGroups.Count == 0 ? "PASS" : "FAIL", "No duplicate files", dupGroups.Count == 0 ? "ok" : string.Join(" | ", dupGroups.Select(g => string.Join(" = ", g))));

            // 8. unused assets
            var referenced = new HashSet<string>();
            foreach (var p in all.Where(p => p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".controller") || p.EndsWith(".asset") && p.Contains("/Settings/")))
                foreach (var d in AssetDatabase.GetDependencies(p, true)) referenced.Add(d);
            var unused = all.Where(p => (p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".mat") || p.EndsWith(".fbx") || p.EndsWith(".hdr")) && !referenced.Contains(p)).ToList();
            Add(unused.Count == 0 ? "PASS" : "WARN", "No unused textures/materials/models", unused.Count == 0 ? "ok" : string.Join("; ", unused.Take(12)));

            // 9. empty folders
            var empty = AssetDatabase.GetSubFolders(Root).SelectMany(f => AllFolders(f)).Where(f => !AssetDatabase.FindAssets("", new[] { f }).Select(AssetDatabase.GUIDToAssetPath).Any(p => !AssetDatabase.IsValidFolder(p))).ToList();
            Add(empty.Count == 0 ? "PASS" : "WARN", "No empty folders", empty.Count == 0 ? "ok" : string.Join("; ", empty));

            // ---- statistics
            var sb = new StringBuilder();
            sb.AppendLine("# Package pre-flight report");
            sb.AppendLine();
            sb.AppendLine("Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " with Unity " + Application.unityVersion + ". This is a local approximation of the Asset Store guidelines; run **Tools > Asset Store > Validator** (Asset Store Publishing Tools) before uploading.");
            sb.AppendLine();
            sb.AppendLine("| Status | Check | Detail |");
            sb.AppendLine("|---|---|---|");
            foreach (var l in lines) sb.AppendLine("| " + l.status + " | " + l.check + " | " + l.detail.Replace("|", "/") + " |");
            sb.AppendLine();
            sb.AppendLine(Stats(prefabs));

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, sb.ToString());
            int fails = lines.Count(l => l.status == "FAIL"), warns = lines.Count(l => l.status == "WARN");
            return "validation: " + fails + " fail, " + warns + " warn -> " + reportPath;
        }

        static bool Regex(string text, string pattern) { return System.Text.RegularExpressions.Regex.IsMatch(text, pattern, System.Text.RegularExpressions.RegexOptions.Multiline); }

        static IEnumerable<string> AllFolders(string f)
        {
            yield return f;
            foreach (var s in AssetDatabase.GetSubFolders(f)) foreach (var x in AllFolders(s)) yield return x;
        }

        static readonly Dictionary<string, string> hashCache = new Dictionary<string, string>();
        static string HashOf(string assetPath)
        {
            using (var md5 = MD5.Create())
            using (var fs = File.OpenRead(assetPath)) return BitConverter.ToString(md5.ComputeHash(fs));
        }

        public static string Stats(List<string> prefabs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Prefab statistics");
            sb.AppendLine();
            sb.AppendLine("| Prefab | Triangles | Vertices | Materials | Size (m) W x H x D |");
            sb.AppendLine("|---|---:|---:|---:|---|");
            long totalTris = 0;
            var textures = new HashSet<string>();
            foreach (var p in prefabs.Where(p => !p.Contains("Classroom_Complete")).OrderBy(p => p))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
                long tris = 0, verts = 0;
                var mats = new HashSet<Material>();
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    Mesh m = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (m != null) { tris += m.triangles.Length / 3; verts += m.vertexCount; }
                    foreach (var mat in r.sharedMaterials) if (mat != null) mats.Add(mat);
                }
                foreach (var mat in mats)
                    foreach (var n in mat.GetTexturePropertyNames()) { var t = mat.GetTexture(n); if (t != null) textures.Add(AssetDatabase.GetAssetPath(t)); }
                var b = ModelConverter.Bounds(go);
                sb.AppendLine("| " + p.Replace(Root + "/Prefabs/", "").Replace(".prefab", "") + " | " + tris.ToString("N0", CultureInfo.InvariantCulture) + " | " + verts.ToString("N0", CultureInfo.InvariantCulture) + " | " + mats.Count + " | " + b.size.x.ToString("F2", CultureInfo.InvariantCulture) + " x " + b.size.y.ToString("F2", CultureInfo.InvariantCulture) + " x " + b.size.z.ToString("F2", CultureInfo.InvariantCulture) + " |");
                totalTris += tris;
                UnityEngine.Object.DestroyImmediate(go);
            }
            sb.AppendLine();
            sb.AppendLine("Unique prefabs listed: " + prefabs.Count(p => !p.Contains("Classroom_Complete")) + ", unique textures used: " + textures.Count + ".");
            var sizes = textures.Select(t => AssetDatabase.LoadAssetAtPath<Texture2D>(t)).Where(t => t != null).GroupBy(t => t.width + "x" + t.height).OrderByDescending(g => g.Count());
            sb.AppendLine();
            sb.AppendLine("Texture resolutions: " + string.Join(", ", sizes.Select(g => g.Key + " (" + g.Count() + ")")) + ".");
            return sb.ToString();
        }

        /// <summary>Triangle/renderer totals of the demo scene (loaded scene).</summary>
        public static string SceneStats()
        {
            long tris = 0; int renderers = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
            {
                Mesh m = r is SkinnedMeshRenderer smr ? smr.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                if (m != null) tris += m.triangles.Length / 3;
                renderers++;
            }
            int lights = UnityEngine.Object.FindObjectsByType<Light>().Length;
            return "scene: " + renderers + " renderers, " + tris.ToString("N0", CultureInfo.InvariantCulture) + " triangles, " + lights + " lights";
        }
    }
}
#endif
