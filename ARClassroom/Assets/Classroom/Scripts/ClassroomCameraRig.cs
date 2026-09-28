using UnityEngine;

namespace ARClassroom
{
    /// <summary>Preset viewpoints (V / 1-6 / UI button) plus mouse orbit and zoom around the current focus point.</summary>
    [RequireComponent(typeof(Camera))]
    public class ClassroomCameraRig : MonoBehaviour
    {
        [System.Serializable]
        public struct View
        {
            public string name;
            public Vector3 position;
            public Vector3 target;
            public float fov;
        }

        public View[] views;
        public int startView;
        public float smooth = 4f;

        int index;
        Vector3 goalPos, goalTarget, curTarget;
        float goalFov;
        Camera cam;
        Vector3 lastMouse;
        float dragDistance;

        public string CurrentName => views != null && views.Length > 0 ? views[index].name : "";
        public bool DraggedThisClick => dragDistance > 6f;

        void Awake() { cam = GetComponent<Camera>(); }

        void Start() { GoTo(startView, true); }

        public void Next() { GoTo(index + 1, false); }

        public void GoTo(int i, bool instant)
        {
            if (views == null || views.Length == 0) return;
            index = ((i % views.Length) + views.Length) % views.Length;
            goalPos = views[index].position;
            goalTarget = views[index].target;
            goalFov = views[index].fov > 1f ? views[index].fov : 55f;
            if (instant)
            {
                transform.position = goalPos;
                curTarget = goalTarget;
                cam.fieldOfView = goalFov;
                transform.rotation = Quaternion.LookRotation(curTarget - transform.position, Vector3.up);
            }
        }

        void Update()
        {
            HandleInput();
            float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, goalPos, k);
            curTarget = Vector3.Lerp(curTarget, goalTarget, k);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, goalFov, k);
            var dir = curTarget - transform.position;
            if (dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Tab)) Next();
            for (int i = 0; i < 9 && views != null && i < views.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) GoTo(i, false);

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) { lastMouse = Input.mousePosition; dragDistance = 0f; }
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                var delta = Input.mousePosition - lastMouse;
                lastMouse = Input.mousePosition;
                dragDistance += delta.magnitude;
                if (dragDistance > 6f)
                {
                    var offset = goalPos - goalTarget;
                    var rot = Quaternion.AngleAxis(delta.x * 0.25f, Vector3.up) * Quaternion.AngleAxis(-delta.y * 0.25f, transform.right);
                    var newOffset = rot * offset;
                    if (Mathf.Abs(Vector3.Dot(newOffset.normalized, Vector3.up)) < 0.95f) offset = newOffset;
                    goalPos = goalTarget + offset;
                    goalPos.y = Mathf.Clamp(goalPos.y, 0.4f, 3.0f);
                }
            }
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var toTarget = goalTarget - goalPos;
                float dist = toTarget.magnitude;
                float move = Mathf.Clamp(scroll * 0.5f, -dist + 0.6f, 4f);
                goalPos += toTarget.normalized * move;
            }
        }
    }
}
