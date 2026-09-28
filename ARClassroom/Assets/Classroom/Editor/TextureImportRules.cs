#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ARClassroom.EditorTools
{
    /// <summary>Applies the correct import settings to the PBR texture sets we ship (Poly Haven naming).</summary>
    public class TextureImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Classroom/Textures/") && !assetPath.StartsWith("Assets/Classroom/Models/SchoolProps/Textures/")) return;
            var ti = (TextureImporter)assetImporter;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (name.Contains("_nor_gl") || name.EndsWith("_normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (name.EndsWith("_arm") || name.Contains("metallicsmoothness") || name.EndsWith("_roughness") || name.EndsWith("_metallic"))
            {
                ti.sRGBTexture = false;
            }
            if (assetPath.EndsWith(".hdr"))
            {
                ti.textureShape = TextureImporterShape.Texture2D;
                ti.sRGBTexture = false;
                ti.maxTextureSize = 2048;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else
            {
                ti.anisoLevel = 8;
                ti.mipmapEnabled = true;
            }
        }
    }
}
#endif
