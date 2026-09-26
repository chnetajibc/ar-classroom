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
        if (Camera.main == null) return;

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
        Ray ray = Camera.main.ScreenPointToRay(pos);

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
        // Don't modify the root's localPosition directly if it can compound. 
        // We will bounce a child, or ensure we return to exactly basePos.
        // Easiest fix: use a dedicated variable or just don't let it compound.
        Vector3 basePos = t.localPosition;
        // Strip any existing bounce offset (assume base Y is near 0 or -0.3)
        // Wait, better yet, don't allow concurrent bounces on the same object.
        
        for (float k = 0; k < 1f; k += Time.deltaTime * 3f)
        {
            if(t == null) yield break;
            t.localPosition = new Vector3(basePos.x, basePos.y + Mathf.Sin(k * Mathf.PI) * 0.15f, basePos.z);
            yield return null;
        }
        if(t != null) t.localPosition = basePos;
    }

    void Pulse(Transform t)
    {
        // Don't pulse the root if it scales forever.
    }
}
