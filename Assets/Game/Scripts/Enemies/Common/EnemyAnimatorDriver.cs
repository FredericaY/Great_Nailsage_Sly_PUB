using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        public enum DeadParamMode
        {
            Bool = 0,
            Trigger = 1
        }

        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Grounded Param (Optional)")]
        [SerializeField] private bool writeGrounded = true;
        [SerializeField] private string groundedParam = "Grounded";

        [Header("Dead Param (Optional)")]
        [SerializeField] private bool writeDead = true;
        [SerializeField] private DeadParamMode deadParamMode = DeadParamMode.Bool;
        [SerializeField] private string deadParam = "Dead";

        private EnemyRoot root;
        private int groundedHash;
        private int deadHash;
        private bool hasGroundedParam;
        private bool hasDeadParam;
        private bool deadTriggerSent;

        private void Reset()
        {
            root = GetComponent<EnemyRoot>();
            if (!animator) animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            root = GetComponent<EnemyRoot>();
            if (!animator && root != null) animator = root.Animator;
            if (!animator) animator = GetComponentInChildren<Animator>(true);

            groundedHash = Animator.StringToHash(groundedParam);
            deadHash = Animator.StringToHash(deadParam);
            deadTriggerSent = false;

            CacheParamAvailability();
        }

        private void LateUpdate()
        {
            if (!animator || root == null) return;

            if (writeGrounded && hasGroundedParam)
            {
                bool grounded = root.Ground != null && root.Ground.IsGrounded;
                animator.SetBool(groundedHash, grounded);
            }

            if (writeDead && hasDeadParam)
            {
                bool isDead = false;
                if (root.HpHealth != null)
                    isDead = root.HpHealth.IsDead;
                else if (root.Blackboard != null)
                    isDead = root.Blackboard.isDead;

                if (deadParamMode == DeadParamMode.Trigger)
                {
                    if (isDead && !deadTriggerSent)
                    {
                        animator.SetTrigger(deadHash);
                        deadTriggerSent = true;
                    }
                    else if (!isDead)
                    {
                        deadTriggerSent = false;
                    }
                }
                else
                {
                    animator.SetBool(deadHash, isDead);
                }
            }
        }

        private void CacheParamAvailability()
        {
            hasGroundedParam = HasBoolParam(animator, groundedParam);
            hasDeadParam = deadParamMode == DeadParamMode.Trigger
                ? HasTriggerParam(animator, deadParam)
                : HasBoolParam(animator, deadParam);
        }

        private static bool HasBoolParam(Animator anim, string paramName)
        {
            if (anim == null || string.IsNullOrEmpty(paramName)) return false;
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                    return true;
            }
            return false;
        }

        private static bool HasTriggerParam(Animator anim, string paramName)
        {
            if (anim == null || string.IsNullOrEmpty(paramName)) return false;
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
                    return true;
            }
            return false;
        }
    }
}
