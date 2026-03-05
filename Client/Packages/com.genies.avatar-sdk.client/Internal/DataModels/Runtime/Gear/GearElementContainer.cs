using System.Collections.Generic;
using Genies.Components.ShaderlessTools;
using UMA;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GearElementContainer : OrderedScriptableObject, IShaderlessAsset, IDynamicAsset
#else
    public sealed class GearElementContainer : OrderedScriptableObject, IShaderlessAsset, IDynamicAsset
#endif
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        public string                     guid;
        public string                     skinName;
        public string                     slot;
        public string                     subcategory;
        public CollisionData              collisionData;
        public List<GearSubElementContainer> subElements = new();
        public List<MeshHideAsset>        meshHideAssets = new();
        public List<SlotDataAssetGroup>   slotGroups = new();
        public List<Object>               extras = new();

        [SerializeField] private ShaderlessMaterials shaderlessMaterials;

        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }
    }
}
