namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AddressableTags
#else
    public enum AddressableTags
#endif
    {
        animation,
        basictemplate,
        decal,
        dolltemplate,
        elementcontainer,
        flair,
        gear,
        geartemplate,
        generativetemplate,
        looks,
        patch,
        pattern,
        scene,
        skin,
        shaders,
        tattoo,
        things,
        thingstemplate,
        thumbnail,
        highthumbnail,
        mediumthumbnail,
        ugctemplate,
        ugcwpattern,
        unified,
        wardrobe,
        subspecies,
        modellibrary,
    }
}