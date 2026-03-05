using TMPro;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StyleItemPickerCellView : GenericItemPickerCellView
#else
    public class StyleItemPickerCellView : GenericItemPickerCellView
#endif
    {
        [SerializeField] private TMP_Text _label;

        public void SetLabel(string label)
        {
            _label.text = label;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _label.text = "";
        }
    }
}
