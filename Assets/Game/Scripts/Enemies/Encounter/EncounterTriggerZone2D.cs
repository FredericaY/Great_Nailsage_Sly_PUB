using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class EncounterTriggerZone2D : MonoBehaviour
{
    [SerializeField] private EnemyEncounter encounter;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;
    [SerializeField] private bool disableColliderAfterTriggered = true;

    private Collider2D triggerCollider;
    private bool hasTriggered;

    private void Reset()
    {
        EnsureSetup();
    }

    private void Awake()
    {
        EnsureSetup();
    }

    public void SetEncounter(EnemyEncounter value)
    {
        encounter = value;
    }

    private void EnsureSetup()
    {
        if (!triggerCollider) triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider && !triggerCollider.isTrigger)
            triggerCollider.isTrigger = true;

        if (!encounter)
            encounter = GetComponentInParent<EnemyEncounter>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        if (!encounter) return;

        bool activated = encounter.TryActivate(other.gameObject);
        if (!activated) return;

        hasTriggered = true;
        if (oneShot && disableColliderAfterTriggered && triggerCollider)
            triggerCollider.enabled = false;
    }
}
