using System;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ItemPickerCellData
#else
    public class ItemPickerCellData
#endif
    {
        public Action OnClicked { get; set; }
    }
}