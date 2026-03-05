using System;
using Newtonsoft.Json;

namespace Genies.Naf.Content.AvatarBaseConfig
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class RootConfig
#else
    public class RootConfig
#endif
    {
        [JsonProperty("AvatarBase")]
        public AvatarBase avatarBase;
    }



    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarBase
#else
    public class AvatarBase
#endif
    {
        [JsonProperty("version")]
        public string version;
    }
}