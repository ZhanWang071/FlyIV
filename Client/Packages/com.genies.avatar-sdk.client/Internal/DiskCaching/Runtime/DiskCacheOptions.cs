namespace Genies.DiskCaching
{
    /// <summary>
    /// Different options for configuring cache on disk.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct DiskCacheOptions
#else
    public struct DiskCacheOptions
#endif
    {
        /// <summary>
        /// The time in seconds before cached files are considered expired.
        /// </summary>
        public int cacheExpirationInSeconds;

        /// <summary>
        /// The maximum size of the cache in bytes. If set to 0, no size limit is enforced.
        /// </summary>
        public long maxCacheSizeInBytes;

        /// <summary>
        /// Default configuration for the disk cache.
        /// </summary>
        public static DiskCacheOptions Default = new()
        {
            cacheExpirationInSeconds = 86400, // 1 day
            maxCacheSizeInBytes = 0,          // No size limit
        };
    }
}
