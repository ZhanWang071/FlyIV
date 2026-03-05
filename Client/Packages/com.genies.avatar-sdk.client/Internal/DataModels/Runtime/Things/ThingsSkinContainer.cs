using System;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Meant to house the multiple skins for Things
    /// Thingskin holds all the mapping and icon textures
    /// </summary>
    [Serializable]
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ThingsSkinContainer", menuName = "Genies/Things/ThingsSkinContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ThingsSkinContainer : ASkinContainer, IDynamicAsset
#else
    public class ThingsSkinContainer : ASkinContainer, IDynamicAsset
#endif
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        public bool IsEditable => editableRegionCount > 0 && editableRegionsMap;

        public GameObject prefab;
        public Texture2D editableRegionsMap;
        public int editableRegionCount;
    }
}
