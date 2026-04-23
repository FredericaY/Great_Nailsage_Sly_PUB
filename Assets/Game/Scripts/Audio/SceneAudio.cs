using UnityEngine;
using System.Collections.Generic;

namespace Game.Audio
{
    [DisallowMultipleComponent]
    public class SceneAudio : MonoBehaviour
    {
        [SerializeField] private SceneAudioProfile profile;
        [SerializeField] private bool applyOnEnable = true;

        private readonly List<EnemyEncounter> _subscribedEncounters = new();

        private void OnEnable()
        {
            if (!applyOnEnable) return;
            if (profile == null) return;

            AudioService.Ensure().ApplySceneProfile(profile);
            SubscribeToEncounters();
        }

        private void OnDisable()
        {
            UnsubscribeFromEncounters();
        }

        public void ApplyNow()
        {
            if (profile == null) return;
            AudioService.Ensure().ApplySceneProfile(profile);
        }

        public void PlayDefaultBgm()
        {
            if (profile == null || profile.DefaultBgm == null)
                return;

            AudioService.Ensure().SwitchBgm(
                profile.DefaultBgm,
                profile.DefaultBgmVolume,
                profile.DefaultBgmLoop,
                profile.BgmFadeOut,
                profile.DefaultBgmFadeIn);
        }

        public void PlayBossBgm()
        {
            if (profile == null || profile.BossBgm == null)
                return;

            AudioService.Ensure().SwitchBgm(
                profile.BossBgm,
                profile.BossBgmVolume,
                profile.BossBgmLoop,
                profile.BgmFadeOut,
                profile.BossBgmFadeIn);
        }

        private void SubscribeToEncounters()
        {
            UnsubscribeFromEncounters();

            EnemyEncounter[] encounters = FindObjectsOfType<EnemyEncounter>(true);
            if (encounters == null || encounters.Length == 0)
                return;

            for (int i = 0; i < encounters.Length; i++)
            {
                EnemyEncounter encounter = encounters[i];
                if (encounter == null)
                    continue;

                encounter.EncounterActivated += HandleEncounterActivated;
                encounter.BossDefeated += HandleBossDefeated;
                _subscribedEncounters.Add(encounter);
            }
        }

        private void UnsubscribeFromEncounters()
        {
            for (int i = 0; i < _subscribedEncounters.Count; i++)
            {
                EnemyEncounter encounter = _subscribedEncounters[i];
                if (encounter == null)
                    continue;

                encounter.EncounterActivated -= HandleEncounterActivated;
                encounter.BossDefeated -= HandleBossDefeated;
            }

            _subscribedEncounters.Clear();
        }

        private void HandleEncounterActivated(EnemyEncounter encounter)
        {
            if (encounter == null || encounter.Mode != EnemyEncounter.EncounterMode.BossSpawnOnPlayerEnter)
                return;

            PlayBossBgm();
        }

        private void HandleBossDefeated(EnemyEncounter encounter, EnemyRoot bossRoot)
        {
            PlayDefaultBgm();
        }
    }
}
