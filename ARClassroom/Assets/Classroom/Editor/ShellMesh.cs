#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARClassroom.EditorTools
{
    /// <summary>
    /// Re-projects the UVs of selected submeshes of a placed model in world space (box mapping) so a tiling PBR texture runs
    /// seamlessly across neighbouring wall/floor/ceiling modules. Works on a saved copy of the mesh; the source model is untouched.
    /// </summary>
    public static class ShellMesh
    {
        const string MeshDir = "Assets/Classroom/Meshes";

        public static void ApplyWorldUV(GameObject instance, string assetName, string replaceMaterialName, Material replacement, float tileMeters)
        {
            Directory.CreateDirectory(MeshDir);
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<MeshRenderer>();
                var mats = r.sharedMaterials;
                var targets = new List<int>();
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && (replaceMaterialName == null || mats[i].name == replaceMaterialName)) targets.Add(i);
                if (targets.Count == 0) continue;

                var src = mf.sharedMesh;
                var mesh = Object.Instantiate(src);
                mesh.name = assetName;
                var verts = new List<Vector3>(); mesh.GetVertices(verts);
                mesh.colors = null; mesh.tangents = null; mesh.SetUVs(1, (List<Vector2>)null);
                var normals = new List<Vector3>(); mesh.GetNormals(normals);
                var uvs = new List<Vector2>(); mesh.GetUVs(0, uvs);
                if (uvs.Count < verts.Count) { uvs.Clear(); for (int i = 0; i < verts.Count; i++) uvs.Add(Vector2.zero); }

                var l2w = mf.transform.localToWorldMatrix;
                foreach (int sm in targets)
                {
                    var tris = new List<int>(); mesh.GetTriangles(tris, sm);
                    var remap = new Dictionary<int, int>();
                    for (int t = 0; t < tris.Count; t++)
                    {
                        int old = tris[t];
                        if (!remap.TryGetValue(old, out int nv))
                        {
                            nv = verts.Count;
                            remap[old] = nv;
                            verts.Add(verts[old]);
                            normals.Add(normals[old]);
                            var wp = l2w.MultiplyPoint3x4(verts[old]);
                            var wn = l2w.MultiplyVector(normals[old]).normalized;
                            uvs.Add(BoxUV(wp, wn) / tileMeters);
                        }
                        tris[t] = nv;
                    }
                    mesh.SetVertices(verts);
                    mesh.SetNormals(normals);
                    mesh.SetUVs(0, uvs);
                    mesh.SetTriangles(tris, sm);
                }
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();

                var path = MeshDir + "/" + assetName + (targets.Count > 0 ? "" : "") + "_" + mf.name + ".asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                mf.sharedMesh = mesh;
                foreach (int sm in targets) mats[sm] = replacement;
                r.sharedMaterials = mats;
            }
        }

        static Vector2 BoxUV(Vector3 p, Vector3 n)
        {
            var a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (a.y >= a.x && a.y >= a.z) return new Vector2(p.x, p.z);
            if (a.x >= a.z) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }
    }
}
#endif
