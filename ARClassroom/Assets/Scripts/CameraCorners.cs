using UnityEngine;

/// <summary>
/// CCTV-style security views: the Main Camera jumps between the 4 elevated
/// room corners, always looking at the room center. Corner 0 (front-left)
/// is the default opening shot. Nobody's perspective — pure observer.
/// </summary>
public class CameraCorners : MonoBehaviour
{
    public static CameraCorners Instance;

    [System.Serializable]
    public struct Corner
    {
        public Vector3 pos;
        public Vector3 lookAt;
        public string name;
    }

    public Corner[] corners = new Corner[]
    {
        new Corner { pos = new Vector3(-3.2f, 2.6f, -1.8f), lookAt = new Vector3(0f, 1f, 0.3f), name = "Front-Left" },
        new Corner { pos = new Vector3(3.2f, 2.6f, -1.8f), lookAt = new Vector3(0f, 1f, 0.3f), name = "Front-Right" },
        new Corner { pos = new Vector3(-3.2f, 2.6f, 2.8f), lookAt = new Vector3(0f, 1f, 0.3f), name = "Back-Left" },
        new Corner { pos = new Vector3(3.2f, 2.6f, 2.8f), lookAt = new Vector3(0f, 1f, 0.3f), name = "Back-Right" },
    };

    int index;

    void Awake() { Instance = this; }

    public int Index => index;
    public string CurrentName => corners.Length > 0 ? corners[index].name : "?";

    public void GoTo(int i)
    {
        if (corners == null || corners.Length == 0) return;
        index = ((i % corners.Length) + corners.Length) % corners.Length;
        transform.position = corners[index].pos;
        transform.LookAt(corners[index].lookAt);
    }

    public void Next() { GoTo(index + 1); }
}
