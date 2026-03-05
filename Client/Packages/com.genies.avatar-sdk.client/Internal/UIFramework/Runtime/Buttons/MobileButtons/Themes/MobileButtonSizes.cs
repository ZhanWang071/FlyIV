using UnityEngine;
using UnityEngine.UI;

namespace Genies.UIFramework
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MobileButtonSizes", menuName = "UIFramework/MobileButtonSizes")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MobileButtonSizes : ScriptableObject
#else
    public class MobileButtonSizes : ScriptableObject
#endif
    {
        public Vector2 XL;
        public Vector2 Large;
        public Vector2 Medium;
    }
}
