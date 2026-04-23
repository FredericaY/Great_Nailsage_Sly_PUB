using UnityEngine;
using Game.Audio;
using Game.Combat;

namespace Game.Player
{
    // PlayerHurtSfx
    // - Listens to hurt events from HeartsHealth.
    // - Plays one-shot hurt SFX using audio key routing.
    [DisallowMultipleComponent]
    public class PlayerHurtSfx : MonoBehaviour
    {
        // ------------------------------
        // Config: References
        // ------------------------------
        [Header("References")]
        [SerializeField] private HeartsHealth heartsHealth;
        [SerializeField] private Transform emitPoint;

        // ------------------------------
        // Methods
        // ------------------------------
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
            if (heartsHealth != null)
                heartsHealth.OnHurt += OnHurt;
        }

        private void OnDisable()
        {
            if (heartsHealth != null)
                heartsHealth.OnHurt -= OnHurt;
        }

        private void AutoAssignRefs()
        {
            if (heartsHealth == null)
                heartsHealth = GetComponent<HeartsHealth>();
            if (heartsHealth == null)
                heartsHealth = GetComponentInParent<HeartsHealth>();

            if (emitPoint == null)
                emitPoint = transform;
        }

        private void OnHurt(DamageInfo info)
        {
            Transform point = emitPoint != null ? emitPoint : transform;
            AudioService.Ensure().PlaySfxByKey(AudioKeys.Player.Hurt, point.position, point);
        }
    }
}
