using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils
{
    public static class SpriteFlashUtil
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        
        private static readonly System.Collections.Generic.Dictionary<SpriteRenderer, MaterialPropertyBlock> Blocks
            = new System.Collections.Generic.Dictionary<SpriteRenderer, MaterialPropertyBlock>();

        public static Coroutine Flash(SpriteRenderer[] targets, float duration, Color flashColor, int times = 1)
        {
            if (targets == null || targets.Length == 0) return null;
            times = Mathf.Max(1, times);
            duration = Mathf.Max(0f, duration);

            var runner = CoroutineRunner.Instance;
            if (runner == null)
            {
               
                SetFlash(targets, 0f, flashColor);
                return null;
            }
            return runner.StartCoroutine(FlashRoutine(targets, duration, flashColor, times));
        }

        private static IEnumerator FlashRoutine(SpriteRenderer[] targets, float duration, Color flashColor, int times)
        {
            float onTime = duration;
            float offTime = duration;

            for (int i = 0; i < times; i++)
            {
                SetFlash(targets, 1f, flashColor);
                yield return new WaitForSeconds(onTime);

                SetFlash(targets, 0f, flashColor);
                yield return new WaitForSeconds(offTime);
            }

            SetFlash(targets, 0f, flashColor);
        }

        private static void SetFlash(SpriteRenderer[] targets, float flash, Color flashColor)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var sr = targets[i];
                if (sr == null) continue;

                if (!Blocks.TryGetValue(sr, out var block) || block == null)
                {
                    block = new MaterialPropertyBlock();
                    Blocks[sr] = block;
                }

                sr.GetPropertyBlock(block);
                block.SetFloat(FlashId, flash);
                block.SetColor(FlashColorId, flashColor);
                sr.SetPropertyBlock(block);
            }
        }
        
        public static Coroutine FlashHz(SpriteRenderer[] targets, float totalDuration, float hz, Color flashColor)
        {
            if (targets == null || targets.Length == 0) return null;
            totalDuration = Mathf.Max(0f, totalDuration);
            hz = Mathf.Max(1f, hz);

            var runner = CoroutineRunner.Instance;
            if (runner == null)
            {
                SetFlash(targets, 0f, flashColor);
                return null;
            }
            return runner.StartCoroutine(FlashHzRoutine(targets, totalDuration, hz, flashColor));
            
            
            
        }

        private static IEnumerator FlashHzRoutine(SpriteRenderer[] targets, float totalDuration, float hz, Color flashColor)
        {
            float end = Time.time + totalDuration;
            float interval = 1f / hz;

            bool on = true;
            while (Time.time < end)
            {
                on = !on;
                SetFlash(targets, on ? 1f : 0f, flashColor);
                yield return new WaitForSeconds(interval);
            }

            SetFlash(targets, 0f, flashColor);
        }

    }
}

