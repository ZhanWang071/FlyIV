using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Genies.UIFramework
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class PopupBlocker : MonoBehaviour, IPointerDownHandler
#else
    public class PopupBlocker : MonoBehaviour, IPointerDownHandler
#endif
    {
        public event Action OnBlockerClicked;

        private bool _active = true;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_active)
            {
                return;
            }

            OnBlockerClicked?.Invoke();
        }

        /// <summary>
        /// Manage whether the user can click on the blocker or not.
        /// </summary>
        /// <param name="isActive"> If true, the blocker will receive input</param>
        public void SetBlockerActive(bool isActive)
        {
            _active = isActive;
        }
    }
}
