using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    // ─────────────────────────────
    // Reads player input and exposes it as simple, frame-safe values.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerInput : MonoBehaviour
    {
        // ─────────────────────────────
        // Input Settings
        // ─────────────────────────────
        [Header("Input Settings")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private string attackButton = "Fire1";
        // ─────────────────────────────
        // Public accessors (read-only)
        // ─────────────────────────────
        // movement
        public float MoveX { get; private set; }
        public float MoveY { get; private set; }  
        // Jump
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpReleased { get; private set; }

        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackReleased { get; private set; }
        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Update()
        {
            // Movement
            MoveX = Input.GetAxisRaw(horizontalAxis);
            MoveY = Input.GetAxisRaw(verticalAxis);    
            // Jump
            JumpPressed = Input.GetButtonDown(jumpButton);
            JumpHeld = Input.GetButton(jumpButton);
            JumpReleased = Input.GetButtonUp(jumpButton);
            
            AttackPressed = Input.GetButtonDown(attackButton);
            AttackHeld = Input.GetButton(attackButton);
            AttackReleased = Input.GetButtonUp(attackButton);
        }

        public void ConsumeJump()
        {
            JumpPressed = false;
        }
        public void ConsumeJumpReleased()
        {
            JumpReleased = false;
        }


        public void ConsumeAttack()
        {
            AttackPressed = false;
        }
    }
}

