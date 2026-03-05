using System;

namespace Genies.UI.Widgets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface INavBar
#else
    public interface INavBar
#endif
    {
        bool IsShown { get; }
        void Show();
        void Hide();
        void HideWithDuration(Action onComplete = null);
        void HideImmediately();
    }
}
