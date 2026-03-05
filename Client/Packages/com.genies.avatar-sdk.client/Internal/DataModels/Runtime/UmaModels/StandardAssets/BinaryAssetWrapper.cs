using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMA
{
    /// <summary>
    /// Simple wrapper ScriptableObject with Binary serialization set that other objects (like Mesh) can be added to to force binary serialization.
    /// See AssetDatabase.AddObjectToAsset()
    /// </summary>
    [System.Serializable]
    [PreferBinarySerialization]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BinaryAssetWrapper : ScriptableObject
#else
    public class BinaryAssetWrapper : ScriptableObject
#endif
    {
    }
}
