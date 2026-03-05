using System;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LongPressCellView : ItemPickerCellView
#else
    public class LongPressCellView : ItemPickerCellView
#endif
    {
        public Action<LongPressCellView> OnLongPress;

        public Image thumbnail;

        [SerializeField]
        private GameObject editableView;

        private bool _showEditableIcon;

        protected override void OnInitialize()
        {
            _button.EnableLongPress = true;
            _button.OnLongPress.AddListener(InvokeLongPress);
        }

        private void InvokeLongPress()
        {
            OnLongPress?.Invoke(this);
        }

        public void SetShowEditableIcon(bool isEditable)
        {
            _showEditableIcon = isEditable;

            if (_selectedView.activeSelf)
            {
                editableView.SetActive(isEditable);
            }
        }

        protected override void OnSelectionChanged(bool isSelected)
        {
            editableView.SetActive(_showEditableIcon && isSelected);
        }

        protected override void OnDispose()
        {
            editableView.SetActive(false);
            _button.OnLongPress.RemoveListener(InvokeLongPress);
        }
    }
}
