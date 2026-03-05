using Genies.CloudSave;
using Genies.Avatars;
using Newtonsoft.Json;

namespace Genies.Avatars.Services.Flair
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FlairColorPresetCloudSaveJsonSerializer : ICloudSaveJsonSerializer<FlairColorPreset>
#else
    public class FlairColorPresetCloudSaveJsonSerializer : ICloudSaveJsonSerializer<FlairColorPreset>
#endif
    {
        public string ToJson(FlairColorPreset data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public FlairColorPreset FromJson(string json)
        {
            return JsonConvert.DeserializeObject<FlairColorPreset>(json);
        }

        public bool IsValidJson(string json)
        {
            return true;
        }
    }
}
