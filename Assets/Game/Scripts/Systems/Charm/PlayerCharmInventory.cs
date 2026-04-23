using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Charm
{
    [DisallowMultipleComponent]
    public class PlayerCharmInventory : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private List<CharmDefinition> startingOwnedCharms = new();
        [SerializeField] private CharmDefinition startingEquippedCharm;
        [SerializeField] private List<CharmDefinition> ownedCharms = new();

        public IReadOnlyList<CharmDefinition> OwnedCharms => ownedCharms;
        public CharmDefinition EquippedCharm { get; private set; }

        public event Action OnInventoryChanged;
        public event Action<CharmDefinition> OnCharmAdded;
        public event Action<CharmDefinition> OnEquippedCharmChanged;

        private void Awake()
        {
            InitializeFromSerializedState();
        }

        public bool HasCharm(CharmDefinition charm)
        {
            return charm != null && ownedCharms.Contains(charm);
        }

        public bool AddCharm(CharmDefinition charm, bool autoEquipIfEmpty = false)
        {
            if (charm == null || ownedCharms.Contains(charm))
                return false;

            ownedCharms.Add(charm);
            if (autoEquipIfEmpty && EquippedCharm == null)
            {
                EquippedCharm = charm;
                OnEquippedCharmChanged?.Invoke(EquippedCharm);
            }

            OnCharmAdded?.Invoke(charm);
            OnInventoryChanged?.Invoke();

            return true;
        }

        public bool EquipCharm(CharmDefinition charm)
        {
            if (charm == null || !ownedCharms.Contains(charm))
                return false;

            if (EquippedCharm == charm)
                return true;

            EquippedCharm = charm;
            OnEquippedCharmChanged?.Invoke(EquippedCharm);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void UnequipCharm()
        {
            if (EquippedCharm == null)
                return;

            EquippedCharm = null;
            OnEquippedCharmChanged?.Invoke(null);
            OnInventoryChanged?.Invoke();
        }

        public bool RemoveCharm(CharmDefinition charm)
        {
            if (charm == null || !ownedCharms.Contains(charm))
                return false;

            bool wasEquipped = EquippedCharm == charm;
            ownedCharms.Remove(charm);

            if (wasEquipped)
            {
                EquippedCharm = null;
                OnEquippedCharmChanged?.Invoke(null);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        private void InitializeFromSerializedState()
        {
            if (ownedCharms == null)
                ownedCharms = new List<CharmDefinition>();

            for (int i = 0; i < startingOwnedCharms.Count; i++)
            {
                CharmDefinition charm = startingOwnedCharms[i];
                if (charm != null && !ownedCharms.Contains(charm))
                    ownedCharms.Add(charm);
            }

            EquippedCharm = null;
            if (startingEquippedCharm != null && ownedCharms.Contains(startingEquippedCharm))
                EquippedCharm = startingEquippedCharm;
        }
    }
}
