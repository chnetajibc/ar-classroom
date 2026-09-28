using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class AutoScreenshot
{
    static AutoScreenshot()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (EditorApplication.isCompiling) return;

        string path = Path.Combine(Application.dataPath, "../auto_screenshot2.png");
        if (!File.Exists(path))
        {
            try
            {
                ScreenshotVerifier.TakeScreenshot();
                File.Move(Path.Combine(Application.dataPath, "../screenshot.png"), path);
                Debug.Log("Screenshot moved to " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }
        EditorApplication.update -= Update;
    }
}
