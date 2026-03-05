using Genies.Customization.Framework.ItemPicker;
using UnityEngine;

namespace Genies.Looks.Customization.UI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class ItemPickerCircleCell : GenericItemPickerCellView
#else
    public sealed class ItemPickerCircleCell : GenericItemPickerCellView
#endif
    {
        [SerializeField]
        private RectTransform iconTransform;

        [SerializeField]
        private float borderWidth = 1.0f;

        protected override void OnSelectionChanged(bool isSelected)
        {
            base.OnSelectionChanged(isSelected);

            if (isSelected)
            {
                iconTransform.offsetMin = iconTransform.offsetMax = Vector2.zero;
            }
            else
            {
                iconTransform.offsetMin = new Vector2(borderWidth, borderWidth);
                iconTransform.offsetMax = new Vector2(-borderWidth, -borderWidth);
            }
        }
    }
}
