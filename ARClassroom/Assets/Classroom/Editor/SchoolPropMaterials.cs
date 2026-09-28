#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARClassroom.EditorTools
{
    /// <summary>
    /// The Anthon school-props FBX ship Unity Standard-style texture names ("Model_Material_Map.png").
    /// Wire them into URP/Lit materials and save one ready-to-use prefab per model.
    /// </summary>
    public static class SchoolPropMaterials
    {
        const string ModelDir = "Assets/Classroom/Models/SchoolProps/Models";
        const string TexDir = "Assets/Classroom/Models/SchoolProps/Textures";
        const string MatDir = "Assets/Classroom/Materials/SchoolProps";
        public const string PrefabDir = "Assets/Classroom/Prefabs/Props";

        public static string Build()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PrefabDir);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var report = new System.Text.StringBuilder();
            foreach (var fbx in Directory.GetFiles(ModelDir, "*.fbx"))
            {
                var path = fbx.Replace('\\', '/');
                var modelName = Path.GetFileNameWithoutExtension(path);
                var prefix = FindPrefix(modelName);
                if (prefix == null) { report.AppendLine(modelName + ": no textures"); continue; }

                var mat = new Material(shader) { name = prefix };
                var albedo = Tex(prefix + "_AlbedoTransparency") ?? Tex(prefix + "_BaseColor");
                if (albedo != null) mat.SetTexture("_BaseMap", albedo);
                var ms = Tex(prefix + "_MetallicSmoothness") ?? Tex(prefix + "_MetallicSmoothness 1");
                if (ms != null)
                {
                    mat.SetTexture("_MetallicGlossMap", ms);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetFloat("_Smoothness", 1f);
                }
                var nrm = Tex(prefix + "_Normal");
                if (nrm != null)
                {
                    mat.SetTexture("_BumpMap", nrm);
                    mat.EnableKeyword("_NORMALMAP");
                }
                var matPath = MatDir + "/" + prefix + ".mat";
                AssetDatabase.DeleteAsset(matPath);
                AssetDatabase.CreateAsset(mat, matPath);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    r.sharedMaterials = mats;
                }
                PrefabUtility.SaveAsPrefabAsset(inst, PrefabDir + "/" + modelName + ".prefab");
                Object.DestroyImmediate(inst);
                report.AppendLine(modelName + " <- " + prefix + " albedo=" + (albedo != null) + " ms=" + (ms != null) + " nrm=" + (nrm != null));
            }
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        static string FindPrefix(string modelName)
        {
            foreach (var f in Directory.GetFiles(TexDir, modelName + "_*.png"))
            {
                var n = Path.GetFileNameWithoutExtension(f);
                foreach (var suffix in new[] { "_AlbedoTransparency", "_BaseColor" })
                    if (n.EndsWith(suffix)) return n.Substring(0, n.Length - suffix.Length);
            }
            return null;
        }

        static Texture2D Tex(string nameNoExt)
        {
            var p = TexDir + "/" + nameNoExt + ".png";
            return File.Exists(p) ? AssetDatabase.LoadAssetAtPath<Texture2D>(p) : null;
        }
    }
}
#endif
