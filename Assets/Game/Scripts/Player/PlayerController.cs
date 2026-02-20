using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Coordinates player modules by routing input into actions.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRoot))]
    public class PlayerController : MonoBehaviour
    {
        // ─────────────────────────────
        // Settings
        // ─────────────────────────────
        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }
        private void Update()
        {
            // make sure the component is enough
            if (_root.Input == null || _root.Movement == null || _root.Combat == null)
                return;

            // 1) Movement routing
            _root.Movement.SetMoveInput(_root.Input.MoveX);
            _root.Facing?.FaceByMoveX(_root.Input.MoveX);
            
            // 2) Jump routing
            if (_root.Input.JumpPressed)
            {
                if (_root.Jump != null && _root.Jump.TryJump())
                {
                    _root.Input.ConsumeJump();
                }
            }
            if (_root.Jump != null && _root.Input.JumpReleased)
            {
                _root.Jump.OnJumpReleased();
                _root.Input.ConsumeJumpReleased();
            }




            // 3) Attack routing
// 3) Attack routing (forward only)
            if (_root.Input.AttackPressed)
            {
                _root.Combat.OnAttackPressed();
                _root.Input.ConsumeAttack();
            }

            if (_root.Input.AttackHeld)
            {
                _root.Combat.OnAttackHeld(Time.deltaTime);
            }
            else
            {
                _root.Combat.OnAttackReleased();
            }

            
        }
        

    }

}
