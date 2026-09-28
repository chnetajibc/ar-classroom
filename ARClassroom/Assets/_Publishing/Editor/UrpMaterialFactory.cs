#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticClassroom.Publishing
{
    /// <summary>Turns glTF PBR material descriptors into standalone URP/Lit materials with URP-layout texture maps.</summary>
    public static class UrpMaterialFactory
    {
        public static Material Create(string matPath, string namePrefix, GltfInfo gi, GltfInfo.Material gm, string textureDir, int maxTex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(matPath));
            AssetDatabase.DeleteAsset(matPath);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Path.GetFileNameWithoutExtension(matPath) };
            mat.SetColor("_BaseColor", gm.baseColor.gamma);
            string tag = namePrefix;

            if (gm.baseTex >= 0)
            {
                var bytes = gi.ImageBytes(gm.baseTex, out var ext);
                var p = ImageTools.ExportColor(bytes, textureDir + "/" + tag + "_BaseColor", maxTex, true);
                ImageTools.SetImport(p, true, false, maxTex);
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p));
            }

            if (gm.normalTex >= 0)
            {
                var bytes = gi.ImageBytes(gm.normalTex, out var ext);
                var p = ImageTools.ExportColor(bytes, textureDir + "/" + tag + "_Normal", maxTex, true);
                ImageTools.SetImport(p, false, true, maxTex);
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p));
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (gm.mrTex >= 0)
            {
                // glTF: G = roughness, B = metallic  ->  URP: R = metallic, A = smoothness
                var mr = ImageTools.Load(gi.ImageBytes(gm.mrTex, out _));
                int w = mr.width, h = mr.height;
                int mapMax = Mathf.Max(512, maxTex / 2);
                var src = ImageTools.Downsample(mr.GetPixels32(), ref w, ref h, mapMax);
                var metal = new Color32[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    byte m = (byte)Mathf.Clamp(Mathf.RoundToInt(src[i].b * gm.metallic), 0, 255);
                    byte s = (byte)Mathf.Clamp(Mathf.RoundToInt(255f - src[i].g * gm.roughness), 0, 255);
                    metal[i] = new Color32(m, m, m, s);
                }
                var mp = textureDir + "/" + tag + "_MetalSmooth.png";
                ImageTools.Write(mp, metal, w, h, false);
                ImageTools.SetImport(mp, false, false, mapMax);
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mp));
                mat.SetFloat("_Smoothness", 1f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                Object.DestroyImmediate(mr);

                if (gm.occTex >= 0)
                {
                    var oc = gm.occTex == gm.mrTex ? null : ImageTools.Load(gi.ImageBytes(gm.occTex, out _));
                    var opx = oc != null ? oc.GetPixels32() : null;
                    int ow = w, oh = h;
                    if (oc != null) { ow = oc.width; oh = oc.height; opx = ImageTools.Downsample(opx, ref ow, ref oh, mapMax); }
                    var mrFull = ImageTools.Load(gi.ImageBytes(gm.mrTex, out _));
                    int fw = mrFull.width, fh = mrFull.height;
                    var basePx = ImageTools.Downsample(mrFull.GetPixels32(), ref fw, ref fh, mapMax);
                    var ao = new Color32[opx != null ? opx.Length : basePx.Length];
                    for (int i = 0; i < ao.Length; i++)
                    {
                        byte a = opx != null ? opx[i].r : basePx[i].r;
                        ao[i] = new Color32(a, a, a, 255);
                    }
                    var ap = textureDir + "/" + tag + "_Occlusion.jpg";
                    ImageTools.Write(ap, ao, opx != null ? ow : fw, opx != null ? oh : fh, true, 93);
                    ImageTools.SetImport(ap, false, false, mapMax);
                    mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ap));
                    mat.EnableKeyword("_OCCLUSIONMAP");
                    Object.DestroyImmediate(mrFull);
                    if (oc != null) Object.DestroyImmediate(oc);
                }
            }
            else
            {
                mat.SetFloat("_Metallic", gm.metallic);
                mat.SetFloat("_Smoothness", 1f - gm.roughness);
            }

            if (gm.emissiveTex >= 0 || gm.emissive.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                mat.SetColor("_EmissionColor", gm.emissive);
                if (gm.emissiveTex >= 0)
                {
                    var p = ImageTools.ExportColor(gi.ImageBytes(gm.emissiveTex, out _), textureDir + "/" + tag + "_Emissive", maxTex, true);
                    ImageTools.SetImport(p, true, false, maxTex);
                    mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p));
                    if (gm.emissive.maxColorComponent <= 0.001f) mat.SetColor("_EmissionColor", Color.white);
                }
            }

            if (gm.doubleSided) mat.SetFloat("_Cull", 0f);
            if (gm.alphaMode == "BLEND") MakeTransparent(mat);
            else if (gm.alphaMode == "MASK")
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", gm.alphaCutoff);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        /// <summary>Material from a Poly Haven PBR texture set (Diffuse / nor_gl / arm jpgs), repacked into the URP layout.</summary>
        public static Material CreateFromPolyHavenSet(string setDir, string id, string matPath, string namePrefix, string textureDir, Color srgbTint, int maxTex, float normalScale = 1f, float smoothnessScale = 1f)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(matPath));
            AssetDatabase.DeleteAsset(matPath);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Path.GetFileNameWithoutExtension(matPath) };
            mat.SetColor("_BaseColor", srgbTint);

            var bp = ImageTools.ExportColor(File.ReadAllBytes(setDir + "/" + id + "_Diffuse.jpg"), textureDir + "/" + namePrefix + "_BaseColor", maxTex, true);
            ImageTools.SetImport(bp, true, false, maxTex);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(bp));

            var np = ImageTools.ExportColor(File.ReadAllBytes(setDir + "/" + id + "_nor_gl.jpg"), textureDir + "/" + namePrefix + "_Normal", maxTex, true);
            ImageTools.SetImport(np, false, true, maxTex);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(np));
            mat.SetFloat("_BumpScale", normalScale);
            mat.EnableKeyword("_NORMALMAP");

            var arm = ImageTools.Load(File.ReadAllBytes(setDir + "/" + id + "_arm.jpg"));
            int w = arm.width, h = arm.height;
            int mapMax = Mathf.Max(512, maxTex / 2);
            var px = ImageTools.Downsample(arm.GetPixels32(), ref w, ref h, mapMax);
            var metal = new Color32[px.Length];
            var ao = new Color32[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                metal[i] = new Color32(px[i].b, px[i].b, px[i].b, (byte)(255 - px[i].g));
                ao[i] = new Color32(px[i].r, px[i].r, px[i].r, 255);
            }
            var mp = textureDir + "/" + namePrefix + "_MetalSmooth.png";
            var ap = textureDir + "/" + namePrefix + "_Occlusion.jpg";
            ImageTools.Write(mp, metal, w, h, false); ImageTools.SetImport(mp, false, false, mapMax);
            ImageTools.Write(ap, ao, w, h, true, 93); ImageTools.SetImport(ap, false, false, mapMax);
            Object.DestroyImmediate(arm);
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mp));
            mat.SetFloat("_Smoothness", smoothnessScale);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ap));
            mat.EnableKeyword("_OCCLUSIONMAP");
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        public static Material CreateFlat(string matPath, Color srgbColor, float metallic, float smoothness)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(matPath));
            AssetDatabase.DeleteAsset(matPath);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = Path.GetFileNameWithoutExtension(matPath) };
            mat.SetColor("_BaseColor", srgbColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (srgbColor.a < 0.99f) MakeTransparent(mat);
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        public static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
#endif
