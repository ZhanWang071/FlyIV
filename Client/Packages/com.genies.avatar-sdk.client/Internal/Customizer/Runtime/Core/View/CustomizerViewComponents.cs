using System;
using Genies.UI.Animations;
using UnityEngine;

namespace Genies.Customization.Framework
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CustomizerViewComponents
#else
    public class CustomizerViewComponents
#endif
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public float Height => rectTransform.sizeDelta.y;

        public void TerminateAnimations()
        {
            canvasGroup.Terminate();
            rectTransform.Terminate();
        }
    }
}
