using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Combat;

namespace Game.Player
{
    // ─────────────────────────────
    // PlayerDeath
    // - Listens to IDamageable death event.
    // - Reloads current scene on player death.
    // - Independent from specific health implementation.
    // ─────────────────────────────
    [DisallowMultipleComponent]
    public class PlayerDeath : MonoBehaviour
    {
        // ─────────────────────────────
        // Outlets
        // ─────────────────────────────
        private PlayerRoot _root;
        private IDamageable _damageable;

        // ─────────────────────────────
        // Methods
        // ─────────────────────────────
        private void Awake()
        {
            _root = GetComponent<PlayerRoot>();

            // Supports either HeartsHealth or HpHealth
            _damageable = GetComponent<IDamageable>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_damageable is HeartsHealth hearts)
                hearts.OnDeath += OnDeath;
            else if (_damageable is HpHealth hp)
                hp.OnDeath += OnDeath;
        }

        private void Unsubscribe()
        {
            if (_damageable is HeartsHealth hearts)
                hearts.OnDeath -= OnDeath;
            else if (_damageable is HpHealth hp)
                hp.OnDeath -= OnDeath;
        }

        private void OnDeath()
        {
            ReloadScene();
        }

        private void ReloadScene()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }
}