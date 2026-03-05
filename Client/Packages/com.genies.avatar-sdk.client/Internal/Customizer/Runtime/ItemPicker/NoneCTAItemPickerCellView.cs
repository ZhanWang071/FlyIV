
using Genies.Customization.Framework.ItemPicker;

namespace Genies.UI.Widgets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NoneCTAItemPickerCellView : GenericItemPickerCellView
#else
    public class NoneCTAItemPickerCellView : GenericItemPickerCellView
#endif
    {

    }
}
