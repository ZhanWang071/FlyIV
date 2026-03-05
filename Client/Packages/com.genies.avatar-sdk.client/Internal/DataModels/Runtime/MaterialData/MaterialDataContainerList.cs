using System.Collections.Generic;
using Genies.Models;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataContainerList", menuName = "Genies/MaterialDataContainerList")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MaterialDataContainerList : ScriptableObject {
#else
    public class MaterialDataContainerList : ScriptableObject {
#endif
        public List<MaterialDataContainer> MaterialContainers;
    }
}
