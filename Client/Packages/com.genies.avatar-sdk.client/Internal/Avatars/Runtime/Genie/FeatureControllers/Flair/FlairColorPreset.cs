using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FlairColorPreset
#else
    public class FlairColorPreset
#endif
    {
        public string FlairType { get; set;  } = null;
        public string Guid { get; set;  } = null;

        /// <summary>
        /// a guid without the prefix property (dependency for use gamefeature API)
        /// </summary>
        public string Id { get; set;  } = null;
        public Color[] Colors { get; set;  } = {Color.black, Color.black, Color.black, Color.black};
    }
}
