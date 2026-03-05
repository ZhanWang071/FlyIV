using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TextureMapWrapper
#else
    public class TextureMapWrapper
#endif
    {
        [SerializeField] private string id;
        [SerializeField] private Texture2D texture;
        
        public string Id
        {
            get => id;
            set => id = value;
        }

        public Texture2D Texture
        {
            get => texture;
            set => texture = value;
        }
    }
}