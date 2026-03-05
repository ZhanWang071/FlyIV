using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine;

namespace Genies.Components.ShaderlessTools
{
    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ShaderlessMaterials
#else
    public class ShaderlessMaterials
#endif
    {
        public List<MaterialProps> materials;

        public ShaderlessMaterials()
        {
            materials = new List<MaterialProps>();
        }
    }

    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct MaterialProps
#else
    public struct MaterialProps
#endif
    {
        public Material material;
        public string hash;
        public ShaderPropertiesData data;
    }
}
