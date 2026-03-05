using UnityEngine;

namespace Genies.UIFramework
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ScriptablePopupConfig", menuName = "Genies/PopupConfig")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ScriptablePopupConfig : ScriptableObject
#else
    public class ScriptablePopupConfig : ScriptableObject
#endif
    {
        [SerializeField] private PopupType _popupType;

        [SerializeField] private PopupConfig _popupConfig;

        /// <summary>
        /// This ScriptablePopupConfig's popupType
        /// </summary>
        public PopupType PopupType
        {
            get
            {
                return _popupType;
            }
        }

        /// <summary>
        /// This ScriptablePopupConfig's popupConfig
        /// </summary>
        public PopupConfig PopupConfig
        {
            get
            {
                return _popupConfig;
            }
        }
    }
}
