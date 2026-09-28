using System.Collections;
using UnityEngine;

namespace RealisticClassroom
{
    /// <summary>A seated student: follows the tutor with head and eyes, glances around, raises a hand and answers when called on.</summary>
    [RequireComponent(typeof(ActorRig))]
    public class StudentBehaviour : MonoBehaviour
    {
        public Transform deskPoint;
        public Transform[] neighbours;
        public Transform tutorFocus;
        public Transform boardFocus;

        ActorRig rig;
        Transform handTarget, elbowHint;
        bool busy;
        public bool HandRaised { get; private set; }
        public string StudentName => rig != null ? rig.displayName : name;
        public ActorRig Rig => rig;

        void Awake() { rig = GetComponent<ActorRig>(); }

        void Start()
        {
            handTarget = new GameObject("HandTarget").transform;
            elbowHint = new GameObject("ElbowHint").transform;
            handTarget.SetParent(transform, false);
            elbowHint.SetParent(transform, false);

            rig.animator.speed = Random.Range(0.93f, 1.07f);
            rig.Play("Sitting_Idle_Loop", 0f, Random.value);
            rig.ik.lookTarget = tutorFocus;
            StartCoroutine(AttentionLoop());
        }

        void LateUpdate()
        {
            if (handTarget == null) return;
            var head = rig.LookPoint.position;
            float k = rig.height / 1.6f;
            var fwd = transform.forward; var right = transform.right;
            handTarget.position = head + right * (0.20f * k) + Vector3.up * (0.36f * k) + fwd * (0.08f * k);
            elbowHint.position = head + right * (0.50f * k) - Vector3.up * (0.05f * k) - fwd * (0.05f * k);
        }

        IEnumerator AttentionLoop()
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 3f));
            while (true)
            {
                if (!busy) rig.ik.lookTarget = PickGaze();
                yield return new WaitForSeconds(Random.Range(2.5f, 6.5f));
            }
        }

        Transform PickGaze()
        {
            float r = Random.value;
            if (r < 0.62f) return tutorFocus;
            if (r < 0.78f) return boardFocus;
            if (r < 0.90f && neighbours != null && neighbours.Length > 0) return neighbours[Random.Range(0, neighbours.Length)];
            return deskPoint;
        }

        public void RaiseHand(bool raise)
        {
            HandRaised = raise;
            rig.ik.rightHandTarget = raise ? handTarget : null;
            rig.ik.rightElbowHint = raise ? elbowHint : null;
            if (raise) rig.ik.lookTarget = tutorFocus;
        }

        public void ResetState()
        {
            busy = false;
            RaiseHand(false);
            rig.ik.lookTarget = tutorFocus;
            rig.Play("Sitting_Idle_Loop", 0.3f, Random.value);
        }

        public IEnumerator Answer(float seconds)
        {
            busy = true;
            RaiseHand(false);
            rig.ik.lookTarget = tutorFocus;
            rig.Play("Sitting_Talking_Loop", 0.3f);
            yield return new WaitForSeconds(seconds);
            rig.Play("Sitting_Idle_Loop", 0.4f, Random.value);
            busy = false;
        }
    }
}
