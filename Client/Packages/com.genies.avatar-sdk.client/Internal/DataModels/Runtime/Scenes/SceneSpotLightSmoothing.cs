using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Settings to make the Spot Light adaptive to all animations to avoid over/underexposure.
    /// </summary>
    [System.Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SceneSpotLightSmoothing
#else
    public class SceneSpotLightSmoothing
#endif
    {
        /// <summary>
        /// Adjusts intensity to compensate light falloff and keeps consistent light values over any distance.
        /// </summary>
        public bool UseIntensitySmoothing;

        /// <summary>
        /// Adjusts light local Z-position to keep within min/max distance of light from the avatar.
        /// </summary>
        public bool UsePositionalSmoothing;

        /// <summary>
        /// Minimum distance from spot light to avatar (for positional smoothing).
        /// </summary>
        [Range(0, 10)]
        public float MinDistance;

        /// <summary>
        /// Maximum distance from spot light to avatar (for positional smoothing).
        /// </summary>
        [Range(1, 20)]
        public float MaxDistance;
    }
}
