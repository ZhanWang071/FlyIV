using System;
using Newtonsoft.Json;

namespace Genies.Inventory
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PipelineVersion
#else
    public class PipelineVersion
#endif
    {
        [JsonConverter(typeof(DefaultValueConverter))]
        public int min;

        [JsonConverter(typeof(DefaultValueConverter))]
        public int max;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultValueConverter : JsonConverter<int>
#else
    public class DefaultValueConverter : JsonConverter<int>
#endif
    {
        public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                return Convert.ToInt32(reader.Value);
            }
            catch
            {
                // Return default value (-1) if deserialization fails
                return -1;
            }
        }

        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
