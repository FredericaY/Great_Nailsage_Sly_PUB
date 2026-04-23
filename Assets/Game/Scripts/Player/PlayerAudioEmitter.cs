using UnityEngine;
using Game.Audio;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public class PlayerAudioEmitter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PlayerRoot root;

        [Header("Walk Loop")]
        [SerializeField] private bool emitWalkLoopSfx = true;
        [SerializeField] private float walkLoopSpeedThreshold = 0.15f;

        private bool _walkLoopPlaying;

        private void Reset()
        {
            AutoWire();
        }

        private void Awake()
        {
            AutoWire();
        }

        private void Update()
        {
            UpdateWalkLoopSfx();
        }

        private void OnDisable()
        {
            StopWalkLoopSfx();
        }

        public void EmitSfxPlayerAttackSlash()
        {
            AudioService.Ensure().PlaySfxByKey(AudioKeys.Player.AttackSlash, transform.position, transform);
        }

        public void EmitSfxPlayerAttackUpper()
        {
            AudioService.Ensure().PlaySfxByKey(AudioKeys.Player.AttackUpper, transform.position, transform);
        }

        public void EmitSfxPlayerAttackDownAir()
        {
            AudioService.Ensure().PlaySfxByKey(AudioKeys.Player.AttackDownAir, transform.position, transform);
        }
        

        private void UpdateWalkLoopSfx()
        {
            if (root == null || root.Rb == null || root.Ground == null)
            {
                StopWalkLoopSfx();
                return;
            }

            if (!emitWalkLoopSfx)
            {
                StopWalkLoopSfx();
                return;
            }

            bool grounded = root.Ground.IsGrounded;
            bool moving = Mathf.Abs(root.Rb.velocity.x) >= Mathf.Max(0f, walkLoopSpeedThreshold);
            bool shouldPlay = grounded && moving;

            if (_walkLoopPlaying == shouldPlay) return;
            _walkLoopPlaying = shouldPlay;

            AudioService.Ensure().SetLoopSfxByKey(
                AudioKeys.Player.WalkLoop,
                shouldPlay,
                transform.position,
                root != null ? root.transform : transform);
        }

        private void StopWalkLoopSfx()
        {
            if (!_walkLoopPlaying) return;
            _walkLoopPlaying = false;
            AudioService audio = AudioService.Instance;
            if (audio != null) audio.StopLoopByKey(AudioKeys.Player.WalkLoop);
        }

        private void AutoWire()
        {
            if (!root) root = GetComponent<PlayerRoot>();
            if (!root) root = GetComponentInParent<PlayerRoot>();
        }
    }
}
