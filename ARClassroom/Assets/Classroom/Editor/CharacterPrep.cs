#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ARClassroom.EditorTools
{
    /// <summary>
    /// Turns the downloaded glTF characters into Humanoid-rigged prefabs (so the Quaternius
    /// Universal Animation Library clips can be retargeted onto them) and prepares the animation clips.
    /// </summary>
    public static class CharacterPrep
    {
        public const string PeopleDir = "Assets/Classroom/Models/People";
        public const string AvatarDir = "Assets/Classroom/Animations/Avatars";
        public const string PrefabDir = "Assets/Classroom/Prefabs/Characters";
        public const string ControllerDir = "Assets/Classroom/Animations/Controllers";

        static readonly string[] UalFiles =
        {
            "Assets/Classroom/Animations/UAL1_Standard.fbx",
            "Assets/Classroom/Animations/UAL2_Standard.fbx",
        };

        // ------------------------------------------------------------------ clips
        public static void ConfigureAnimationImport()
        {
            foreach (var path in UalFiles)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.animationType = ModelImporterAnimationType.Human;
                imp.importAnimation = true;
                imp.importBlendShapes = false;
                var clips = imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.name = c.name.Replace("Armature|", "");
                    c.loopTime = c.name.EndsWith("_Loop");
                    c.loopPose = c.loopTime;
                    // keep the pose exactly as authored (sitting keeps its lowered pelvis) and never drift the root
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
            }
        }

        public static AnimationClip FindClip(string clipName)
        {
            foreach (var path in UalFiles)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is AnimationClip c && c.name == clipName) return c;
            return null;
        }

        // ------------------------------------------------------------------ avatars + prefabs
        public static string BuildAll()
        {
            Directory.CreateDirectory(AvatarDir);
            Directory.CreateDirectory(PrefabDir);
            var report = new System.Text.StringBuilder();
            foreach (var file in Directory.GetFiles(PeopleDir, "*.glb"))
            {
                var path = file.Replace('\\', '/');
                report.AppendLine(BuildCharacter(path));
            }
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        static string BuildCharacter(string glbPath)
        {
            var name = Path.GetFileNameWithoutExtension(glbPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                var map = FindBoneMap(instance.transform);
                if (map == null) return name + ": no known rig layout";
                MakeTPose(instance.transform, map);

                var desc = new HumanDescription
                {
                    upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                    armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
                };
                var human = new List<HumanBone>();
                foreach (var kv in map)
                {
                    human.Add(new HumanBone
                    {
                        humanName = kv.Key,
                        boneName = kv.Value.name,
                        limit = new HumanLimit { useDefaultValues = true },
                    });
                }
                desc.human = human.ToArray();

                var skeleton = new List<SkeletonBone>();
                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                {
                    skeleton.Add(new SkeletonBone
                    {
                        name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale,
                    });
                }
                desc.skeleton = skeleton.ToArray();

                var avatar = AvatarBuilder.BuildHumanAvatar(instance, desc);
                if (avatar == null || !avatar.isValid || !avatar.isHuman)
                    return name + ": avatar INVALID";
                avatar.name = name + "Avatar";
                var avatarPath = AvatarDir + "/" + name + "Avatar.asset";
                AssetDatabase.DeleteAsset(avatarPath);
                AssetDatabase.CreateAsset(avatar, avatarPath);

                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // animated humanoids face -Z after retargeting; wrap so the prefab root faces +Z like the rest of the scene
                var wrapper = new GameObject(name);
                instance.transform.SetParent(wrapper.transform, false);
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                instance.name = "Model";
                var prefabPath = PrefabDir + "/" + name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                Object.DestroyImmediate(wrapper);
                instance = null;
                return name + ": OK (" + human.Count + " humanoid bones)";
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        /// <summary>The source rigs rest with arms hanging down; Humanoid needs a T-pose, so swing both arms out horizontally first.</summary>
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

        /// <summary>Recognises the three rig layouts we ship and returns HumanBodyBones-name -> Transform.</summary>
        static Dictionary<string, Transform> FindBoneMap(Transform root)
        {
            Transform Find(string n)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == n) return t;
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

            if (Find("Body") != null && Find("Torso") != null) // Quaternius "HumanArmature" / "CharacterArmature"
            {
                Add("Hips", "Body");
                Add("Spine", "Abdomen");
                if (Find("Chest") != null && Find("Torso") != null)
                {
                    Add("Chest", "Torso");
                    Add("UpperChest", "Chest");
                }
                else Add("Chest", "Torso");
                Add("Neck", "Neck");
                Add("Head", "Head");
                foreach (var s in new[] { "L", "R" })
                {
                    var side = s == "L" ? "Left" : "Right";
                    Add(side + "Shoulder", "Shoulder." + s);
                    Add(side + "UpperArm", "UpperArm." + s);
                    Add(side + "LowerArm", "LowerArm." + s);
                    if (!Add(side + "Hand", "Wrist." + s)) Add(side + "Hand", "Palm." + s);
                    Add(side + "UpperLeg", "UpperLeg." + s);
                    Add(side + "LowerLeg", "LowerLeg." + s);
                    Add(side + "Foot", "LowerLeg." + s + "_end");
                }
                return m;
            }
            if (Find("Hips") != null && Find("LeftUpLeg") != null) // Mixamo naming
            {
                Add("Hips", "Hips"); Add("Spine", "Spine"); Add("Chest", "Spine1"); Add("UpperChest", "Spine2");
                Add("Neck", "Neck"); Add("Head", "Head");
                foreach (var side in new[] { "Left", "Right" })
                {
                    Add(side + "Shoulder", side + "Shoulder");
                    Add(side + "UpperArm", side + "Arm");
                    Add(side + "LowerArm", side + "ForeArm");
                    Add(side + "Hand", side + "Hand");
                    Add(side + "UpperLeg", side + "UpLeg");
                    Add(side + "LowerLeg", side + "Leg");
                    Add(side + "Foot", side + "Foot");
                    Add(side + "Toes", side + "ToeBase");
                }
                return m;
            }
            return null;
        }

        // ------------------------------------------------------------------ controllers
        public static void BuildControllers()
        {
            Directory.CreateDirectory(ControllerDir);
            BuildController("Student", new[] { "Sitting_Idle_Loop", "Sitting_Talking_Loop" }, "Sitting_Idle_Loop");
            BuildController("Tutor",
                new[] { "Idle_Loop", "Idle_Talking_Loop", "Walk_Formal_Loop", "Interact", "Idle_FoldArms_Loop", "Idle_No_Loop", "Yes" },
                "Idle_Talking_Loop");
        }

        static void BuildController(string name, string[] clipNames, string defaultState)
        {
            var path = ControllerDir + "/" + name + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var cn in clipNames)
            {
                var clip = FindClip(cn);
                if (clip == null) { Debug.LogWarning("[CharacterPrep] missing clip " + cn); continue; }
                var st = sm.AddState(cn);
                st.motion = clip;
                st.iKOnFeet = true;
                if (cn == defaultState) sm.defaultState = st;
            }
            // the IK pass on layer 0 lets HumanoidLook aim head/eyes/torso
            var layers = ctrl.layers;
            layers[0].iKPass = true;
            ctrl.layers = layers;
            EditorUtility.SetDirty(ctrl);
        }
    }
}
#endif
