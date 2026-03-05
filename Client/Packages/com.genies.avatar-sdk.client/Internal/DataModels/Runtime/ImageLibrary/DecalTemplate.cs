using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Meant for wearables (including generative) and can extend to hair and other “things”
    /// Decals are completely flat (1 albedo map always)
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DecalTemplate", menuName = "Genies/ImageLibrary/DecalTemplate")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DecalTemplate : LibraryAssetTemplate
#else
    public class DecalTemplate : LibraryAssetTemplate
#endif
    {
    }
}
