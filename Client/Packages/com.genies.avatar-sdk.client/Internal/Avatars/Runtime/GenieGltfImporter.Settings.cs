using System;
namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static partial class GenieGltfImporter
#else
    public static partial class GenieGltfImporter
#endif
    {
        [Serializable]
        public sealed class Settings
        {
            public bool multithreadedImport        = true;
            public bool keepCPUCopyOfMeshes        = false;
            public bool keepCPUCopyOfTextures      = false;
            public bool generateMipMapsForTextures = true;
        }
    }
}
