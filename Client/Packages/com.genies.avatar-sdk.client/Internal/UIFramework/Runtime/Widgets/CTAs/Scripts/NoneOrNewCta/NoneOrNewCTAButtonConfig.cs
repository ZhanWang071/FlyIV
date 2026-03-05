using System;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Config class for a given pair of None and Create New CTA buttons.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NoneOrNewCTAButtonConfig
#else
    public class NoneOrNewCTAButtonConfig
#endif
    {
        [SerializeField] private CTAButtonConfig _noneButton;
        [SerializeField] private CTAButtonConfig _createNewButton;
        [SerializeField] private HorizontalOrVerticalLayoutGroup _layoutGroup;

        [SerializeField] private bool _isVerticallyStacked = true;
        public CTAButtonConfig NoneButton => _noneButton;
        public CTAButtonConfig CreateNewButton => _createNewButton;
        public bool IsVerticallyStacked => _isVerticallyStacked;

        public HorizontalOrVerticalLayoutGroup LayoutGroup => _layoutGroup;
    }
}
