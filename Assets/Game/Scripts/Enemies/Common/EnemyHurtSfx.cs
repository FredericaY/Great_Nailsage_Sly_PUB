using UnityEngine;
using Game.Audio;
using Game.Combat;

namespace Game.Enemies
{
    // EnemyHurtSfx
    // - Listens to hurt events from root HpHealth.
    // - Plays one shared enemy hurt SFX cue.
    [DisallowMultipleComponent]
    public class EnemyHurtSfx : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyRoot root;
        [SerializeField] private Transform emitPoint;

        private HpHealth _hpHealth;

        private void Reset()
        {
            AutoAssignRefs();
        }

        private void Awake()
        {
            AutoAssignRefs();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void AutoAssignRefs()
        {
            if (root == null)
                root = GetComponent<EnemyRoot>();
            if (root == null)
                root = GetComponentInParent<EnemyRoot>();

            if (emitPoint == null)
                emitPoint = transform;
        }

        private void Bind()
        {
            Unbind();
            AutoAssignRefs();

            if (root == null) return;
            _hpHealth = root.HpHealth;
            if (_hpHealth != null)
                _hpHealth.OnHurt += OnHurt;
        }

        private void Unbind()
        {
            if (_hpHealth != null)
                _hpHealth.OnHurt -= OnHurt;
            _hpHealth = null;
        }

        private void OnHurt(DamageInfo info)
        {
            if (_hpHealth != null && _hpHealth.IsDead) return;

            Transform point = emitPoint != null ? emitPoint : transform;
            AudioService.Ensure().PlaySfxByKey(AudioKeys.Enemy.Hurt, point.position, point);
        }
    }
}
