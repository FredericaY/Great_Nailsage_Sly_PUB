using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyAttack : MonoBehaviour
    {
        [Header("Melee Attack Data")]
        [SerializeField] private int meleeDamage = 10;
        [SerializeField] private float meleeHitboxLifetime = 0.12f;
        [SerializeField] private LayerMask meleeHittableLayers;

        public int MeleeDamage => Mathf.Max(0, meleeDamage);
        public float MeleeHitboxLifetime => Mathf.Max(0f, meleeHitboxLifetime);
        public LayerMask MeleeHittableLayers => meleeHittableLayers;
    }
}
