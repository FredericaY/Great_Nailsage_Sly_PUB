using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Holds the player's geo (money). Enemies add to this when they die.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCurrency : MonoBehaviour
    {
        [Header("Starting Amount")]
        [SerializeField] private int startingGeo = 0;

        /// <summary>Current geo amount. Use Add() or Set() to modify.</summary>
        public int Geo => _geo;

        /// <summary>Fired when geo changes. (previousAmount, newAmount)</summary>
        public event System.Action<int, int> OnGeoChanged;

        private int _geo;

        private void Awake()
        {
            _geo = Mathf.Max(0, startingGeo);
        }

        /// <summary>Add geo. Use positive values.</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            int previous = _geo;
            _geo = Mathf.Max(0, _geo + amount);
            if (_geo != previous)
                OnGeoChanged?.Invoke(previous, _geo);
        }

        /// <summary>Set geo directly (e.g. for loading saved data).</summary>
        public void Set(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (_geo == amount) return;
            int previous = _geo;
            _geo = amount;
            OnGeoChanged?.Invoke(previous, _geo);
        }

        /// <summary>True if the player has at least this much geo.</summary>
        public bool CanAfford(int amount)
        {
            return amount >= 0 && _geo >= amount;
        }

        /// <summary>Spend geo if possible. Returns false if not enough.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (_geo < amount) return false;
            int previous = _geo;
            _geo -= amount;
            OnGeoChanged?.Invoke(previous, _geo);
            return true;
        }
    }
}
