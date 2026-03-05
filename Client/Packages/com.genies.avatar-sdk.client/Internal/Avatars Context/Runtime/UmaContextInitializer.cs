using System;
using Cysharp.Threading.Tasks;
using Genies.Utilities.Internal;
using UMA;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Initializes the UMA context through the given prefab.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UmaContextInitializer : Initializer
#else
    public sealed class UmaContextInitializer : Initializer
#endif
    {
        [SerializeField]
        private UMAContextBase umaContextPrefab;

        protected override UniTask InitializeAsync()
        {
            if (UMAContextBase.Instance is not null)
            {
                return UniTask.CompletedTask;
            }

            // if no UMA context is available then we must instantiate the given prefab
            if (umaContextPrefab)
            {
                Instantiate(umaContextPrefab);
            }
            else
            {
                throw new Exception($"[{nameof(UmaContextInitializer)}] no {nameof(umaContextPrefab)} was provided");
            }

            return UniTask.CompletedTask;
        }
    }
}
