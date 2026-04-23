using UnityEngine;
using Game.Enemies;

namespace Game.Combat
{
    /// <summary>
    /// Runtime payload passed from emitters to projectile instances.
    /// Keep this compact and extensible for both enemy/player projectiles.
    /// </summary>
    public struct ProjectileInitData
    {
        public GameObject owner;
        public Vector2 castDirection;
        public Transform target;
        public EnemyMoveRange2D moveRange;
        public EnemyProjectileData enemyProjectileData;
    }
}

