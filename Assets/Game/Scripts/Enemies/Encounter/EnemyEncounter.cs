using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Game.Player;
using Game.UI;
using Game.Utils;
using Game.Combat;

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

    [Header("Boss Intro")]
    [SerializeField] private string bossDisplayName = "Boss";
    [SerializeField] private Vector2 playerInputLockDurationRange = new Vector2(1f, 1.5f);
    [SerializeField] private float bossNameDisplayDuration = 2f;
    [SerializeField] private EncounterShakeEmitter encounterShake;

    [Header("Hooks (for future UI/VFX)")]
    [SerializeField] private UnityEvent onEncounterActivated;
    [SerializeField] private UnityEvent onBossSpawned;
    [SerializeField] private UnityEvent onBossDefeated;

    public event Action<EnemyEncounter> EncounterActivated;
    public event Action<EnemyEncounter, EnemyRoot> BossSpawned;
    public event Action<EnemyEncounter, EnemyRoot> BossDefeated;

    public EncounterMode Mode => mode;
    public bool IsActivated { get; private set; }
    public EnemyRoot SpawnedBossRoot { get; private set; }

    private HpHealth _spawnedBossHealth;
    private bool _bossDefeatedNotified;
    private bool _bossDefeated;

    private void Reset()
    {
        AutoWire();
        AutoWireIntro();
    }

    private void Awake()
    {
        AutoWire();
        AutoWireIntro();

        if (mode == EncounterMode.Ambient)
            IsActivated = true;
    }

    private void OnDisable()
    {
        UnbindBossDeath();
    }

    private void AutoWire()
    {
        if (!triggerZone)
            triggerZone = GetComponentInChildren<EncounterTriggerZone2D>(includeInactive: true);

        if (triggerZone)
            triggerZone.SetEncounter(this);
    }

    private void AutoWireIntro()
    {
        if (!encounterShake)
            encounterShake = GetComponent<EncounterShakeEmitter>();

        if (!encounterShake)
            encounterShake = GetComponentInChildren<EncounterShakeEmitter>(includeInactive: true);
    }

    public bool TryActivate(GameObject activator = null)
    {
        if (activateOnce && IsActivated)
            return false;
        if (_bossDefeated)
            return false;

        IsActivated = true;
        onEncounterActivated?.Invoke();
        EncounterActivated?.Invoke(this);

        if (mode == EncounterMode.BossSpawnOnPlayerEnter)
        {
            SpawnBoss();
            StartCoroutine(PlayBossIntroSequence(activator));
        }

        return true;
    }

    public EnemyRoot SpawnBoss()
    {
        if (_bossDefeated)
            return null;
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

        BindBossDeath(SpawnedBossRoot);

        onBossSpawned?.Invoke();
        BossSpawned?.Invoke(this, SpawnedBossRoot);
        return SpawnedBossRoot;
    }

    private IEnumerator PlayBossIntroSequence(GameObject activator)
    {
        PlayerLock playerLock = ResolvePlayerLock(activator);
        float lockDuration = ResolveLockDuration();

        if (playerLock != null)
            playerLock.Acquire();

        TriggerBossIntroShake();
        BossEncounterTitleOverlay.ShowOnHud(bossDisplayName, bossNameDisplayDuration);

        if (lockDuration > 0f)
            yield return new WaitForSecondsRealtime(lockDuration);

        if (playerLock != null)
            playerLock.Release();
    }

    private float ResolveLockDuration()
    {
        float min = Mathf.Max(0f, playerInputLockDurationRange.x);
        float max = Mathf.Max(min, playerInputLockDurationRange.y);
        return Mathf.Approximately(min, max) ? min : UnityEngine.Random.Range(min, max);
    }

    private PlayerLock ResolvePlayerLock(GameObject activator)
    {
        if (activator == null)
            return null;

        PlayerLock playerLock = activator.GetComponent<PlayerLock>();
        if (playerLock != null)
            return playerLock;

        PlayerRoot root = activator.GetComponent<PlayerRoot>() ?? activator.GetComponentInParent<PlayerRoot>();
        if (root == null)
            return null;

        return root.GetComponent<PlayerLock>();
    }

    private void TriggerBossIntroShake()
    {
        EncounterShakeEmitter shakeSource = ResolveEncounterShakeSource();
        if (shakeSource != null)
            shakeSource.ShakeEncounter();
    }

    private EncounterShakeEmitter ResolveEncounterShakeSource()
    {
        if (encounterShake != null)
            return encounterShake;

        encounterShake = GetComponent<EncounterShakeEmitter>();
        if (encounterShake != null)
            return encounterShake;

        encounterShake = GetComponentInChildren<EncounterShakeEmitter>(includeInactive: true);
        if (encounterShake != null)
            return encounterShake;

        return null;
    }

    private void BindBossDeath(EnemyRoot bossRoot)
    {
        UnbindBossDeath();

        _bossDefeatedNotified = false;
        if (bossRoot == null)
            return;

        _spawnedBossHealth = bossRoot.HpHealth != null ? bossRoot.HpHealth : bossRoot.GetComponentInChildren<HpHealth>();
        if (_spawnedBossHealth == null)
            return;

        _spawnedBossHealth.OnDeath += HandleSpawnedBossDeath;
    }

    private void UnbindBossDeath()
    {
        if (_spawnedBossHealth == null)
            return;

        _spawnedBossHealth.OnDeath -= HandleSpawnedBossDeath;
        _spawnedBossHealth = null;
    }

    private void HandleSpawnedBossDeath()
    {
        if (_bossDefeatedNotified)
            return;

        _bossDefeatedNotified = true;
        _bossDefeated = true;

        if (triggerZone != null)
            triggerZone.gameObject.SetActive(false);

        onBossDefeated?.Invoke();
        BossDefeated?.Invoke(this, SpawnedBossRoot);
        UnbindBossDeath();
    }
}
