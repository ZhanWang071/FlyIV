using System;

namespace Genies.Models {
    /// <summary>
    /// Meant to be the enum to have subcategories
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ThingsCategory {
#else
    public enum ThingsCategory {
#endif
        none,
        gnrlthing
    }
}
