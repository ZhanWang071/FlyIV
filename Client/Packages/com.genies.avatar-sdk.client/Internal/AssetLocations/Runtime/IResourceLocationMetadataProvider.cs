using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Models;

namespace Genies.AssetLocations
{
    /// <summary>
    /// Generic interface to generate ResourceLocationMetadata out of any metadata object,
    /// multiple CMS sources (baserow, geniesApi, airtable..) would implement this interface to generate asset locations
    /// </summary>
    /// <typeparam name="T"> Type of metadata to build locations out of </typeparam>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IResourceLocationMetadataProvider<in T>
#else
    public interface IResourceLocationMetadataProvider<in T>
#endif
    {
        public UniTask<List<ResourceLocationMetadata>> Provide(T metadata, string platform, string baseUrl, IEnumerable<string> lods, IEnumerable<string> iconSizes);
    }
}
