using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BodyTypeItemPickerCellView : ItemPickerCellView
#else
    public class BodyTypeItemPickerCellView : ItemPickerCellView
#endif
    {
        public Image thumbnail;

        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
            thumbnail.color = Color.grey;
        }

        protected override void OnSelectionChanged(bool isSelected)
        {
            thumbnail.color = !isSelected ? Color.grey : Color.white;
        }
    }
}
