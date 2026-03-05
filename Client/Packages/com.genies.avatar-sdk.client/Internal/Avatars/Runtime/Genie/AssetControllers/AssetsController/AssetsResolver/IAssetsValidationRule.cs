using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// A rule that validates a set of assets.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAssetsValidationRule<TAsset>
#else
    public interface IAssetsValidationRule<TAsset>
#endif
        where TAsset : IAsset
    {
        void Apply(HashSet<TAsset> assets);
    }
}