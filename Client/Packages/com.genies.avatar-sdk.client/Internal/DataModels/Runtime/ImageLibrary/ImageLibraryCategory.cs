namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ImageLibraryCategory
#else
    public enum ImageLibraryCategory
#endif
    {
        none,
        gnrldecal,
        distress,
        gnrltat,
        colorBurst,
        creator,
        floral,
        funky,
        graphic,
        grid,
        grunge,
        kaleid,
        lace,
        minimal,
        print,
        wavy,
    }
}