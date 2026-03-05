using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a group of <see cref="MeshAsset"/> instances that can be merged together in a single Unity submesh.
    /// It is assumed that the data is static and never changes.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IMeshGroupAsset
#else
    public interface IMeshGroupAsset
#endif
    {
        Material Material   { get; }
        int      AssetCount { get; }
        
        MeshAsset GetAsset   (int assetIndex);
        Vector2    GetUvOffset(int assetIndex);
        Vector2    GetUvScale (int assetIndex);
    }
}