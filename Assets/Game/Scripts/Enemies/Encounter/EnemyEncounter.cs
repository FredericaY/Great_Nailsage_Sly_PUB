using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemyEncounter : MonoBehaviour
{
    public enum EncounterMode
    {
        Ambient = 0,
        BossSpawnOnPlayerEnter = 1
    }

    [Header("Mode")]
    [SerializeField] private EncounterMode mode = EncounterMode.Ambient;

    [Header("Boss Spawn")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private bool activateOnce = true;
    [SerializeField] private bool parentSpawnedBossToEncounter = true;

    [Header("Trigger (for boss mode)")]
    [SerializeField] private EncounterTriggerZone2D triggerZone;

    [Header("Hooks (for future UI/VFX)")]
    [SerializeField] private UnityEvent onEncounterActivated;
    [SerializeField] private UnityEvent onBossSpawned;

    public event Action<EnemyEncounter> EncounterActivated;
    public event Action<EnemyEncounter, EnemyRoot> BossSpawned;

    public EncounterMode Mode => mode;
    public bool IsActivated { get; private set; }
    public EnemyRoot SpawnedBossRoot { get; private set; }

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();

        if (mode == EncounterMode.Ambient)
            IsActivated = true;
    }

    private void AutoWire()
    {
        if (!triggerZone)
            triggerZone = GetComponentInChildren<EncounterTriggerZone2D>(includeInactive: true);

        if (triggerZone)
            triggerZone.SetEncounter(this);
    }

    public bool TryActivate(GameObject activator = null)
    {
        if (activateOnce && IsActivated)
            return false;

        IsActivated = true;
        onEncounterActivated?.Invoke();
        EncounterActivated?.Invoke(this);

        if (mode == EncounterMode.BossSpawnOnPlayerEnter)
            SpawnBoss();

        return true;
    }

    public EnemyRoot SpawnBoss()
    {
        if (SpawnedBossRoot != null)
            return SpawnedBossRoot;

        if (!bossPrefab)
        {
            Debug.LogWarning($"[{name}] EnemyEncounter has no bossPrefab assigned.", this);
            return null;
        }

        Transform spawn = bossSpawnPoint ? bossSpawnPoint : transform;
        GameObject bossInstance = Instantiate(bossPrefab, spawn.position, spawn.rotation);

        if (parentSpawnedBossToEncounter)
            bossInstance.transform.SetParent(transform, worldPositionStays: true);

        SpawnedBossRoot = bossInstance.GetComponent<EnemyRoot>();
        if (!SpawnedBossRoot)
            SpawnedBossRoot = bossInstance.GetComponentInChildren<EnemyRoot>();

        onBossSpawned?.Invoke();
        BossSpawned?.Invoke(this, SpawnedBossRoot);
        return SpawnedBossRoot;
    }
}
