using Genies.Utilities;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAnimationFeature
#else
    public interface IAnimationFeature
#endif
    {
        bool SupportsParameters(AnimatorParameters parameters);
        GenieComponent CreateFeatureComponent(AnimatorParameters parameters);

        bool TrySerialize(out JToken token)
            => SerializerAs<IAnimationFeature>.TrySerialize(this, out token);
    }
}