using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBlackboard : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float distanceToPlayer;

    [Header("Facing")]
    [Tooltip("True means facing right (scale.x > 0).")]
    public bool facingRight = true;

    [Header("State Flags")]
    public bool isDead;
    public bool isHurtLocked;   // 受击硬直/不可行动
    public bool isAttacking;

    [Header("Cooldowns")]
    public float nextAttackTime;

    [Header("Action Memory")]
    [Tooltip("Used to prevent backstep being selected twice in a row.")]
    public bool lastMoveWasBackstep;
    [Tooltip("Used to prevent jump-to-player being selected twice in a row.")]
    public bool lastMoveWasJumpToPlayer;

    [Header("Config")]
    public string playerTag = "Player";

    void Update()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }

        if (player)
        {
            distanceToPlayer = Vector2.Distance(transform.position, player.position);
        }
    }

    public void FacePlayerByFlippingRoot()
    {
        if (!player) return;

        bool shouldFaceRight = player.position.x >= transform.position.x;
        if (shouldFaceRight == facingRight) return;

        facingRight = shouldFaceRight;

        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        transform.localScale = s;
    }

    public void MarkBackstepUsed()
    {
        lastMoveWasBackstep = true;
        lastMoveWasJumpToPlayer = false;
    }

    public void MarkJumpToPlayerUsed()
    {
        lastMoveWasJumpToPlayer = true;
        lastMoveWasBackstep = false;
    }

    /// <summary>
    /// Call after non-jump actions (normal/wave/jump-attack, etc.)
    /// so jump/backstep locks don't stay stuck forever.
    /// </summary>
    public void MarkOtherActionUsed()
    {
        lastMoveWasJumpToPlayer = false;
        lastMoveWasBackstep = false;
    }
}

