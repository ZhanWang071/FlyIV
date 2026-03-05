using System;

namespace Genies.UI.Animations
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAnimation<T> {
#else
    public interface IAnimation<T> {
#endif
        bool IsRunning { get; }
        T AnimatedValue { get; }
        void Animate(T start, T end, float duration, Action callback = null);
        void Stop();
        void UpdateAnimation(float dt);
    }
}