using Genies.Utilities;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
    /// <summary>
    /// Wraps any <see cref="AnimationFeatureAsset"/> and behaves as a reference, so it is automatically destroyed when
    /// no longer used to avoid memory leaks. Highly recommended to use when deserializing animation feature assets.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class AnimationFeatureAssetRef : IAnimationFeature
#else
    public sealed class AnimationFeatureAssetRef : IAnimationFeature
#endif
    {
        public readonly AnimationFeatureAsset Asset;
        
        private readonly UnityObjectRef _assetRef;
        
        public AnimationFeatureAssetRef(AnimationFeatureAsset asset, UnityObjectRef assetRef)
        {
            Asset = asset;
            _assetRef = assetRef;
        }

        public bool SupportsParameters(AnimatorParameters parameters)
            => Asset.SupportsParameters(parameters);
        public GenieComponent CreateFeatureComponent(AnimatorParameters parameters)
            => Asset.CreateFeatureComponent(parameters);
        bool IAnimationFeature.TrySerialize(out JToken token)
            => SerializerAs<IAnimationFeature>.TrySerialize(Asset, out token);
    }
}