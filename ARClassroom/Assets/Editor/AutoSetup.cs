#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup for the AR Classroom.
/// Menu: AR Classroom / Setup Scene + Enable AR + Open How-To.
/// Also auto-runs when the scene opens and something is missing (self-heal).
/// </summary>
public static class AutoSetup
{
    const string ScenePath = "Assets/Scenes/ARClassroom.unity";

    [MenuItem("AR Classroom/1. Setup Scene (click me first)")]
    public static void SetupScene()
    {
        EnsureSceneObjects();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[AR Classroom] Scene ready. Now press ▶ Play.");
    }

    [MenuItem("AR Classroom/2. Add Real-AR Objects (needs AR Foundation installed)")]
    public static void AddARObjects()
    {
        // Creates AR Session + XR Origin with plane tracking, without compile-time refs.
        // If AR Foundation is NOT installed yet, shows a friendly message instead.
        var arSessionType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSession, Unity.XR.ARFoundation");
        if (arSessionType == null)
        {
            EditorUtility.DisplayDialog("AR Foundation not installed yet",
                "Real phone-AR needs the AR Foundation packages.\n\n" +
                "They are already listed in Packages/manifest.json, so Unity installs them automatically on first open.\n\n" +
                "Wait for the install to finish (see progress bar bottom-right), then click this menu item again.\n\n" +
                "TIP: Pressing Play right now already works in Desktop Preview mode — no phone needed.",
                "Got it");
            return;
        }

        EnsureARSession();
        EnsureXROrigin();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[AR Classroom] Real-AR objects added. Build to Android/iOS for true AR.");
    }

    [MenuItem("AR Classroom/3. Open How-To-Run Doc")]
    public static void OpenDoc()
    {
        var path = System.IO.Path.GetFullPath("HOW_TO_RUN_AR.md");
        if (System.IO.File.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        else
            EditorUtility.DisplayDialog("Doc", "HOW_TO_RUN_AR.md sits next to the ARClassroom folder.", "OK");
    }

    // NOTE: no auto-run on editor startup on purpose. An earlier version rebuilt
    // the scene via [InitializeOnLoadMethod], which created physics colliders
    // before Unity's PhysicsManager existed -> "GetManagerFromContext ... NULL".
    // Setup now runs ONLY when you click the menu item below, when the editor
    // is fully initialized.

    static void EnsureSceneObjects()
    {
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            cam.transform.position = new Vector3(-3.2f, 2.6f, -1.8f);
            cam.transform.rotation = Quaternion.Euler(23.6f, 56.7f, 0f);
            go.AddComponent<CameraCorners>();
        }
        else if (cam.GetComponent<CameraCorners>() == null)
        {
            cam.gameObject.AddComponent<CameraCorners>();
        }

        if (Object.FindFirstObjectByType<ClassroomBuilder>() == null)
        {
            var root = new GameObject("ClassroomRoot");
            var builder = root.AddComponent<ClassroomBuilder>();
            builder.rows = 3; builder.cols = 3;
            builder.emptySeats = new int[] { 2, 5, 7 };
        }
        var classroomRoot = Object.FindFirstObjectByType<ClassroomBuilder>().gameObject;

        if (Object.FindFirstObjectByType<ARClassroomPlacer>() == null)
        {
            var m = new GameObject("ARPlacer").AddComponent<ARClassroomPlacer>();
            m.classroomRoot = classroomRoot;
        }
        else
        {
            var p = Object.FindFirstObjectByType<ARClassroomPlacer>();
            if (p.classroomRoot == null) p.classroomRoot = classroomRoot;
        }

        if (Object.FindFirstObjectByType<InteractionManager>() == null)
            new GameObject("InteractionManager").AddComponent<InteractionManager>();
        if (Object.FindFirstObjectByType<ClassroomUI>() == null)
            new GameObject("ClassroomUI").AddComponent<ClassroomUI>();

        if (Object.FindFirstObjectByType<Light>() == null)
        {
            var sun = new GameObject("Directional Light");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }

    static void EnsureARSession()
    {
        var t = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSession, Unity.XR.ARFoundation");
        if (GameObject.Find("AR Session") != null) return;
        var go = new GameObject("AR Session");
        go.AddComponent(t);
    }

    static void EnsureXROrigin()
    {
        if (GameObject.Find("XR Origin") != null) return;
        var origin = new GameObject("XR Origin");
        var originType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSessionOrigin, Unity.XR.ARFoundation")
            ?? System.Type.GetType("UnityEngine.XR.ARFoundation.XROrigin, Unity.XR.ARFoundation");
        if (originType != null) origin.AddComponent(originType);

        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.transform.SetParent(origin.transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            var camType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraManager, Unity.XR.ARFoundation");
            if (camType != null && cam.GetComponent(camType) == null) cam.gameObject.AddComponent(camType);
            var bgType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraBackground, Unity.XR.ARFoundation");
            if (bgType != null && cam.GetComponent(bgType) == null) cam.gameObject.AddComponent(bgType);
        }

        // Plane manager + raycast manager + point cloud (all optional, reflection-safe)
        AddIfExists(origin, "UnityEngine.XR.ARFoundation.ARPlaneManager, Unity.XR.ARFoundation");
        AddIfExists(origin, "UnityEngine.XR.ARFoundation.ARRaycastManager, Unity.XR.ARFoundation");
        AddIfExists(origin, "UnityEngine.XR.ARFoundation.ARPointCloudManager, Unity.XR.ARFoundation");

        // Visualize planes with a default plane prefab is skipped (needs asset) — plane
        // detection still works; placement uses tap. See HOW_TO_RUN_AR.md.
    }

    static void AddIfExists(GameObject go, string typeName)
    {
        var t = System.Type.GetType(typeName);
        if (t != null && go.GetComponent(t) == null) go.AddComponent(t);
    }
}
#endif
