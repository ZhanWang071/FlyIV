using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Base <see cref="ScriptableObject"/> class to create <see cref="IGenieComponentCreator"/> assets.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class GenieComponentAsset : ScriptableObject, IGenieComponentCreator
#else
    public abstract class GenieComponentAsset : ScriptableObject, IGenieComponentCreator
#endif
    {
        public abstract GenieComponent CreateComponent();
    }
}