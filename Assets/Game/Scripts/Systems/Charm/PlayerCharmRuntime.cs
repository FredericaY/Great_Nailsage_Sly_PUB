using UnityEngine;

namespace Game.Systems.Charm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCharmInventory))]
    public class PlayerCharmRuntime : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCharmInventory inventory;

        public PlayerCharmInventory Inventory => inventory;

        private void Reset()
        {
            if (inventory == null)
                inventory = GetComponent<PlayerCharmInventory>();
        }

        private void Awake()
        {
            if (inventory == null)
                inventory = GetComponent<PlayerCharmInventory>();
        }

        public CharmDefinition GetEquippedCharm()
        {
            return inventory != null ? inventory.EquippedCharm : null;
        }

        public bool HasCharm(CharmDefinition charm)
        {
            return inventory != null && inventory.HasCharm(charm);
        }

        public bool HasAbility(CharmAbility ability)
        {
            CharmDefinition equipped = GetEquippedCharm();
            if (equipped == null)
                return false;

            return (equipped.GrantedAbilities & ability) == ability;
        }

        public bool HasDoubleJumpAbility()
        {
            return HasAbility(CharmAbility.DoubleJump);
        }

        public bool HasDashAbility()
        {
            return HasAbility(CharmAbility.Dash);
        }

        public bool HasGeoMagnetAbility()
        {
            return HasAbility(CharmAbility.GeoMagnet);
        }

        public bool HasQuickHealAbility()
        {
            return HasAbility(CharmAbility.QuickHeal);
        }

        public float GetMoveSpeedMultiplier()
        {
            CharmDefinition equipped = GetEquippedCharm();
            return equipped != null ? equipped.MoveSpeedMultiplier : 1f;
        }

        public float GetAttackDamageMultiplier()
        {
            CharmDefinition equipped = GetEquippedCharm();
            return equipped != null ? equipped.AttackDamageMultiplier : 1f;
        }

        public float GetAttackCooldownMultiplier()
        {
            CharmDefinition equipped = GetEquippedCharm();
            return equipped != null ? equipped.AttackCooldownMultiplier : 1f;
        }
    }
}
