#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>Minimal glTF / GLB reader used by the one-off asset converter: material descriptors and embedded image bytes.</summary>
    public class GltfInfo
    {
        [Serializable] class Root { public GMat[] materials; public GTex[] textures; public GImg[] images; public GView[] bufferViews; }
        [Serializable] class GMat
        {
            public string name; public GPbr pbrMetallicRoughness; public GRef normalTexture = new GRef(); public GRef occlusionTexture = new GRef();
            public GRef emissiveTexture = new GRef(); public float[] emissiveFactor; public string alphaMode = "OPAQUE"; public float alphaCutoff = 0.5f; public bool doubleSided;
        }
        [Serializable] class GPbr { public float[] baseColorFactor; public GRef baseColorTexture = new GRef(); public GRef metallicRoughnessTexture = new GRef(); public float metallicFactor = 1f; public float roughnessFactor = 1f; }
        [Serializable] class GRef { public int index = -1; }
        [Serializable] class GTex { public int source = -1; }
        [Serializable] class GImg { public string uri; public string mimeType; public int bufferView = -1; }
        [Serializable] class GView { public int byteOffset; public int byteLength; }

        public class Material
        {
            public string name;
            public Color baseColor = Color.white; // linear factors
            public int baseTex = -1, normalTex = -1, mrTex = -1, occTex = -1, emissiveTex = -1; // image indices
            public float metallic = 1f, roughness = 1f;
            public Color emissive = Color.black;
            public string alphaMode = "OPAQUE";
            public float alphaCutoff = 0.5f;
            public bool doubleSided;
        }

        public Material[] Materials;
        readonly Root root;
        readonly string dir;
        readonly byte[] bin;

        public GltfInfo(string path)
        {
            dir = Path.GetDirectoryName(path);
            string json;
            if (path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            {
                var b = File.ReadAllBytes(path);
                int jsonLen = BitConverter.ToInt32(b, 12);
                json = Encoding.UTF8.GetString(b, 20, jsonLen);
                int binHeader = 20 + jsonLen;
                if (binHeader + 8 <= b.Length)
                {
                    int binLen = BitConverter.ToInt32(b, binHeader);
                    bin = new byte[binLen];
                    Buffer.BlockCopy(b, binHeader + 8, bin, 0, binLen);
                }
            }
            else json = File.ReadAllText(path);

            root = JsonUtility.FromJson<Root>(json);
            var list = new System.Collections.Generic.List<Material>();
            if (root.materials != null)
            {
                foreach (var m in root.materials)
                {
                    var mat = new Material { name = m.name, alphaMode = m.alphaMode, alphaCutoff = m.alphaCutoff, doubleSided = m.doubleSided };
                    if (m.pbrMetallicRoughness != null)
                    {
                        var p = m.pbrMetallicRoughness;
                        if (p.baseColorFactor != null && p.baseColorFactor.Length >= 3)
                            mat.baseColor = new Color(p.baseColorFactor[0], p.baseColorFactor[1], p.baseColorFactor[2], p.baseColorFactor.Length > 3 ? p.baseColorFactor[3] : 1f);
                        mat.metallic = p.metallicFactor; mat.roughness = p.roughnessFactor;
                        mat.baseTex = Image(p.baseColorTexture.index);
                        mat.mrTex = Image(p.metallicRoughnessTexture.index);
                    }
                    mat.normalTex = Image(m.normalTexture.index);
                    mat.occTex = Image(m.occlusionTexture.index);
                    mat.emissiveTex = Image(m.emissiveTexture.index);
                    if (m.emissiveFactor != null && m.emissiveFactor.Length >= 3) mat.emissive = new Color(m.emissiveFactor[0], m.emissiveFactor[1], m.emissiveFactor[2]);
                    list.Add(mat);
                }
            }
            Materials = list.ToArray();
        }

        int Image(int textureIndex)
        {
            if (textureIndex < 0 || root.textures == null || textureIndex >= root.textures.Length) return -1;
            return root.textures[textureIndex].source;
        }

        public Material Find(string name)
        {
            foreach (var m in Materials) if (m.name == name) return m;
            return null;
        }

        /// <summary>Raw encoded image bytes (jpg/png) and its extension.</summary>
        public byte[] ImageBytes(int imageIndex, out string extension)
        {
            extension = ".png";
            var img = root.images[imageIndex];
            if (!string.IsNullOrEmpty(img.uri))
            {
                var p = Path.Combine(dir, Uri.UnescapeDataString(img.uri));
                extension = Path.GetExtension(p);
                return File.ReadAllBytes(p);
            }
            var v = root.bufferViews[img.bufferView];
            var bytes = new byte[v.byteLength];
            Buffer.BlockCopy(bin, v.byteOffset, bytes, 0, v.byteLength);
            extension = img.mimeType == "image/jpeg" ? ".jpg" : ".png";
            return bytes;
        }
    }
}
#endif
