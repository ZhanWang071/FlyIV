using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.UI
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class PointerEventDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
#else
    public class PointerEventDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
#endif
    {
        public Action OnDragEnded;
        public Action OnDragStarted;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            OnDragStarted?.Invoke();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            OnDragEnded?.Invoke();
        }
    }
}
