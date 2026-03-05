using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Genies.Assets.Services
{
#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Assets Service/Addressable Group Metadata", fileName = "AddressableGroupMetadata")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AddressableGroupMetadata : ScriptableObject
#else
    public class AddressableGroupMetadata : ScriptableObject
#endif
    {
        public string displayName;
        public AssetReference thumbnail;
        public List<AddressableGroupMetadata> children;
        public List<AddressableMetadata> assets;
    }
}
