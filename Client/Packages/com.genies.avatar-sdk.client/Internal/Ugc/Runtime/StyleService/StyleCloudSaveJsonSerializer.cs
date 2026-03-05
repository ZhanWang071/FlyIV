using System;
using Genies.CloudSave;
using Genies.CrashReporting;
using Newtonsoft.Json;

namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StyleCloudSaveJsonSerializer : ICloudSaveJsonSerializer<Style>
#else
    public class StyleCloudSaveJsonSerializer : ICloudSaveJsonSerializer<Style>
#endif
    {
        public string ToJson(Style data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public Style FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Style>(json);
        }

        public bool IsValidJson(string json)
        {
            try
            {
                JsonConvert.DeserializeObject<Style>(json);
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
