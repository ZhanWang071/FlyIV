using System;
using System.Collections.Generic;

namespace Genies.Naf
{
    /// <summary>
    /// Represents a request to load an asset from the native Container API.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct ContainerAssetRequest
#else
    public struct ContainerAssetRequest
#endif
    {
        /// <summary>
        /// The unique identifier of the asset to load.
        /// </summary>
        public string assetId;

        /// <summary>
        /// Optional parameters for the asset load operation.
        /// </summary>
        public Dictionary<string, string> parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAssetRequest"/> struct.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to load.</param>
        /// <param name="parameters">Optional parameters for the asset load operation.</param>
        public ContainerAssetRequest(string assetId, Dictionary<string, string> parameters = null)
        {
            this.assetId    = assetId;
            this.parameters = parameters;
        }
    }
}
