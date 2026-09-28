#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    /// <summary>Imports the Quaternius Universal Animation Library as Humanoid clips and builds the two Animator Controllers used by the demo.</summary>
    public static class AnimationSetup
    {
        const string Root = ModelConverter.Root;
        const string Dir = Root + "/Animations/UniversalAnimationLibrary";
        public const string ControllerDir = Root + "/Animations/Controllers";
        static readonly string[] Files = { "UAL1_Standard.fbx", "UAL2_Standard.fbx" };

        public static string Run()
        {
            Directory.CreateDirectory(Dir);
            foreach (var f in Files)
            {
                var dst = Dir + "/" + f;
                if (!File.Exists(dst)) File.Copy("Assets/_Publishing/Sources/Animations/" + f, dst);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var f in Files) Configure(Dir + "/" + f);

            Directory.CreateDirectory(ControllerDir);
            Build("Student", new[] { "Sitting_Idle_Loop", "Sitting_Talking_Loop" }, "Sitting_Idle_Loop");
            Build("Tutor", new[] { "Idle_Loop", "Idle_Talking_Loop", "Walk_Formal_Loop", "Interact", "Idle_FoldArms_Loop", "Idle_No_Loop", "Yes" }, "Idle_Talking_Loop");
            AssetDatabase.SaveAssets();
            return "animations OK";
        }

        static void Configure(string path)
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
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        public static AnimationClip Find(string clipName)
        {
            foreach (var f in Files)
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Dir + "/" + f))
                    if (o is AnimationClip c && c.name == clipName) return c;
            return null;
        }

        static void Build(string name, string[] clipNames, string defaultState)
        {
            var path = ControllerDir + "/" + name + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var cn in clipNames)
            {
                var clip = Find(cn);
                if (clip == null) { Debug.LogWarning("[AnimationSetup] missing clip " + cn); continue; }
                var st = sm.AddState(cn);
                st.motion = clip;
                st.iKOnFeet = true;
                if (cn == defaultState) sm.defaultState = st;
            }
            var layers = ctrl.layers;
            layers[0].iKPass = true;
            ctrl.layers = layers;
            EditorUtility.SetDirty(ctrl);
        }
    }
}
#endif
