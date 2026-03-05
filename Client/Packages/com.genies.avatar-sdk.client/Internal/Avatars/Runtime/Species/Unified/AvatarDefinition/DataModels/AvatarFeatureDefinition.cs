using System;
using Newtonsoft.Json;

namespace Genies.Avatars.Definition.DataModels
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class AvatarFeatureDefinition
#else
    public abstract class AvatarFeatureDefinition
#endif
    {
        [JsonProperty("JsonVersion", Required = Required.Always)]
        public string JsonVersion => GetDefinitionVersion();

        [JsonProperty("FeatureKey", Required = Required.Always)]
        public string FeatureKey => GetFeatureKey();
        
        protected abstract string GetFeatureKey();
        protected abstract string GetDefinitionVersion();
    }
}