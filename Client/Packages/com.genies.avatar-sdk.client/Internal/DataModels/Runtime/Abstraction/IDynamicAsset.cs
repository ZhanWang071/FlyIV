namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IDynamicAsset
#else
    public interface IDynamicAsset
#endif
    {
        public int PipelineVersion { get; set; }
    }
}