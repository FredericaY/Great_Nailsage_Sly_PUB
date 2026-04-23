using UnityEngine;
using Game.Combat;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyEncounter))]
    public class BossDefeatHealer : MonoBehaviour
    {
        [Header("Healing")]
        [SerializeField] private int healAmount = 3;

        private EnemyEncounter _encounter;

        private void Awake()
        {
            _encounter = GetComponent<EnemyEncounter>();
        }

        private void OnEnable()
        {
            if (_encounter != null)
                _encounter.BossDefeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            if (_encounter != null)
                _encounter.BossDefeated -= OnBossDefeated;
        }

        private void OnBossDefeated(EnemyEncounter encounter, EnemyRoot bossRoot)
        {
            HeartsHealth playerHearts = FindObjectOfType<HeartsHealth>();
            if (playerHearts == null)
            {
                Debug.LogWarning("[BossDefeatHealer] Could not find HeartsHealth on any GameObject.");
                return;
            }

            playerHearts.Restore(healAmount);
            Debug.Log($"[BossDefeatHealer] Boss defeated — restored {healAmount} HP to player.");
        }
    }
}
