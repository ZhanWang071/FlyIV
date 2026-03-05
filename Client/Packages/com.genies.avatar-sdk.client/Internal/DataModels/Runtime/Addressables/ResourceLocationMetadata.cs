using System;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ResourceLocationMetadata
#else
    public class ResourceLocationMetadata
#endif
    {
        public Type Type;
        public string Address;
        public string InternalId;
        public string BundleKey;
        public string RemoteUrl;
        public string[] Labels;
    }
}
