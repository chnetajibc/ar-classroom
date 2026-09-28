#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>Builds a Humanoid description for the Quaternius rigs (leg chains end at an ankle leaf, so that leaf is mapped as the foot).</summary>
    public static class HumanoidSetup
    {
        public static HumanDescription? Describe(Transform root)
        {
            var map = FindBoneMap(root);
            if (map == null) return null;
            var desc = new HumanDescription
            {
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
            var human = new List<HumanBone>();
            foreach (var kv in map)
                human.Add(new HumanBone { humanName = kv.Key, boneName = kv.Value.name, limit = new HumanLimit { useDefaultValues = true } });
            desc.human = human.ToArray();
            var skeleton = new List<SkeletonBone>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            desc.skeleton = skeleton.ToArray();
            return desc;
        }

        public static Dictionary<string, Transform> FindBoneMap(Transform root)
        {
            Transform Find(string n)
            {
                // the FBX importer turns '.' in node names into '_'
                var wanted = n.Replace('.', '_');
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.Replace('.', '_') == wanted) return t;
                return null;
            }
            var m = new Dictionary<string, Transform>();
            bool Add(string human, string bone)
            {
                var t = Find(bone);
                if (t == null) return false;
                m[human] = t;
                return true;
            }
            if (Find("Body") != null && Find("Torso") != null)
            {
                Add("Hips", "Body"); Add("Spine", "Abdomen");
                if (Find("Chest") != null) { Add("Chest", "Torso"); Add("UpperChest", "Chest"); } else Add("Chest", "Torso");
                Add("Neck", "Neck"); Add("Head", "Head");
                foreach (var s in new[] { "L", "R" })
                {
                    var side = s == "L" ? "Left" : "Right";
                    Add(side + "Shoulder", "Shoulder." + s); Add(side + "UpperArm", "UpperArm." + s); Add(side + "LowerArm", "LowerArm." + s);
                    if (!Add(side + "Hand", "Wrist." + s)) Add(side + "Hand", "Palm." + s);
                    Add(side + "UpperLeg", "UpperLeg." + s); Add(side + "LowerLeg", "LowerLeg." + s); Add(side + "Foot", "LowerLeg." + s + "_end");
                }
                return m;
            }
            return null;
        }
    }
}
#endif
