using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SpacesIdleContainer", menuName = "Genies/AnimationLibrary/SpacesIdleContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SpacesIdleContainer : AnimationContainer
#else
    public class SpacesIdleContainer : AnimationContainer
#endif
    {
        [SerializeField] private string[] protocols;
        [SerializeField] private List<ChildAsset> childAssets;

        public string[] ProtocolTags
        {
            get => protocols;
            set => protocols = value;
        }

        public List<ChildAsset> ChildAssets
        {
            get => childAssets ??= new List<ChildAsset>();
            set => childAssets = value;
        }
    }
}
