using UnityEngine;
using Game.Audio;

[DisallowMultipleComponent]
public class ASPAudioEmitter : MonoBehaviour
{
    [SerializeField] private EnemyRoot root;
    [SerializeField] private EnemyBlackboard blackboard;
    [SerializeField] private bool emitFlyLoopSfx = true;
    [SerializeField] private bool debugAudio = false;

    private bool isLoopPlaying;
    private string loopRuntimeKey;

    private void Awake()
    {
        AutoWire();
        loopRuntimeKey = $"{AudioKeys.Enemy.Aspid.FlyLoop}.{GetInstanceID()}";
    }

    private void Reset()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        UpdateLoopState();
    }

    private void Update()
    {
        UpdateLoopState();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void AutoWire()
    {
        if (!root) root = GetComponent<EnemyRoot>();
        if (!root) root = GetComponentInParent<EnemyRoot>();

        if (!blackboard) blackboard = GetComponent<EnemyBlackboard>();
        if (!blackboard && root != null) blackboard = root.Blackboard;
        if (!blackboard) blackboard = GetComponentInParent<EnemyBlackboard>();
    }

    private void UpdateLoopState()
    {
        bool shouldPlay = emitFlyLoopSfx && (blackboard == null || !blackboard.isDead);
        if (!shouldPlay)
        {
            if (isLoopPlaying) StopLoop();
            return;
        }

        // already playing
        if (isLoopPlaying) return;

        AudioService audio = AudioService.Ensure();
        bool ok = audio.SetLoopSfxByKey(
            loopRuntimeKey,
            AudioKeys.Enemy.Aspid.FlyLoop,
            shouldPlay,
            transform.position,
            root != null ? root.transform : transform);
        isLoopPlaying = ok;

        if (debugAudio)
        {
            bool dead = blackboard != null && blackboard.isDead;
            Debug.Log(
                $"[ASPAudioEmitter:{name}] FlyLoop try shouldPlay={shouldPlay}, ok={ok}, dead={dead}, runtimeKey={loopRuntimeKey}, cueKey={AudioKeys.Enemy.Aspid.FlyLoop}, " +
                $"master={audio.MasterVolume:0.00}, sfx={audio.SfxBusVolume:0.00}",
                this);
        }
    }

    private void StopLoop()
    {
        if (!isLoopPlaying) return;
        isLoopPlaying = false;
        AudioService audio = AudioService.Instance;
        if (audio != null) audio.StopLoopByKey(loopRuntimeKey);
    }

    /// <summary>
    /// Animation event: play ASP attack SFX.
    /// </summary>
    public void AnimEvent_SfxEnemyAspidAttack()
    {
        EmitSfxEnemyAspidAttack();
    }

    public void EmitSfxEnemyAspidAttack()
    {
        AudioService audio = AudioService.Ensure();
        bool ok = audio.PlaySfxByKey(
            AudioKeys.Enemy.Aspid.Attack,
            transform.position,
            root != null ? root.transform : transform);

        if (debugAudio)
        {
            Debug.Log(
                $"[ASPAudioEmitter:{name}] AttackSfx play ok={ok}, key={AudioKeys.Enemy.Aspid.Attack}, " +
                $"master={audio.MasterVolume:0.00}, sfx={audio.SfxBusVolume:0.00}",
                this);
        }
    }
    
}
