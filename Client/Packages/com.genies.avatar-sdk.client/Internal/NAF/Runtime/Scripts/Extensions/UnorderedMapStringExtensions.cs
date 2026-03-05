using System.Collections.Generic;
using GnWrappers;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class UnorderedMapStringExtensions
#else
    public static class UnorderedMapStringExtensions
#endif
    {
        public static UnorderedMapString AsUnorderedMapString(this Dictionary<string, string> dictionary, bool nullIfNullOrEmpty = false)
        {
            if (dictionary is null || dictionary.Count == 0)
            {
                return nullIfNullOrEmpty ? null : new UnorderedMapString();
            }

            var map = new UnorderedMapString();
            foreach ((string key, string value) in dictionary)
            {
                map.Add(key, value);
            }

            return map;
        }
    }
}