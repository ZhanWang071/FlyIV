using System.Collections.Generic;
using UnityEngine;

namespace Genies.Services.DynamicConfigs
{
    /// <summary>
    /// A collection of sensitive data info of the dynamic configs
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DynamicConfigDataInfo", menuName = "GeniesParty/Dynamic Configs/DynamicConfigDataInfo")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicConfigDataInfo : ScriptableObject
#else
    public class DynamicConfigDataInfo : ScriptableObject
#endif
    {
        [SerializeField] private List<string> _data;
        public List<string> Data => _data;
    }
}
