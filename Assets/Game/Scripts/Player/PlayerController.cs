using UnityEngine;

namespace Game.Player
{
    // PlayerController
    // - Routes input into movement, jump and attack modules.
    // - Owns facing arbitration between movement and wall logic.
    // - Applies short wall-exit facing lock to prevent flicker.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRoot))]
    public class PlayerController : MonoBehaviour
    {
        // ------------------------------
        // Types
        // ------------------------------
        private enum WallFacingMode
        {
            FaceWall = 0,
            FaceAwayFromWall = 1
        }

        // ------------------------------
        // Config: Wall Facing
        // ------------------------------
        [Header("Wall Facing")]
        [SerializeField] private WallFacingMode wallFacingMode = WallFacingMode.FaceWall;
        [SerializeField, Min(0f)] private float wallExitFacingLockTime = 0.12f;
        [SerializeField, Range(0f, 1f)] private float faceInputDeadZone = 0.01f;

        // ------------------------------
        // Outlets / Runtime state
        // ------------------------------
        private PlayerRoot _root;
        private bool _wasOnWallLastFrame;
        private float _wallFaceLockUntil;

        // ------------------------------
        // Methods
        // ------------------------------
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();
        }

        private void Update()
        {
            if (_root.Input == null || _root.Movement == null || _root.Combat == null)
                return;

            // 1) Movement routing
            _root.Movement.SetMoveInput(_root.Input.MoveX);
            UpdateFacing();

            // 2) Jump routing
            if (_root.Input.JumpPressed)
            {
                if (_root.Jump != null && _root.Jump.TryJump())
                    _root.Input.ConsumeJump();
            }

            if (_root.Jump != null && _root.Input.JumpReleased)
            {
                _root.Jump.OnJumpReleased();
                _root.Input.ConsumeJumpReleased();
            }

            // 3) Attack routing (forward only)
            if (_root.Input.AttackPressed)
            {
                _root.Combat.OnAttackPressed();
                _root.Input.ConsumeAttack();
            }

            if (_root.Input.AttackHeld)
                _root.Combat.OnAttackHeld(Time.deltaTime);
            else
                _root.Combat.OnAttackReleased();
        }

        private void UpdateFacing()
        {
            if (_root.Facing == null || _root.Ground == null || _root.Wall == null || _root.Input == null)
                return;

            bool isOnWallInAir = !_root.Ground.IsGrounded && _root.Wall.IsOnWall && _root.Wall.WallSide != 0;

            if (isOnWallInAir)
            {
                _root.Facing.SetFacing(GetWallFacingDir(_root.Wall.WallSide));
                _wasOnWallLastFrame = true;
                return;
            }

            if (_wasOnWallLastFrame)
            {
                _wallFaceLockUntil = Time.time + wallExitFacingLockTime;
                _wasOnWallLastFrame = false;
            }

            // Keep last wall-facing for a short moment after leaving wall to avoid flicker.
            if (Time.time < _wallFaceLockUntil)
                return;

            float moveX = _root.Input.MoveX;
            if (Mathf.Abs(moveX) < faceInputDeadZone)
                return;

            _root.Facing.FaceByMoveX(moveX, faceInputDeadZone);
        }

        private PlayerFacing.FacingDir GetWallFacingDir(int wallSide)
        {
            // wallSide: -1 = left wall, 1 = right wall.
            if (wallFacingMode == WallFacingMode.FaceWall)
                return wallSide < 0 ? PlayerFacing.FacingDir.Left : PlayerFacing.FacingDir.Right;

            return wallSide < 0 ? PlayerFacing.FacingDir.Right : PlayerFacing.FacingDir.Left;
        }
    }
}
