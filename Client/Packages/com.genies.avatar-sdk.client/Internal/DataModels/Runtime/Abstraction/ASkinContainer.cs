using Genies.Components.ShaderlessTools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class ASkinContainer : OrderedScriptableObject, IShaderlessAsset
#else
    public abstract class ASkinContainer : OrderedScriptableObject, IShaderlessAsset
#endif
    {
        public string guid;
        public string assetAddress;
        public string assetName;

        // Dont't change FormerlySerializedAs attribute, All shaderless element containers where created with the old field.
        [SerializeField][FormerlySerializedAs("ShaderlessMaterials")]
        private ShaderlessMaterials shaderlessMaterials;

        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }
    }
}
