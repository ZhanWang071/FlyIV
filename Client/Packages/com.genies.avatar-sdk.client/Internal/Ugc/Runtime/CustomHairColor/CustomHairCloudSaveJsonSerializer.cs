using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc.CustomHair
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CustomHairCloudSaveJsonSerializer : ICloudSaveJsonSerializer<CustomHairColorData>
#else
    public class CustomHairCloudSaveJsonSerializer : ICloudSaveJsonSerializer<CustomHairColorData>
#endif
    {
        public string ToJson(CustomHairColorData data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public CustomHairColorData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CustomHairColorData>(json);
        }

        public bool IsValidJson(string json)
        {
            try
            {
                JsonConvert.DeserializeObject<Pattern>(json);
                return true;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                return false;
            }
        }
    }
}
