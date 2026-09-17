using UnityEngine;

/// <summary>
/// Tap / click interactions: board cycles topics, tutor explains, students popup.
/// Works with mouse (Editor) and touch (phone). Also keyboard: N = next topic.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    ClassroomBuilder classroom;
    ClassroomUI ui;
    StudentController selected;

    void Start()
    {
        classroom = FindFirstObjectByType<ClassroomBuilder>();
        ui = FindFirstObjectByType<ClassroomUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && classroom != null && classroom.Board != null)
            classroom.Board.NextTopic();

        bool tapped = Input.GetMouseButtonDown(0) ||
                      (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!tapped) return;

        // Ignore taps on UI buttons
        try
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null)
            {
                if (Input.touchCount > 0) { if (es.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return; }
                else if (es.IsPointerOverGameObject()) return;
            }
        }
        catch { }

        Vector2 pos = Input.touchCount > 0 ? (Vector2)Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        Ray ray = Camera.main ? Camera.main.ScreenPointToRay(pos) : new Ray();
        if (Camera.main == null) return;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Board?
            var board = hit.collider.GetComponentInParent<BoardController>();
            if (board != null) { board.NextTopic(); Pulse(hit.collider.transform); ui?.Toast("Board: " + board.CurrentTopic().title); return; }

            // Tutor?
            var tutor = hit.collider.GetComponentInParent<TutorController>();
            if (tutor != null) { tutor.OnTapped(); ui?.Toast(tutor.tutorName + " is explaining…"); return; }

            // Student?
            var student = hit.collider.GetComponentInParent<StudentController>();
            if (student != null) { SelectStudent(student); return; }
        }
    }

    void SelectStudent(StudentController s)
    {
        if (selected) selected.SetHighlight(false);
        selected = s;
        s.SetHighlight(true);
        ui?.ShowInfo(s.studentName, s.GetInfo());
        // little bounce on the student
        StopAllCoroutines();
        StartCoroutine(CoBounce(s.transform));
    }

    System.Collections.IEnumerator CoBounce(Transform t)
    {
        Vector3 basePos = t.localPosition;
        for (float k = 0; k < 1f; k += Time.deltaTime * 3f)
        {
            t.localPosition = basePos + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.15f;
            yield return null;
        }
        t.localPosition = basePos;
    }

    void Pulse(Transform t)
    {
        StopAllCoroutines();
        StartCoroutine(CoPulse(t));
    }

    System.Collections.IEnumerator CoPulse(Transform t)
    {
        Vector3 s0 = t.localScale;
        float k = 0;
        while (k < 1f) { k += Time.deltaTime * 4f; t.localScale = s0 * (1 + Mathf.Sin(k * Mathf.PI) * 0.03f); yield return null; }
        t.localScale = s0;
    }
}
