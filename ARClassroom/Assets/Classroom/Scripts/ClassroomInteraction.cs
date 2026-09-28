using UnityEngine;
using UnityEngine.EventSystems;

namespace ARClassroom
{
    /// <summary>Click a student to have the teacher call on them, the teacher to get a gesture, the board for the next lesson.</summary>
    public class ClassroomInteraction : MonoBehaviour
    {
        public Camera cam;
        public ClassroomCameraRig rig;
        public LessonDirector director;
        public ClassroomUI ui;

        void Update()
        {
            if (!Input.GetMouseButtonUp(0) || cam == null) return;
            if (rig != null && rig.DraggedThisClick) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 60f)) return;

            var student = hit.collider.GetComponentInParent<StudentBehaviour>();
            if (student != null) { director?.CallOn(student); return; }

            var tutor = hit.collider.GetComponentInParent<TutorBehaviour>();
            if (tutor != null) { ui?.Say(tutor.TutorName, "Good to see everyone engaged. Keep the questions coming!", 3f); StartCoroutine(tutor.Gesture()); return; }

            if (hit.collider.GetComponentInParent<BoardText>() != null || hit.collider.name.Contains("Blackboard")) director?.NextLesson();
        }
    }
}
