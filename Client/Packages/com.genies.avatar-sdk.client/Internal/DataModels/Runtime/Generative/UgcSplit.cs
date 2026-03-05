using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UgcSplit
#else
    public class UgcSplit
#endif
    {
        [SerializeField] private string id;
        [SerializeField] private UgcElement[] elements;

        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public UgcElement[] Elements
        {
            get => elements;
            set => elements = value;
        }
    }
}