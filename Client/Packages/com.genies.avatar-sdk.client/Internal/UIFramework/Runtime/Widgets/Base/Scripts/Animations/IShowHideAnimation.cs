using UnityEngine;

namespace Genies.UI.Widgets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IShowHideAnimation
#else
    public interface IShowHideAnimation
#endif
    {
        RectTransform RectTransform { get; set; }
        void Show();
        void Hide();
    }
}
