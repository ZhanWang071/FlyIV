using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Genies.Components.ShaderlessTools
{
    [Serializable][Preserve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ShaderPropertiesData
#else
    public class ShaderPropertiesData
#endif
    {
        public string materialName;
        public string materialJson;
        public string shaderName;
        public string serializedFieldsVersion;
        public string serializedPropsVersion;

        public List<ShaderField> serializedFields;
        public List<ShaderProperty> serializedProperties;
    }
}
