using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GenericItemPickerCellView : ItemPickerCellView
#else
    public class GenericItemPickerCellView : ItemPickerCellView
#endif
    {
        public Image thumbnail;

        [SerializeField]
        private GameObject _editableView;

        private bool _isEditable;

        public void SetIsEditable(bool isEditable)
        {
            _isEditable = isEditable;

            if (_selectedView.activeSelf)
            {
                _editableView.SetActive(isEditable);
            }
        }

        protected override void OnSelectionChanged(bool isSelected)
        {
            _editableView.SetActive(_isEditable && isSelected);
        }

        protected override void OnInitialize()
        {
        }

        protected override void OnDispose()
        {
            _editableView.SetActive(false);
        }
    }
}
