using System.Collections.Generic;
using UnityEngine;

namespace Genies.Assets.Services
{
    /// <summary>
    /// <see cref="ScriptableObject"/> asset that declares a key map of builtin assets to use with a <see cref="BuiltinAssetsService"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BuiltinAssets", menuName = "Genies/Assets Service/Builtin Assets")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class BuiltinAssetsAsset : ScriptableObject
#else
    public sealed class BuiltinAssetsAsset : ScriptableObject
#endif
    {
        [SerializeField]
        private List<BuiltinAssets.Asset> assets;

        public List<BuiltinAssets.Asset> List => assets;
        public IBuiltinAssets BuiltinAssets => _assets ??= new BuiltinAssets(assets);

        private BuiltinAssets _assets;

        private void OnValidate()
        {
            _assets?.SetAssets(assets);
        }
    }
}
