using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public class PlayerLock : MonoBehaviour
    {
        private PlayerRoot _root;
        private int _lockCount;

        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }

        public bool IsLocked => _lockCount > 0;

        public void Acquire()
        {
            _lockCount++;
            ApplyLockedState(true);
        }

        public void Release()
        {
            _lockCount = Mathf.Max(0, _lockCount - 1);
            ApplyLockedState(_lockCount > 0);
        }

        public void ForceClear()
        {
            _lockCount = 0;
            ApplyLockedState(false);
        }

        private void ApplyLockedState(bool locked)
        {
            if (_root == null) return;

            var controller = _root.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = !locked;
            
            if (_root.Movement != null) _root.Movement.enabled = !locked;
            if (_root.Jump != null) _root.Jump.enabled = !locked;
            if (_root.Combat != null) _root.Combat.enabled = !locked;
            
            if (_root.Rb != null)
            {
                _root.Rb.velocity = Vector2.zero;
                _root.Rb.angularVelocity = 0f;
                _root.Rb.gravityScale = locked ? 0f : _defaultGravity;
            }
        }

        private float _defaultGravity = 1f;
        private void Start()
        {
            if (_root != null && _root.Rb != null)
                _defaultGravity = _root.Rb.gravityScale;
        }
    }
}
