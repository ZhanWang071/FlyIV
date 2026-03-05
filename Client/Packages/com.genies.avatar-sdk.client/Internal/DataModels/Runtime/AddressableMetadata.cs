using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Genies.Assets.Services
{
#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Assets Service/Addressable Metadata", fileName = "AddressableMetadata")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AddressableMetadata : ScriptableObject
#else
    public class AddressableMetadata : ScriptableObject
#endif
    {
        public string displayName;
        public AssetReference thumbnail;
        public AssetReference asset;
    }
}
