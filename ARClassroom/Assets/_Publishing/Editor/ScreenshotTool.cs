#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace RealisticClassroom.Publishing
{
    /// <summary>Headless verification helpers: offscreen renders to PNG and edit-mode pose sampling.</summary>
    public static class ScreenshotTool
    {
        public static string Capture(string outPath, Vector3 pos, Vector3 lookAt, float fov = 60f, int width = 1600, int height = 900, Color? background = null)
        {
            var go = new GameObject("__ShotCam");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.transform.position = pos;
                cam.transform.rotation = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 200f;
                cam.allowHDR = true;
                cam.clearFlags = background.HasValue ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
                if (background.HasValue) cam.backgroundColor = background.Value;
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.renderShadows = true;

                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return outPath;
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>Evaluate an animation clip on a humanoid in edit mode (leaves the pose applied to the transforms).</summary>
        public static void Pose(GameObject character, AnimationClip clip, float time)
        {
            var animator = character.GetComponent<Animator>();
            var graph = PlayableGraph.Create("pose");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "out", animator);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            output.SetSourcePlayable(playable);
            playable.SetTime(time);
            graph.Evaluate(0f);
            graph.Destroy();
        }
    }
}
#endif
