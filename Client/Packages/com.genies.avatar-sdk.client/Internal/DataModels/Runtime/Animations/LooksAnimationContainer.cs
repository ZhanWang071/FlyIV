using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TODO DEPRECATE
namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for LooksAnimationContainer.
    /// It holds are the data that the thing will use in the addressable bundles
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LooksAnimationContainer", menuName = "Genies/Looks/LooksAnimationContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LooksAnimationContainer : OrderedScriptableObject
#else
    public class LooksAnimationContainer : OrderedScriptableObject
#endif
    {
        [SerializeField] private string assetId;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private Texture2D thumbnail;

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public AnimationClip Clip
        {
            get => clip;
            set => clip = value;
        }

        public Texture2D Thumbnail
        {
            get => thumbnail;
            set => thumbnail = value;
        }
    }
}
