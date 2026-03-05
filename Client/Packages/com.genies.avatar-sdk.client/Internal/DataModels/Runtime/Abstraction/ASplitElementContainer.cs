using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ASplitElementContainer : OrderedScriptableObject
#else
    public class ASplitElementContainer : OrderedScriptableObject
#endif
    {
        [SerializeField] private string assetId;
        [SerializeField] private string assetName;

        public string AssetId
        {
            get => assetId;
            set
            {
                assetName = value.Split('_')[1];
                assetId = value;
            }
        }
        
        public string AssetName
        {
            get => assetName;
            set => assetName = value;
        }
    }
}