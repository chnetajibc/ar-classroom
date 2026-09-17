using UnityEngine;

/// <summary>
/// Places the classroom in AR or Desktop preview.
/// - REAL AR (phone): if AR Foundation plane tracking is present, tap a
///   detected plane to place the classroom. Uses reflection so the project
///   still compiles/runs in the Editor with zero AR packages installed.
/// - EDITOR / DESKTOP (click Play on Mac): no planes exist, so the room is
///   staged at the origin for the fixed scene camera. Everything works
///   with mouse: click board/tutor/students, drag to orbit, wheel to zoom.
/// </summary>
public class ARClassroomPlacer : MonoBehaviour
{
    public GameObject classroomRoot;
    public float spawnDistance = 2.2f;
    public float spawnScale = 1f;

    bool placed;
    float startTime;
    Component planeManager;      // ARPlaneManager via reflection
    object raycastManager;       // ARRaycastManager via reflection
    System.Collections.IList raycastHits; // List<ARRaycastHit> via reflection

    void Start()
    {
        startTime = Time.time;
        TryBindARFoundation();
        if (classroomRoot) classroomRoot.SetActive(false);
    }

    void TryBindARFoundation()
    {
        try
        {
            // Find ARPlaneManager / ARRaycastManager without compile-time refs
#pragma warning disable CS0618 // classic API: compiles on every Unity version, warning suppressed
            foreach (var obj in FindObjectsOfType<MonoBehaviour>())
#pragma warning restore CS0618
            {
                string tn = obj.GetType().FullName ?? "";
                if (tn.Contains("ARPlaneManager")) planeManager = obj;
                if (tn.Contains("ARRaycastManager")) raycastManager = obj;
            }
            if (raycastManager != null)
            {
                var hitType = System.Type.GetType(
                    "UnityEngine.XR.ARFoundation.ARRaycastHit, Unity.XR.ARFoundation");
                if (hitType != null)
                {
                    var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(hitType);
                    raycastHits = (System.Collections.IList)System.Activator.CreateInstance(listType);
                }
            }
        }
        catch (System.Exception e) { Debug.Log("[AR] bind skipped: " + e.Message); }
    }

    void Update()
    {
        if (placed) { HandleDesktopOrbit(); return; }

        // 1) Try AR tap-to-place
        if (TryARPlace()) return;

        // 2) Fallback: auto-place for Editor/Desktop after short delay
        if (Time.time - startTime > 1.0f)
            PlaceDesktop();
    }

    bool TryARPlace()
    {
        if (raycastManager == null) return false;
        // Touch tap?
        Vector2? tap = null;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            tap = Input.GetTouch(0).position;
        else if (Input.GetMouseButtonDown(0))
            tap = Input.mousePosition;
        if (tap == null) return false;

        try
        {
            // 100% reflection: no compile-time reference to AR assemblies, so this
            // file compiles even when AR Foundation is NOT installed yet.
            var rm = raycastManager as Component;
            System.Type trackableType = System.Type.GetType(
                "UnityEngine.XR.ARSubsystems.TrackableType, Unity.XR.ARSubsystems");
            if (trackableType == null) return false;
            System.Reflection.MethodInfo method = null;
            foreach (var m in rm.GetType().GetMethods())
            {
                if (m.Name != "Raycast") continue;
                var ps = m.GetParameters();
                if (ps.Length == 3 && ps[0].ParameterType == typeof(Vector2) &&
                    typeof(System.Collections.IList).IsAssignableFrom(ps[1].ParameterType) &&
                    ps[2].ParameterType == trackableType) { method = m; break; }
            }
            if (method == null) return false;
            object trackable = System.Enum.Parse(trackableType, "PlaneWithinPolygon");
            bool hit = (bool)method.Invoke(rm, new object[] { tap.Value, raycastHits, trackable });
            if (hit && raycastHits.Count > 0)
            {
                var pose = (Pose)raycastHits[0].GetType().GetProperty("pose").GetValue(raycastHits[0]);
                PlaceAt(pose.position, Quaternion.Euler(0, Camera.main.transform.eulerAngles.y - 180, 0));
                Debug.Log("[AR] Classroom placed on detected plane.");
                return true;
            }
        }
        catch (System.Exception e) { Debug.Log("[AR] raycast failed: " + e.Message); }
        return false;
    }

    void PlaceDesktop()
    {
        // Fixed staging: the classroom always sits at the origin and the scene
        // camera is pre-framed (front-left seat view: tutor + students in shot).
        // Deterministic beats camera-relative for a one-click demo.
        PlaceAt(Vector3.zero, Quaternion.identity);
        Debug.Log("[Preview] Classroom staged at origin for the fixed camera. Click board / tutor / students!");
    }

    void PlaceAt(Vector3 pos, Quaternion rot)
    {
        classroomRoot.SetActive(true);
        classroomRoot.transform.position = pos;
        classroomRoot.transform.rotation = rot;
        classroomRoot.transform.localScale = Vector3.one * spawnScale;
        placed = true;
        var tutor = classroomRoot.GetComponentInChildren<TutorController>();
        if (tutor) tutor.Speak("Welcome! Tap me, the board, or any student.", 4f);
    }

    public void ResetPlacement()
    {
        placed = false;
        startTime = Time.time - 2f; // re-place next frame
        if (classroomRoot) classroomRoot.SetActive(false);
    }

    // Simple desktop orbit: right-drag rotates, wheel zooms, R resets
    void HandleDesktopOrbit()
    {
        if (classroomRoot == null) return;
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            classroomRoot.transform.RotateAround(classroomRoot.transform.position, Vector3.up, dx * 6f);
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float s = Mathf.Clamp(classroomRoot.transform.localScale.x * (1 + scroll), 0.3f, 3f);
            classroomRoot.transform.localScale = Vector3.one * s;
        }
        if (Input.GetKeyDown(KeyCode.R)) ResetPlacement();
    }
}
