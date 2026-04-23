using UnityEngine;
using Game.Combat;
using Game.Player;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyProjectileBase2D : BaseProjectile2D
    {
        [Header("Enemy Projectile Data Source")]
        [Tooltip("Runtime: prefer InitData.enemyProjectileData. Fallback: auto-find in parent.")]
        [SerializeField] protected EnemyProjectileData projectileData;

        [Header("Fallback Data")]
        [SerializeField] protected int fallbackDamage = 1;
        [SerializeField] protected LayerMask fallbackHittableLayers;
        [SerializeField] protected float fallbackSpeed = 8f;
        [SerializeField] protected float fallbackLifeTime = 2f;
        [SerializeField] protected bool fallbackDestroyOnMoveRangeBound = true;
        [SerializeField] protected float fallbackBoundaryPadding = 0.05f;
        [SerializeField] protected float fallbackHomingUpdateInterval = 0.08f;
        [SerializeField] protected float fallbackHomingDuration = 0f;

        [Header("Enemy Defaults")]
        [SerializeField] private bool forceEnemyProjectileLayer = true;
        [SerializeField] private string enemyProjectileLayerName = "EnemyProjectile";

        private Transform _target;
        private float _homingEndTime;
        private float _nextHomingUpdateTime;

        protected override void Awake()
        {
            base.Awake();
            if (projectileData == null)
                projectileData = GetComponentInParent<EnemyProjectileData>();
            ApplyEnemyProjectileLayerIfNeeded();
        }

        public override void Init(ProjectileInitData initData)
        {
            if (initData.enemyProjectileData != null)
                projectileData = initData.enemyProjectileData;

            Transform fallbackTarget = projectileData != null ? projectileData.DefaultTarget : null;
            _target = ResolveAimPointTarget(initData.target != null ? initData.target : fallbackTarget);

            _homingEndTime = Time.time + (projectileData != null ? projectileData.HomingDuration : fallbackHomingDuration);
            _nextHomingUpdateTime = Time.time;

            base.Init(initData);
        }

        protected override int ResolveDamage()
        {
            return projectileData != null ? projectileData.ProjectileDamage : fallbackDamage;
        }

        protected override LayerMask ResolveHittableLayers()
        {
            LayerMask layers = projectileData != null ? projectileData.ProjectileHittableLayers : fallbackHittableLayers;
            if (layers.value != 0) return layers;
            return LayerMask.GetMask("Player");
        }

        protected override float ResolveSpeed()
        {
            return projectileData != null ? projectileData.Speed : fallbackSpeed;
        }

        protected override float ResolveLifeTime()
        {
            return projectileData != null ? projectileData.MaxLifeTime : fallbackLifeTime;
        }

        protected override bool ShouldUseMoveRangeBounds()
        {
            return projectileData != null ? projectileData.DestroyOnMoveRangeBound : fallbackDestroyOnMoveRangeBound;
        }

        protected override float ResolveBoundaryPadding()
        {
            return projectileData != null ? projectileData.BoundaryPadding : fallbackBoundaryPadding;
        }

        protected override Vector2 ResolveInitialDirection(Vector2 castDirection, Transform targetRef)
        {
            Vector2 forward = castDirection.sqrMagnitude > 0f ? castDirection.normalized : Vector2.right;
            float faceX = Mathf.Sign(forward.x == 0f ? 1f : forward.x);

            if (projectileData == null) return new Vector2(faceX, 0f);

            if (projectileData.Kind == EnemyProjectileData.ProjectileKind.GroundForwardX)
                return new Vector2(faceX, 0f);

            Transform t = ResolveAimPointTarget(targetRef != null ? targetRef : _target);
            if (t == null) return forward;

            Vector2 aimed = ((Vector2)t.position - (Vector2)transform.position).normalized;
            if (aimed.sqrMagnitude <= 0.0001f) return forward;
            return aimed;
        }

        protected override void UpdateDirectionIfNeeded()
        {
            if (projectileData == null) return;
            if (projectileData.Kind != EnemyProjectileData.ProjectileKind.TimedHomingTarget) return;
            if (_target == null) return;
            if (Time.time > _homingEndTime) return;
            if (Time.time < _nextHomingUpdateTime) return;

            Vector2 desired = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            if (desired.sqrMagnitude > 0.0001f)
                Direction = desired;

            float interval = Mathf.Max(0.01f, projectileData.HomingUpdateInterval);
            _nextHomingUpdateTime = Time.time + interval;
        }

        private void ApplyEnemyProjectileLayerIfNeeded()
        {
            if (!forceEnemyProjectileLayer) return;
            if (string.IsNullOrEmpty(enemyProjectileLayerName)) return;
            int layer = LayerMask.NameToLayer(enemyProjectileLayerName);
            if (layer >= 0) gameObject.layer = layer;
        }

        private static Transform ResolveAimPointTarget(Transform rawTarget)
        {
            if (rawTarget == null) return null;
            PlayerRoot playerRoot = rawTarget.GetComponentInParent<PlayerRoot>();
            if (playerRoot != null && playerRoot.AimPoint != null)
                return playerRoot.AimPoint;
            return rawTarget;
        }
    }
}
