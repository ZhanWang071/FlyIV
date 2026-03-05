using UnityEngine;

namespace Genies.UI.Transitions
{
    /// <summary>
    /// Interface for UI elements that can participate in transition animations.
    /// Defines the required components for objects that support slide, scale, and fade transitions.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ITransitionable
#else
    public interface ITransitionable
#endif
    {
        /// <summary>
        /// Gets the RectTransform component used for position and scale transitions.
        /// </summary>
        RectTransform RectTransform { get; }

        /// <summary>
        /// Gets the CanvasGroup component used for alpha/fade transitions.
        /// </summary>
        CanvasGroup CanvasGroup { get; }
    }
}
