using UnityEngine;

namespace RealisticClassroom
{
    /// <summary>
    /// Runs on the GameObject that owns the humanoid Animator (needs an IK pass on the controller layer).
    /// Drives head/eye look-at, pins seated feet to the floor and can raise or place the right hand.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HumanoidIK : MonoBehaviour
    {
        [Header("Look")]
        public Transform lookTarget;
        [Range(0f, 1f)] public float lookWeight = 0.85f;
        public float lookSpeed = 4f;

        [Header("Feet")]
        public bool pinFeetToFloor;
        public float floorY;
        public float ankleHeight = 0.04f;

        [Header("Right hand")]
        public Transform rightHandTarget;
        public Transform rightElbowHint;

        Animator animator;
        Vector3 smoothLook;
        bool lookInit;
        float lookBlend;
        float handBlend;
        Transform handCache, elbowCache;

        void Awake() { animator = GetComponent<Animator>(); }

        void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;
            float dt = Time.deltaTime;

            float wantLook = lookTarget != null ? lookWeight : 0f;
            lookBlend = Mathf.MoveTowards(lookBlend, wantLook, dt * 2.5f);
            if (lookTarget != null)
            {
                if (!lookInit) { smoothLook = lookTarget.position; lookInit = true; }
                smoothLook = Vector3.Lerp(smoothLook, lookTarget.position, 1f - Mathf.Exp(-lookSpeed * dt));
            }
            animator.SetLookAtWeight(lookBlend, 0.25f, 0.85f, 0.35f, 0.6f);
            animator.SetLookAtPosition(smoothLook);

            if (pinFeetToFloor)
            {
                PinFoot(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee);
                PinFoot(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee);
            }

            if (rightHandTarget != null) { handCache = rightHandTarget; elbowCache = rightElbowHint; }
            handBlend = Mathf.MoveTowards(handBlend, rightHandTarget != null ? 1f : 0f, dt * 2.2f);
            if (handBlend > 0.001f && handCache != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handBlend);
                animator.SetIKPosition(AvatarIKGoal.RightHand, handCache.position);
                if (elbowCache != null)
                {
                    animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, handBlend);
                    animator.SetIKHintPosition(AvatarIKHint.RightElbow, elbowCache.position);
                }
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
            }
        }

        void PinFoot(AvatarIKGoal goal, AvatarIKHint hint)
        {
            var p = animator.GetIKPosition(goal);
            p.y = floorY + ankleHeight;
            animator.SetIKPositionWeight(goal, 1f);
            animator.SetIKPosition(goal, p);
            animator.SetIKRotationWeight(goal, 0.6f);
            var r = animator.GetIKRotation(goal);
            var fwd = r * Vector3.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-4f) animator.SetIKRotation(goal, Quaternion.LookRotation(fwd.normalized, Vector3.up));
            animator.SetIKHintPositionWeight(hint, 1f);
            animator.SetIKHintPosition(hint, animator.GetIKHintPosition(hint));
        }
    }
}
