#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>
    /// Stage 1 of the character pipeline: swings the arms of each downloaded glTF character into a T-pose and stores it as a wrapper prefab
    /// (Model child rotated 180 degrees). ModelConverter then exports that T-posed model to FBX with a Humanoid avatar.
    /// </summary>
    public static class CharacterPrep
    {
        const string PeopleDir = "Assets/_Publishing/Sources/Models/People";
        const string PrefabDir = "Assets/_Publishing/Sources/Prefabs/Characters";

        public static string BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            var report = new System.Text.StringBuilder();
            foreach (var file in Directory.GetFiles(PeopleDir, "*.glb"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\', '/')));
                var map = HumanoidSetup.FindBoneMap(instance.transform);
                if (map == null) { report.AppendLine(name + ": unknown rig"); Object.DestroyImmediate(instance); continue; }
                MakeTPose(instance.transform, map);
                var wrapper = new GameObject(name);
                instance.transform.SetParent(wrapper.transform, false);
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                instance.name = "Model";
                PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabDir + "/" + name + ".prefab");
                Object.DestroyImmediate(wrapper);
                report.AppendLine(name + ": T-pose wrapper saved");
            }
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        /// <summary>The source rigs rest with arms hanging down; Humanoid needs a T-pose, so swing both arms out horizontally.</summary>
        static void MakeTPose(Transform root, Dictionary<string, Transform> map)
        {
            foreach (var side in new[] { "Left", "Right" })
            {
                if (!map.TryGetValue(side + "UpperArm", out var upper) || !map.TryGetValue(side + "LowerArm", out var lower) ||
                    !map.TryGetValue(side + "Hand", out var hand)) continue;
                float sign = Mathf.Sign(root.InverseTransformPoint(upper.position).x);
                if (sign == 0f) sign = side == "Left" ? 1f : -1f;
                Vector3 target = root.TransformDirection(new Vector3(sign, 0f, 0f));
                upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, target) * upper.rotation;
                lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target) * lower.rotation;
            }
        }
    }
}
#endif
