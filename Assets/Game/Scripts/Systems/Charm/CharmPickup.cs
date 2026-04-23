using Game.Player;
using UnityEngine;

namespace Game.Systems.Charm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class CharmPickup : MonoBehaviour
    {
        [Header("Charm")]
        [SerializeField] private CharmDefinition charm;

        [Header("Target")]
        [SerializeField] private string playerTag = "Player";

        private bool _collected;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision.collider);
        }

        private void TryCollect(Collider2D other)
        {
            if (_collected || charm == null || other == null)
                return;

            PlayerCharmInventory inventory = ResolveInventory(other);
            if (inventory == null)
                return;

            // Auto-equip on pickup so ability-granting charms (e.g. DoubleJump) work immediately.
            inventory.AddCharm(charm, autoEquipIfEmpty: true);
            inventory.EquipCharm(charm);

            _collected = true;
            Destroy(gameObject);
        }

        private PlayerCharmInventory ResolveInventory(Collider2D other)
        {
            if (other == null)
                return null;

            PlayerCharmInventory inventory = other.GetComponent<PlayerCharmInventory>() ?? other.GetComponentInParent<PlayerCharmInventory>();
            if (inventory != null)
                return inventory;

            PlayerRoot playerRoot = other.GetComponent<PlayerRoot>() ?? other.GetComponentInParent<PlayerRoot>();
            if (playerRoot != null)
                return playerRoot.CharmInventory;

            if (!other.CompareTag(playerTag) && !other.transform.root.CompareTag(playerTag))
                return null;

            GameObject player = GameObject.FindWithTag(playerTag);
            if (player == null)
                return null;

            return player.GetComponent<PlayerCharmInventory>() ?? player.GetComponentInChildren<PlayerCharmInventory>();
        }
    }
}
