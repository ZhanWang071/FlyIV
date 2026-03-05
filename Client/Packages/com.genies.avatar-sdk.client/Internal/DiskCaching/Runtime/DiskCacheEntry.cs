using System;

namespace Genies.DiskCaching
{
    /// <summary>
    /// A model for a cached file on disk.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct DiskCacheEntry
#else
    public struct DiskCacheEntry
#endif
    {
        public string s3DistributionUrl;
        public string filePath;
        public DateTime creationTimestamp;
        public string tag; // Tag to group or categorize related entries (ex. buildId)
    }
}
