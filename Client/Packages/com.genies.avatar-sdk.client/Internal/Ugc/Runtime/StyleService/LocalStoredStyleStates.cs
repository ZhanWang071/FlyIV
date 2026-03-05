using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Genies.Ugc
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LocalStoredStyleStates
#else
    public class LocalStoredStyleStates
#endif
    {
        [JsonProperty("styles")]
        public readonly Dictionary<string, string> Styles = new Dictionary<string, string>();
    }
}
