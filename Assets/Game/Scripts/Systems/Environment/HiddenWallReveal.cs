using System.Collections.Generic;
using Game.Player;
using UnityEngine;

namespace Game.Systems.Environment
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class HiddenWallReveal : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool activateByDistance = true;
        [SerializeField] private float activationDistance = 2.5f;
        [SerializeField] private float reenterBlockDuration = 0.6f;
        [SerializeField] private float disableNearbyRadius = 12f;

        [Header("Room")]
        [SerializeField] private Vector2 roomOffset = Vector2.zero;
        [SerializeField] private bool useTriggerSizeForRoom = true;
        [SerializeField] private Vector2 roomSize = new(14f, 8f);
        [SerializeField] private Vector2 roomSizePadding = new(2f, 2f);
        [SerializeField] private float roomSizeMultiplier = 1f;
        [SerializeField] private float wallThickness = 0.5f;
        [SerializeField] private float spawnYOffset = -2.5f;
        [SerializeField] private int roomCollisionLayer = 7;

        [Header("Exit")]
        [SerializeField] private Vector2 exitPortalOffset = new(5f, -2f);
        [SerializeField] private Vector2 exitPortalSize = new(1.2f, 2f);
        [SerializeField] private bool exitByDistanceFallback = true;
        [SerializeField] private float exitActivationPadding = 0.6f;
        [SerializeField] private float minExitDelay = 0.35f;
        [SerializeField] private bool showExitPortalInGame = false;
        [SerializeField] private bool autoCreateExitReturnPoint = true;
        [SerializeField] private Vector2 returnOffset = new(0f, 1f);
        [SerializeField] private Transform exitReturnPoint;

        [Header("Reward")]
        [SerializeField] private GameObject[] consumablePrefabs = new GameObject[0];
        [SerializeField, Min(0f)] private float consumableWeight = 0.5f;
        [SerializeField, Min(0f)] private float geoWeight = 0.5f;
        [SerializeField, Min(0)] private int minGeo = 8;
        [SerializeField, Min(0)] private int maxGeo = 20;

        private readonly List<Collider2D> _disabledColliders = new();
        private readonly Collider2D[] _overlapBuffer = new Collider2D[512];

        private Transform _roomRoot;
        private Transform _spawnPoint;
        private Vector3 _entryPos;
        private bool _hasEntryPos;
        private bool _entered;
        private bool _rewardSpawned;
        private PlayerRoot _cachedPlayer;
        private Vector3 _exitPortalPos;
        private bool _hasExitPortal;
        private float _enteredAtTime;
        private float _nextEnterAllowedAt;
        private const string AutoReturnPointName = "ExitReturnPoint_Auto";
        private static Sprite _squareSprite;

        private void Reset()
        {
            EnsureExitReturnPoint();
        }

        private void Awake()
        {
            var zone = GetComponent<Collider2D>();
            if (zone != null && !zone.isTrigger) zone.isTrigger = true;

            activationDistance = Mathf.Max(0.2f, activationDistance);
            reenterBlockDuration = Mathf.Max(0f, reenterBlockDuration);
            disableNearbyRadius = Mathf.Max(0.5f, disableNearbyRadius);
            roomSize = new Vector2(Mathf.Max(4f, roomSize.x), Mathf.Max(3f, roomSize.y));
            roomSizePadding = new Vector2(Mathf.Max(0f, roomSizePadding.x), Mathf.Max(0f, roomSizePadding.y));
            roomSizeMultiplier = Mathf.Max(0.1f, roomSizeMultiplier);
            wallThickness = Mathf.Max(0.2f, wallThickness);
            roomCollisionLayer = Mathf.Clamp(roomCollisionLayer, 0, 31);
            exitActivationPadding = Mathf.Max(0f, exitActivationPadding);
            minExitDelay = Mathf.Max(0f, minExitDelay);
            maxGeo = Mathf.Max(minGeo, maxGeo);

            EnsureExitReturnPoint();
        }

        private void Update()
        {
            if (_entered && exitByDistanceFallback)
                TryExitByDistance();

            if (!activateByDistance || _entered)
                return;

            TryEnterByDistanceFromEdge();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_entered) return;
            if (Time.time < _nextEnterAllowedAt) return;
            if (!IsPlayer(other, out var root)) return;
            TryEnter(root, other);
        }

        private void TryEnter(PlayerRoot playerRoot, Collider2D playerCollider)
        {
            if (_entered) return;
            if (Time.time < _nextEnterAllowedAt) return;
            _entered = true;
            _enteredAtTime = Time.time;

            if (playerRoot != null)
            {
                _entryPos = playerRoot.transform.position;
                _hasEntryPos = true;
            }

            DisableNearby(playerCollider);
            EnsureRoom();
            EnsureReward();
            TeleportToRoom(playerRoot);
        }

        private void TryEnterByDistanceFromEdge()
        {
            PlayerRoot player = FindPlayer();
            if (player == null) return;

            Collider2D zone = GetComponent<Collider2D>();
            if (zone == null) return;

            Collider2D playerCol = player.GetComponent<Collider2D>() ?? player.GetComponentInChildren<Collider2D>();
            if (playerCol == null || !playerCol.enabled) return;

            ColliderDistance2D dist = zone.Distance(playerCol);
            if (!dist.isValid) return;
            if (!dist.isOverlapped && dist.distance > activationDistance) return;

            TryEnter(player, playerCol);
        }

        public void ReturnPlayerToEntry(Collider2D playerCollider)
        {
            if (!_entered) return;

            PlayerRoot root = ResolvePlayerRoot(playerCollider);
            if (root == null)
                root = FindPlayer();
            if (root == null) return;

            RestoreNearby();
            Vector2 desired = ResolveExitPos(root);
            root.transform.position = desired;
            if (root.Rb != null)
            {
                root.Rb.velocity = Vector2.zero;
                root.Rb.angularVelocity = 0f;
            }

            _entered = false;
            _nextEnterAllowedAt = Time.time + reenterBlockDuration;
            ResetRoomRuntimeState();
        }

        private PlayerRoot ResolvePlayerRoot(Collider2D fromCollider)
        {
            if (fromCollider == null) return null;

            PlayerRoot root = fromCollider.GetComponentInParent<PlayerRoot>();
            if (root != null) return root;

            if (fromCollider.attachedRigidbody != null)
            {
                root = fromCollider.attachedRigidbody.GetComponent<PlayerRoot>();
                if (root != null) return root;
                root = fromCollider.attachedRigidbody.GetComponentInParent<PlayerRoot>();
                if (root != null) return root;
            }

            return null;
        }

        private void DisableNearby(Collider2D playerCollider)
        {
            _disabledColliders.Clear();
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, disableNearbyRadius, _overlapBuffer);
            Collider2D zone = GetComponent<Collider2D>();

            for (int i = 0; i < count; i++)
            {
                Collider2D c = _overlapBuffer[i];
                _overlapBuffer[i] = null;
                if (c == null || !c.enabled || c.isTrigger) continue;
                if (c == playerCollider || c == zone) continue;
                if (c.transform.IsChildOf(transform)) continue;
                if (c.GetComponentInParent<PlayerRoot>() != null) continue;

                c.enabled = false;
                _disabledColliders.Add(c);
            }
        }

        private void RestoreNearby()
        {
            for (int i = 0; i < _disabledColliders.Count; i++)
            {
                if (_disabledColliders[i] != null)
                    _disabledColliders[i].enabled = true;
            }
            _disabledColliders.Clear();
        }

        private void EnsureRoom()
        {
            if (_roomRoot != null) return;

            ResolveRoomBounds(out Vector3 center, out Vector2 size);

            GameObject root = new GameObject("SecretRoom_Runtime");
            _roomRoot = root.transform;
            _roomRoot.position = center;
            root.layer = roomCollisionLayer;

            CreateWall("Floor", center + new Vector3(0f, -size.y * 0.5f, 0f), new Vector2(size.x, wallThickness));
            CreateWall("Ceiling", center + new Vector3(0f, size.y * 0.5f, 0f), new Vector2(size.x, wallThickness));
            CreateWall("LeftWall", center + new Vector3(-size.x * 0.5f, 0f, 0f), new Vector2(wallThickness, size.y));
            CreateWall("RightWall", center + new Vector3(size.x * 0.5f, 0f, 0f), new Vector2(wallThickness, size.y));

            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(_roomRoot, false);
            float floorY = -size.y * 0.5f;
            float safeSpawnY = floorY + wallThickness + 1.4f + Mathf.Max(0f, spawnYOffset);
            spawn.transform.position = center + new Vector3(0f, safeSpawnY, 0f);
            _spawnPoint = spawn.transform;

            CreateExitPortal(center + (Vector3)exitPortalOffset);
        }

        private void CreateWall(string name, Vector3 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_roomRoot, true);
            go.transform.position = pos;
            go.layer = roomCollisionLayer;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = size;
        }

        private void CreateExitPortal(Vector3 position)
        {
            GameObject go = new GameObject("ExitPortal");
            go.transform.SetParent(_roomRoot, false);
            go.transform.position = position;

            _exitPortalPos = position;
            _hasExitPortal = true;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = exitPortalSize;

            if (showExitPortalInGame)
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetSquareSprite();
                sr.color = new Color(1f, 0.15f, 1f, 0.65f);
                sr.sortingOrder = 210;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = exitPortalSize;
            }

            var exit = go.AddComponent<HiddenRoomExitPortal>();
            exit.owner = this;
        }

        private void EnsureReward()
        {
            if (_rewardSpawned) return;
            _rewardSpawned = true;

            Vector3 pos = _spawnPoint != null ? _spawnPoint.position + Vector3.up * 1.2f : transform.position;
            float total = consumableWeight + geoWeight;
            if (total <= 0f) return;

            bool spawnConsumable = Random.value * total < consumableWeight;
            if (spawnConsumable && TrySpawnConsumable(pos)) return;

            int geo = Random.Range(minGeo, maxGeo + 1);
            if (geo > 0) SpawnStaticGeo(pos, geo);
        }

        private bool TrySpawnConsumable(Vector3 pos)
        {
            if (consumablePrefabs == null || consumablePrefabs.Length == 0) return false;

            var valid = new List<GameObject>();
            for (int i = 0; i < consumablePrefabs.Length; i++)
                if (consumablePrefabs[i] != null) valid.Add(consumablePrefabs[i]);
            if (valid.Count == 0) return false;

            Instantiate(valid[Random.Range(0, valid.Count)], pos, Quaternion.identity);
            return true;
        }

        private void SpawnStaticGeo(Vector3 pos, int geoValue)
        {
            GameObject go = new GameObject("RoomGeoReward");
            go.transform.position = pos;
            go.layer = roomCollisionLayer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = new Color(1f, 0.85f, 0.25f, 1f);
            sr.sortingOrder = 200;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var pickup = go.AddComponent<HiddenRoomGeoPickup>();
            pickup.playerTag = playerTag;
            pickup.geoValue = Mathf.Max(1, geoValue);
        }

        private void TeleportToRoom(PlayerRoot root)
        {
            if (root == null || _spawnPoint == null) return;
            root.transform.position = _spawnPoint.position;
            if (root.Rb != null) root.Rb.velocity = Vector2.zero;
        }

        private void TryExitByDistance()
        {
            if (!_hasExitPortal) return;
            if (Time.time < _enteredAtTime + minExitDelay) return;

            PlayerRoot root = FindPlayer();
            if (root == null) return;

            Vector2 d = (Vector2)root.transform.position - (Vector2)_exitPortalPos;
            float ax = exitPortalSize.x * 0.5f + exitActivationPadding;
            float ay = exitPortalSize.y * 0.5f + exitActivationPadding;
            if (Mathf.Abs(d.x) > ax || Mathf.Abs(d.y) > ay) return;

            Collider2D playerCol = root.GetComponent<Collider2D>() ?? root.GetComponentInChildren<Collider2D>();
            ReturnPlayerToEntry(playerCol);
        }

        private Vector2 ResolveExitPos(PlayerRoot root)
        {
            // Always drop the player exactly at the configured exit return point.
            // No safe-position search, no entry-position fallback — whatever the
            // designer (or auto-generated point) says, that's where we land.
            if (exitReturnPoint != null) return exitReturnPoint.position;

            // Only if no return point exists at all do we fall back to the player's
            // entry position, and finally the player's current position.
            if (_hasEntryPos) return _entryPos;
            return root.transform.position;
        }

        private void ResetRoomRuntimeState()
        {
            if (_roomRoot != null)
                Destroy(_roomRoot.gameObject);

            _roomRoot = null;
            _spawnPoint = null;
            _hasExitPortal = false;
            _exitPortalPos = Vector3.zero;
            _enteredAtTime = 0f;
        }

        private PlayerRoot FindPlayer()
        {
            if (_cachedPlayer != null) return _cachedPlayer;

            GameObject tagged = GameObject.FindWithTag(playerTag);
            if (tagged != null)
            {
                _cachedPlayer = tagged.GetComponent<PlayerRoot>() ?? tagged.GetComponentInChildren<PlayerRoot>();
                if (_cachedPlayer != null) return _cachedPlayer;
            }

            _cachedPlayer = FindFirstObjectByType<PlayerRoot>();
            return _cachedPlayer;
        }

        private bool IsPlayer(Collider2D other, out PlayerRoot root)
        {
            root = null;
            if (other == null)
                return false;
            if (other.CompareTag(playerTag))
            {
                root = other.GetComponentInParent<PlayerRoot>();
                if (root != null) _cachedPlayer = root;
                return true;
            }

            root = other.GetComponentInParent<PlayerRoot>();
            if (root == null)
                return false;

            _cachedPlayer = root;
            return true;
        }

        private void ResolveRoomBounds(out Vector3 center, out Vector2 size)
        {
            Vector2 fallback = new Vector2(Mathf.Max(4f, roomSize.x), Mathf.Max(3f, roomSize.y));
            center = transform.position;
            size = fallback;

            Collider2D zone = GetComponent<Collider2D>();
            if (zone != null)
            {
                center = zone.bounds.center;

                if (useTriggerSizeForRoom)
                {
                    if (zone is BoxCollider2D box)
                    {
                        Vector3 scale = box.transform.lossyScale;
                        Vector2 boxSize = new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
                        if (boxSize.x > 0.01f && boxSize.y > 0.01f)
                            size = boxSize;
                    }
                    else
                    {
                        Vector2 zoneSize = zone.bounds.size;
                        if (zoneSize.x > 0.01f && zoneSize.y > 0.01f)
                            size = zoneSize;
                    }
                }
            }

            center += (Vector3)roomOffset;
            size = Vector2.Scale(size, Vector2.one * roomSizeMultiplier) + roomSizePadding * 2f;
            size = new Vector2(Mathf.Max(2f, size.x), Mathf.Max(2f, size.y));
        }

        private void EnsureExitReturnPoint()
        {
            if (!autoCreateExitReturnPoint || exitReturnPoint != null)
                return;

            Transform t = transform.Find(AutoReturnPointName);
            if (t == null)
            {
                GameObject go = new GameObject(AutoReturnPointName);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(returnOffset.x, returnOffset.y, 0f);
                t = go.transform;
            }
            exitReturnPoint = t;
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _squareSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _squareSprite;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureExitReturnPoint();
        }

        private void OnDrawGizmosSelected()
        {
            ResolveRoomBounds(out Vector3 center, out Vector2 size);

            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.95f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0f));

            if (activateByDistance && activationDistance > 0f)
            {
                Vector2 expanded = size + Vector2.one * (activationDistance * 2f);
                Gizmos.color = new Color(0.3f, 0.95f, 1f, 0.4f);
                Gizmos.DrawWireCube(center, new Vector3(expanded.x, expanded.y, 0f));
            }

            float floorY = -size.y * 0.5f;
            float safeSpawnY = floorY + wallThickness + 1.4f + Mathf.Max(0f, spawnYOffset);
            Vector3 spawnPos = center + new Vector3(0f, safeSpawnY, 0f);
            Gizmos.color = new Color(0.2f, 1f, 0.35f, 1f);
            Gizmos.DrawSphere(spawnPos, 0.18f);

            Vector3 exitPos = center + (Vector3)exitPortalOffset;
            Gizmos.color = new Color(1f, 0.2f, 1f, 0.95f);
            Gizmos.DrawWireCube(exitPos, new Vector3(exitPortalSize.x, exitPortalSize.y, 0f));

            Vector3 returnPos = exitReturnPoint != null ? exitReturnPoint.position : transform.position + (Vector3)returnOffset;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.95f);
            Gizmos.DrawSphere(returnPos, 0.16f);
            Gizmos.DrawLine(returnPos + Vector3.left * 0.25f, returnPos + Vector3.right * 0.25f);
            Gizmos.DrawLine(returnPos + Vector3.up * 0.25f, returnPos + Vector3.down * 0.25f);
        }
#endif
    }

    internal sealed class HiddenRoomExitPortal : MonoBehaviour
    {
        public HiddenWallReveal owner;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null) owner.ReturnPlayerToEntry(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (owner != null) owner.ReturnPlayerToEntry(other);
        }
    }

    internal sealed class HiddenRoomGeoPickup : MonoBehaviour
    {
        public string playerTag = "Player";
        public int geoValue = 1;
        private bool _collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other == null) return;

            PlayerRoot playerRoot = other.GetComponentInParent<PlayerRoot>();
            if (playerRoot == null && !other.CompareTag(playerTag)) return;

            GameObject rootGo = playerRoot != null ? playerRoot.gameObject : other.gameObject;
            PlayerCurrency currency = rootGo.GetComponent<PlayerCurrency>() ?? rootGo.GetComponentInChildren<PlayerCurrency>();
            if (currency == null) return;

            _collected = true;
            currency.Add(Mathf.Max(1, geoValue));
            Object.Destroy(gameObject);
        }
    }
}
