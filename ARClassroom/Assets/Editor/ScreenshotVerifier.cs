using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScreenshotVerifier
{
    [MenuItem("Tools/Take Verification Screenshot")]
    public static void TakeScreenshot()
    {
        // Find main camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("No Main Camera found!");
            return;
        }

        // Build the classroom procedurally to make sure it exists
        var builder = Object.FindFirstObjectByType<ClassroomBuilder>();
        if (builder != null)
        {
            builder.Build();
        }

        RenderTexture rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null; 
        Object.DestroyImmediate(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, "../screenshot.png");
        File.WriteAllBytes(path, bytes);
        Debug.Log("Screenshot saved to " + path);
    }
}
