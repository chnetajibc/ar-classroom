using UnityEngine;

namespace RealisticClassroom
{
    /// <summary>Shared references for every person in the room (created by the scene builder).</summary>
    public class ActorRig : MonoBehaviour
    {
        public string displayName;
        public Animator animator;
        public HumanoidIK ik;
        public Transform head;
        public float height = 1.6f;

        public Transform LookPoint => head != null ? head : transform;

        /// <summary>Cross-fade to a state by name (state names equal clip names in the controllers).</summary>
        public void Play(string state, float fade = 0.25f, float normalizedOffset = 0f)
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(state, fade, 0, normalizedOffset * ClipLength(state));
        }

        public float ClipLength(string state)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return 1f;
            foreach (var c in animator.runtimeAnimatorController.animationClips)
                if (c.name == state) return c.length;
            return 1f;
        }
    }
}
