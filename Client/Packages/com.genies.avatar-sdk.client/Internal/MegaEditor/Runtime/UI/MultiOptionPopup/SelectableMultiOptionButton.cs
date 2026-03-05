using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.MultiOptionPopup.Scripts
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SelectableMultiOptionButton : MultiOptionButton
#else
    public class SelectableMultiOptionButton : MultiOptionButton
#endif
    {
        [SerializeField] private Image _selectedIcon;
        [SerializeField] private Image _unselectedIcon;

        public void SetSelected(bool selected)
        {
            _selectedIcon.gameObject.SetActive(selected);
            _unselectedIcon.gameObject.SetActive(!selected);
        }

        public void SetColorButton(Color color)
        {
            text.color = color;
            if (icon != null)
            {
                icon.color = color;
            }
        }
    }
}
