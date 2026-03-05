using System;
using UnityEngine;

namespace Genies.Telemetry
{
    
#if GENIES_SDK && !GENIES_INTERNAL
    [Serializable]
    internal class GeniesSdkVersionCache : ScriptableObject
#else
    [Serializable]
    public class GeniesSdkVersionCache : ScriptableObject
        
#endif
    {
        public string Version;
        public string PackageName;
        public string LastUpdatedUtc;
        public string Notes;
    }
}
