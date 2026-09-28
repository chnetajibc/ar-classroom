using System.Collections;
using UnityEngine;

namespace RealisticClassroom
{
    /// <summary>The teacher: walks the front of the room, turns to face the class or the board, gestures, writes and reacts.</summary>
    [RequireComponent(typeof(ActorRig))]
    public class TutorBehaviour : MonoBehaviour
    {
        public float walkSpeed = 1.15f;
        public float turnSpeed = 200f;

        ActorRig rig;
        Transform writeTarget, writeElbow;
        public ActorRig Rig => rig;
        public string TutorName => rig != null ? rig.displayName : name;

        void Awake() { rig = GetComponent<ActorRig>(); }

        void Start()
        {
            writeTarget = new GameObject("WriteTarget").transform;
            writeElbow = new GameObject("WriteElbow").transform;
            writeTarget.SetParent(transform.parent, false);
            writeElbow.SetParent(transform.parent, false);
            rig.Play("Idle_Talking_Loop", 0f, Random.value);
        }

        public void Talk(bool talking) { rig.Play(talking ? "Idle_Talking_Loop" : "Idle_Loop", 0.3f); }

        public void LookAt(Transform t) { rig.ik.lookTarget = t; }

        public IEnumerator WalkTo(Vector3 position)
        {
            position.y = transform.position.y;
            rig.Play("Walk_Formal_Loop", 0.25f);
            rig.ik.lookTarget = null;
            while (true)
            {
                var to = position - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.05f) break;
                var want = Quaternion.LookRotation(to.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * 1.5f * Time.deltaTime);
                float align = Mathf.Clamp01(Quaternion.Angle(transform.rotation, want) < 40f ? 1f : 0f);
                transform.position += transform.forward * (Mathf.Min(walkSpeed * Time.deltaTime, dist) * align);
                yield return null;
            }
            rig.Play("Idle_Loop", 0.3f);
        }

        public IEnumerator FaceTowards(Vector3 worldPoint)
        {
            var to = worldPoint - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f) yield break;
            var want = Quaternion.LookRotation(to.normalized, Vector3.up);
            while (Quaternion.Angle(transform.rotation, want) > 1.5f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
                yield return null;
            }
        }

        public IEnumerator Gesture()
        {
            rig.Play("Interact", 0.2f);
            yield return new WaitForSeconds(Mathf.Min(rig.ClipLength("Interact"), 2.4f));
            rig.Play("Idle_Talking_Loop", 0.3f);
        }

        public IEnumerator Nod()
        {
            rig.Play("Yes", 0.2f);
            yield return new WaitForSeconds(Mathf.Min(rig.ClipLength("Yes"), 2.0f));
            rig.Play("Idle_Talking_Loop", 0.3f);
        }

        public IEnumerator ShakeHead()
        {
            rig.Play("Idle_No_Loop", 0.2f);
            yield return new WaitForSeconds(1.6f);
            rig.Play("Idle_Talking_Loop", 0.3f);
        }

        /// <summary>Scribble at a point on the board with the right hand while looking at it.</summary>
        public IEnumerator Write(Transform boardPoint, float seconds)
        {
            rig.Play("Idle_Loop", 0.3f);
            rig.ik.lookTarget = boardPoint;
            rig.ik.rightHandTarget = writeTarget;
            rig.ik.rightElbowHint = writeElbow;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                var wobble = boardPoint.right * (Mathf.Sin(t * 5.5f) * 0.14f * (t / seconds + 0.3f)) + boardPoint.up * (Mathf.Sin(t * 9f) * 0.03f);
                writeTarget.position = boardPoint.position - boardPoint.forward * 0.06f + wobble;
                writeElbow.position = writeTarget.position + Vector3.down * 0.15f + boardPoint.right * 0.35f - boardPoint.forward * 0.25f;
                yield return null;
            }
            rig.ik.rightHandTarget = null;
            rig.ik.rightElbowHint = null;
        }
    }
}
