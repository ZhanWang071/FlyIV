namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum GalleryItemPickerPanelState
#else
    public enum GalleryItemPickerPanelState
#endif
    {
        Hidden,
        QuarterSize,
        HalfSize,
        FullSize
    }
}