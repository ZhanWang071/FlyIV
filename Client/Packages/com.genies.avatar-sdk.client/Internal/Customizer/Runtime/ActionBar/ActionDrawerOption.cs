using System;
using UnityEngine;

namespace Genies.Customization.Framework.Actions
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ActionDrawerOption
#else
    public class ActionDrawerOption
#endif
    {
        public string displayName;
        public Action onClick;
        public Func<bool> getOptionEnabled;
        public bool riskOption = false;
        public Sprite icon;
    }
}
