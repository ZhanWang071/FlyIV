using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UgcElement
#else
    public class UgcElement
#endif
    {
        [SerializeField] private string id;
        [SerializeField] private UgcSkin[] skins;
        
        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public UgcSkin[] Skins
        {
            get => skins;
            set => skins = value;
        }
    }
}