using UnityEngine;
using Game.Audio;

[DisallowMultipleComponent]
public class FKAudioEmitter : MonoBehaviour
{
    [SerializeField] private EnemyRoot root;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
    }

    private void AutoWire()
    {
        if (!root) root = GetComponent<EnemyRoot>();
        if (!root) root = GetComponentInParent<EnemyRoot>();
    }

    public void AnimEvent_SfxEnemyFalseKnightAttack()
    {
        EmitSfxEnemyFalseKnightAttack();
    }

    public void EmitSfxEnemyFalseKnightAttack()
    {
        PlayByKey(AudioKeys.Enemy.FalseKnight.Attack);
    }

    public void AnimEvent_SfxEnemyFalseKnightStrikeGround()
    {
        EmitSfxEnemyFalseKnightStrikeGround();
    }

    public void EmitSfxEnemyFalseKnightStrikeGround()
    {
        PlayByKey(AudioKeys.Enemy.FalseKnight.StrikeGround);
    }

    public void AnimEvent_SfxEnemyFalseKnightJump()
    {
        EmitSfxEnemyFalseKnightJump();
    }

    public void EmitSfxEnemyFalseKnightJump()
    {
        PlayByKey(AudioKeys.Enemy.FalseKnight.Jump);
    }

    public void AnimEvent_SfxEnemyFalseKnightLand()
    {
        EmitSfxEnemyFalseKnightLand();
    }

    public void EmitSfxEnemyFalseKnightLand()
    {
        PlayByKey(AudioKeys.Enemy.FalseKnight.Land);
    }

    private void PlayByKey(string key)
    {
        AudioService.Ensure().PlaySfxByKey(
            key,
            transform.position,
            root != null ? root.transform : transform);
    }
}
