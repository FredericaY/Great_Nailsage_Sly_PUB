using Game.Combat;
using Game.Core.Input;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent]
    public class PlayerConsumables : MonoBehaviour
    {
        [Header("Quick Heal")]
        [SerializeField] private int startingQuickHealCharges = 0;
        [SerializeField] private int quickHealAmount = 3;

        [Header("References")]
        [SerializeField] private HeartsHealth heartsHealth;
        [SerializeField] private GameInputRouter inputRouter;

        public int QuickHealCharges => _quickHealCharges;

        public event System.Action<int> OnQuickHealChargesChanged;
        public event System.Action<string> OnConsumableUseFeedback;

        private int _quickHealCharges;

        private void Awake()
        {
            if (heartsHealth == null)
                heartsHealth = GetComponent<HeartsHealth>() ?? GetComponentInChildren<HeartsHealth>();
            if (inputRouter == null)
                inputRouter = FindFirstObjectByType<GameInputRouter>();

            _quickHealCharges = Mathf.Max(0, startingQuickHealCharges);
        }

        private void Update()
        {
            if (inputRouter == null)
                inputRouter = FindFirstObjectByType<GameInputRouter>();
            if (inputRouter == null || !inputRouter.UseConsumablePressedThisFrame)
                return;

            TryUseQuickHeal();
        }

        public void AddQuickHealCharges(int amount)
        {
            if (amount <= 0)
                return;

            _quickHealCharges += amount;
            OnQuickHealChargesChanged?.Invoke(_quickHealCharges);
        }

        public bool TryUseQuickHeal()
        {
            if (_quickHealCharges <= 0)
            {
                OnConsumableUseFeedback?.Invoke("No Quick Heal charges.");
                return false;
            }

            if (heartsHealth == null || heartsHealth.IsDead)
                return false;

            if (heartsHealth.Hearts >= heartsHealth.MaxHearts)
            {
                OnConsumableUseFeedback?.Invoke("Health is already full.");
                return false;
            }

            heartsHealth.Restore(quickHealAmount);
            _quickHealCharges = Mathf.Max(0, _quickHealCharges - 1);
            OnQuickHealChargesChanged?.Invoke(_quickHealCharges);
            OnConsumableUseFeedback?.Invoke($"Quick Heal used (-1). {quickHealAmount} hearts restored.");
            return true;
        }
    }
}
