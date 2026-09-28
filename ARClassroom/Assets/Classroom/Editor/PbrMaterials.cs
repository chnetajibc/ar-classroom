#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARClassroom.EditorTools
{
    /// <summary>Builds URP/Lit materials from Poly Haven "Diffuse / nor_gl / arm" texture sets.</summary>
    public static class PbrMaterials
    {
        const string TexRoot = "Assets/Classroom/Textures";
        const string MatDir = "Assets/Classroom/Materials";

        public static Material FromPolyHaven(string id, string matName, Color tint, float normalScale = 1f, float smoothnessScale = 1f)
        {
            Directory.CreateDirectory(MatDir);
            var matPath = MatDir + "/" + matName + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var mat = existing != null ? existing : new Material(Shader.Find("Universal Render Pipeline/Lit"));

            string dir = TexRoot + "/" + id;
            string metalPath = dir + "/" + id + "_MetalSmooth.png";
            string aoPath = dir + "/" + id + "_Occlusion.png";
            if (!File.Exists(metalPath) || !File.Exists(aoPath)) SplitArm(dir + "/" + id + "_arm.jpg", metalPath, aoPath);

            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + id + "_Diffuse.jpg"));
            mat.SetColor("_BaseColor", tint);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/" + id + "_nor_gl.jpg"));
            mat.SetFloat("_BumpScale", normalScale);
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metalPath));
            mat.SetFloat("_Smoothness", smoothnessScale);
            mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath));
            mat.SetFloat("_OcclusionStrength", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.EnableKeyword("_OCCLUSIONMAP");
            if (existing == null) AssetDatabase.CreateAsset(mat, matPath);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>ARM (AO in R, roughness in G, metal in B) -> URP layout: metal R + smoothness A, and AO in G.</summary>
        static void SplitArm(string armPath, string metalOut, string aoOut)
        {
            var arm = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            arm.LoadImage(File.ReadAllBytes(armPath));
            var src = arm.GetPixels32();
            var metal = new Color32[src.Length];
            var ao = new Color32[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var p = src[i];
                metal[i] = new Color32(p.b, p.b, p.b, (byte)(255 - p.g));
                ao[i] = new Color32(p.r, p.r, p.r, 255);
            }
            WritePng(metalOut, metal, arm.width, arm.height);
            WritePng(aoOut, ao, arm.width, arm.height);
            Object.DestroyImmediate(arm);
            AssetDatabase.ImportAsset(metalOut);
            AssetDatabase.ImportAsset(aoOut);
        }

        static void WritePng(string path, Color32[] px, int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.SetPixels32(px);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
    }
}
#endif
