namespace Genies.UI.Scroller
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IScrollableAnimator
#else
    public interface IScrollableAnimator
#endif
    {
        void Animate(float normalizedValue);
    }
}
