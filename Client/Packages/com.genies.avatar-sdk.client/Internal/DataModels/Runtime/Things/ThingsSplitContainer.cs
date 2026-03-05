using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for things splits.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ThingsSplitContainer", menuName = "Genies/Things/ThingsSplitContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ThingsSplitContainer : ASplitElementContainer
#else
    public class ThingsSplitContainer : ASplitElementContainer
#endif
    {

    }
}
