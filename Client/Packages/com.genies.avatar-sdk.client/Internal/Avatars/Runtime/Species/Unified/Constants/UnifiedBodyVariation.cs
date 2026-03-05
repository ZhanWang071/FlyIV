using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the body variations available for the unified species.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class UnifiedBodyVariation
#else
    public static class UnifiedBodyVariation
#endif
    {
        public const string Male = "male";
        public const string Female = "female";
        public const string Gap = "gap";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            Male,
            Female,
            Gap
        }.AsReadOnly();
    }
}