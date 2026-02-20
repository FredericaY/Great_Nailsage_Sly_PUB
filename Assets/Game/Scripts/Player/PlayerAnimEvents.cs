using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // Must be on the same GameObject as Animator
    public class PlayerAnimEvents : MonoBehaviour
    {
        private PlayerRoot _root;

        private void Awake()
        {
            _root = GetComponentInParent<PlayerRoot>();
        }

        // Called by Animation Event
        public void AnimEvent_AttackEnd()
        {
            if (_root != null && _root.Combat != null)
                _root.Combat.AnimEvent_AttackEnd();
        }
        public void AnimEvent_SpawnAttackHitbox()
        {
            if (_root != null && _root.Combat != null)
                _root.Combat.AnimEvent_SpawnAttackHitbox();
        }

    }
}
