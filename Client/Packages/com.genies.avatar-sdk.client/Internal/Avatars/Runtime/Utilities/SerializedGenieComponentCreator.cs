using Genies.Utilities;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IGenieComponentCreator"/> implementation that creates the component instances from a serialized
    /// component token.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class SerializedGenieComponentCreator : IGenieComponentCreator
#else
    public sealed class SerializedGenieComponentCreator : IGenieComponentCreator
#endif
    {
        private readonly JToken _token;

        public SerializedGenieComponentCreator(JToken token)
        {
            _token = token;
        }

        public GenieComponent CreateComponent()
        {
            return SerializerAs<GenieComponent>.TryDeserialize(_token, out GenieComponent component) ? component : null;
        }
    }
}