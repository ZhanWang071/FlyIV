using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct UgcSkin
#else
    public struct UgcSkin
#endif
    {
        [SerializeField] private string id;
        [SerializeField] private string guid;
        [SerializeField] private int subElements;

        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public string Guid
        {
            get => guid;
            set => guid = value;
        }
        
        public int SubElements
        {
            get => subElements;
            set => subElements = value;
        }
    }
}