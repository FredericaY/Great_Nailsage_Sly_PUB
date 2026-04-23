using UnityEngine;

namespace Game.Player
{
    // PlyerAttackEmitter
    // - Receives Animation Events from graphics clips.
    // - Forwards those events to PlayerCombat.
    // - Must be on the same GameObject as the graphics Animator.
    public class PlyerAttackEmitter : MonoBehaviour
    {
        // ------------------------------
        // Outlets
        // ------------------------------
        private PlayerRoot _root;
        private PlayerAudioEmitter _audio;

        // ------------------------------
        // Methods
        // ------------------------------
        private void Awake()
        {
            _root = GetComponentInParent<PlayerRoot>();
            _audio = _root != null ? _root.GetComponent<PlayerAudioEmitter>() : null;
        }

        // Called by Animation Event.
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

    // Backward-compat for existing prefabs/components before rename.
    public class PlayerAnimEvents : PlyerAttackEmitter { }
}
