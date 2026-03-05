using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class IconContainer : OrderedScriptableObject
#else
    public class IconContainer : OrderedScriptableObject
#endif
    {
        public Texture2D icon;

        public Texture2D _256;
        public Texture2D _512;
        public Texture2D _1024;
        public string assetId;
    }
}