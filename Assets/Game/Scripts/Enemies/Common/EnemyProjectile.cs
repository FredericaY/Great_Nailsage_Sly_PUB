using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyProjectile : MonoBehaviour
    {
        public enum ProjectileKind
        {
            GroundForwardX = 0,
            AimAtTargetOnSpawn = 1,
            TimedHomingTarget = 2
        }

        [Header("Projectile Data")]
        [SerializeField] private ProjectileKind projectileKind = ProjectileKind.GroundForwardX;
        [SerializeField] private int projectileDamage = 10;
        [SerializeField] private LayerMask projectileHittableLayers;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float maxLifeTime = 2f;
        [SerializeField] private bool destroyOnMoveRangeBound = true;
        [SerializeField] private float boundaryPadding = 0.05f;

        [Header("Timed Homing Data")]
        [SerializeField] private float homingUpdateInterval = 0.08f;
        [SerializeField] private float homingDuration = 1.2f;
        [SerializeField] private Transform defaultTarget;

        public ProjectileKind Kind => projectileKind;
        public int ProjectileDamage => Mathf.Max(0, projectileDamage);
        public LayerMask ProjectileHittableLayers => projectileHittableLayers;
        public float Speed => Mathf.Max(0f, speed);
        public float MaxLifeTime => Mathf.Max(0.05f, maxLifeTime);
        public bool DestroyOnMoveRangeBound => destroyOnMoveRangeBound;
        public float BoundaryPadding => Mathf.Max(0f, boundaryPadding);
        public float HomingUpdateInterval => Mathf.Max(0.02f, homingUpdateInterval);
        public float HomingDuration => Mathf.Max(0f, homingDuration);
        public Transform DefaultTarget => defaultTarget;
    }
}
