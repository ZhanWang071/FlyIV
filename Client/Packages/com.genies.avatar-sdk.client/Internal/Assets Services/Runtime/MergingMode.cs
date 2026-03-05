/**
 * The IAssetsService is heavily inspired in the Addressables API, so we have load methods that allows to set the merge mode. But we want to create a
 * layer of abstraction between Addressables and IAssetsService, so I have created this enum outside of the Addressables namespace.
 */

namespace Genies.Assets.Services
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum MergingMode
#else
    public enum MergingMode
#endif
    {
        None = 0,
        UseFirst = 0,
        Union,
        Intersection
    }
}
