using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Helper class for short-lived, one-shot UI animations (spawn pop, click squish, etc.).
    /// Idle / looping animations are handled by <see cref="UICell.Update"/> using sine waves.
    /// </summary>
    public static class UIAnimationUtils
    {
        public static async UniTask ScaleTo(
            Transform transform,
            Vector3 target,
            float time,
            CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (time <= 0f)
            {
                transform.localScale = target;
                return;
            }

            float t = 0f;
            var start = transform.localScale;

            while (t < time && !token.IsCancellationRequested)
            {
                float normalized = Mathf.Clamp01(t / time);
                float eased = Mathf.SmoothStep(0f, 1f, normalized);

                transform.localScale = Vector3.Lerp(start, target, eased);

                t += Time.deltaTime;

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!token.IsCancellationRequested)
            {
                transform.localScale = target;
            }
        }
    }
}
