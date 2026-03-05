using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ITattooController
#else
    public interface ITattooController
#endif
    {
        IReadOnlyList<TattooSlotController> SlotControllers { get; }
        
        event Action Updated;
    }
}