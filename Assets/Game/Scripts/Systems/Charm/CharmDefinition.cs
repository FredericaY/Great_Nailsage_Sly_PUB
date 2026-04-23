using UnityEngine;

namespace Game.Systems.Charm
{
    [CreateAssetMenu(fileName = "CharmDefinition", menuName = "Game/Player/Charm Definition")]
    public class CharmDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string charmId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Stat Modifiers")]
        [SerializeField, Min(0.1f)] private float attackCooldownMultiplier = 1f;
        [SerializeField, Min(0f)] private float attackDamageMultiplier = 1f;
        [SerializeField, Min(0f)] private float moveSpeedMultiplier = 1f;

        [Header("Granted Abilities")]
        [SerializeField] private CharmAbility grantedAbilities = CharmAbility.None;

        [Header("Shop")]
        [Tooltip("Geo cost when sold at a charm vendor. 0 = not sold (pickup-only) unless vendor overrides.")]
        [SerializeField, Min(0)] private int shopGeoPrice;

        public string CharmId => charmId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float AttackCooldownMultiplier => Mathf.Max(0.1f, attackCooldownMultiplier);
        public float AttackDamageMultiplier => Mathf.Max(0f, attackDamageMultiplier);
        public float MoveSpeedMultiplier => Mathf.Max(0f, moveSpeedMultiplier);
        public CharmAbility GrantedAbilities => grantedAbilities;
        public int ShopGeoPrice => Mathf.Max(0, shopGeoPrice);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(charmId))
                charmId = name;

            attackCooldownMultiplier = Mathf.Max(0.1f, attackCooldownMultiplier);
            attackDamageMultiplier = Mathf.Max(0f, attackDamageMultiplier);
            moveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier);
            shopGeoPrice = Mathf.Max(0, shopGeoPrice);
        }
#endif
    }
}
