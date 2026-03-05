using System;
using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Ugc
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SkinColorData
#else
    public class SkinColorData
#endif
    {
        public string Id;
        [JsonProperty("BaseColor")]
        [JsonConverter(typeof(FlexibleColorConverter))]
        public Color BaseColor;
    }
}
