using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TextureMap
#else
    public class TextureMap
#endif
    {
        [FormerlySerializedAs("type")] public TextureMapType Type;
        [FormerlySerializedAs("texture")] public Texture2D Texture;
    }
}
