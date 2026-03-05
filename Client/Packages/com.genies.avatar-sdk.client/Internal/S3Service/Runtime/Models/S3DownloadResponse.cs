namespace Genies.S3Service.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct S3DownloadResponse
#else
    public struct S3DownloadResponse
#endif
    {
        public bool wasDownloaded;
        public string downloadedFilePath;

        public S3DownloadResponse(bool wasDownloaded, string downloadedFilePath)
        {
            this.wasDownloaded = wasDownloaded;
            this.downloadedFilePath = downloadedFilePath;
        }
    }
}
