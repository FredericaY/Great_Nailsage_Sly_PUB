using System.Collections;
using UnityEngine;
using Game.Combat;
using Game.Player;
using BehaviorDesigner.Runtime;

[DisallowMultipleComponent]
public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard bb;
    [SerializeField] private HpHealth hpHealth;

    [Header("Geo Reward")]
    [SerializeField] private int geoReward = 5;
    [Tooltip("If true, coins scatter in a random arc instead of dropping down (for flying enemies).")]
    [SerializeField] private bool isFlying = false;
    [SerializeField] private string playerTag = "Player";

    [SerializeField] private Collider2D[] collidersToDisable;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private bool disableRigidbodyOnDeath = true;

    private static readonly int T_Dead = Animator.StringToHash("Dead");

    void Reset()
    {
        root = GetComponent<EnemyRoot>();
        bb = GetComponent<EnemyBlackboard>();
        hpHealth = GetComponent<HpHealth>();
        collidersToDisable = GetComponentsInChildren<Collider2D>();
    }

    void Awake()
    {
        if (!root) root = GetComponent<EnemyRoot>();
        if (!bb) bb = GetComponent<EnemyBlackboard>();
        if (!hpHealth) hpHealth = GetComponent<HpHealth>();

        hpHealth.OnDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (hpHealth != null)
            hpHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        GiveGeoToPlayer();

        bb.isDead = true;

        if (root.BehaviorTree)
            root.BehaviorTree.enabled = false;

        foreach (var c in collidersToDisable)
            c.enabled = false;

        if (root.Animator)
            root.Animator.SetTrigger(T_Dead);

        if (disableRigidbodyOnDeath)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (!rb) rb = GetComponentInChildren<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }
        }

        if (destroyOnDeath)
            StartCoroutine(DestroyAfterDelay());
    }

    private void GiveGeoToPlayer()
    {
        if (geoReward <= 0) return;

        // Spawn coin drop(s) - player collects by walking over them
        var spawner = GeoPickupSpawner.Instance;
        if (spawner != null)
        {
            spawner.Spawn(geoReward, transform.position, isFlying);
        }
        else
        {
            // Fallback: give geo instantly if no spawner
            var player = GameObject.FindWithTag(playerTag);
            if (player != null)
            {
                var currency = player.GetComponent<PlayerCurrency>() ?? player.GetComponentInChildren<PlayerCurrency>();
                if (currency != null)
                    currency.Add(geoReward);
            }
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        float delay = Mathf.Max(0f, destroyDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (root != null)
            Destroy(root.gameObject);
        else
            Destroy(gameObject);
    }
}


