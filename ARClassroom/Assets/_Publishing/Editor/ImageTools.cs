#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>CPU image helpers for the converter (load, box-downsample, channel packing, PNG/JPG encode).</summary>
    public static class ImageTools
    {
        public static Texture2D Load(byte[] encoded)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            t.LoadImage(encoded, false);
            return t;
        }

        /// <summary>Halves the image until it is at most maxSize wide/high (exact 2x2 box filter).</summary>
        public static Color32[] Downsample(Color32[] px, ref int w, ref int h, int maxSize)
        {
            while (w > maxSize && h > maxSize && w % 2 == 0 && h % 2 == 0)
            {
                int nw = w / 2, nh = h / 2;
                var o = new Color32[nw * nh];
                for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                {
                    var a = px[(2 * y) * w + 2 * x]; var b = px[(2 * y) * w + 2 * x + 1];
                    var c = px[(2 * y + 1) * w + 2 * x]; var d = px[(2 * y + 1) * w + 2 * x + 1];
                    o[y * nw + x] = new Color32(
                        (byte)((a.r + b.r + c.r + d.r + 2) >> 2), (byte)((a.g + b.g + c.g + d.g + 2) >> 2),
                        (byte)((a.b + b.b + c.b + d.b + 2) >> 2), (byte)((a.a + b.a + c.a + d.a + 2) >> 2));
                }
                px = o; w = nw; h = nh;
            }
            return px;
        }

        public static void Write(string assetPath, Color32[] px, int w, int h, bool jpg, int quality = 92)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.SetPixels32(px);
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, jpg ? t.EncodeToJPG(quality) : t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        /// <summary>Writes a colour texture (encoded source bytes), optionally downsampled; returns the asset path.</summary>
        public static string ExportColor(byte[] encoded, string assetPathNoExt, int maxSize, bool preferJpg)
        {
            var t = Load(encoded);
            int w = t.width, h = t.height;
            var px = Downsample(t.GetPixels32(), ref w, ref h, maxSize);
            bool hasAlpha = false;
            foreach (var p in px) if (p.a < 255) { hasAlpha = true; break; }
            bool jpg = preferJpg && !hasAlpha;
            var path = assetPathNoExt + (jpg ? ".jpg" : ".png");
            Write(path, px, w, h, jpg);
            Object.DestroyImmediate(t);
            return path;
        }

        public static void SetImport(string path, bool srgb, bool normalMap, int maxSize = 2048)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb && !normalMap;
            ti.maxTextureSize = maxSize;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.SaveAndReimport();
        }
    }
}
#endif
