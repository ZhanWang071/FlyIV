using System.Collections.Generic;
using UnityEngine;
using Genies.Services.Model;

namespace Genies.Inventory
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultInventoryAsset
#else
    public class DefaultInventoryAsset
#endif
    {
        public string AssetId;
        public AssetType AssetType;
        public string Name;
        public string Category;
        public List<string> SubCategories;
        public int Order;

        public PipelineData PipelineData;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class ColoredInventoryAsset : DefaultInventoryAsset
#else
    public class ColoredInventoryAsset : DefaultInventoryAsset
#endif
    {
        public List<Color> Colors;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class ColorTaggedInventoryAsset : DefaultInventoryAsset
#else
    public class ColorTaggedInventoryAsset : DefaultInventoryAsset
#endif
    {
        public List<string> ColorTags;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultAvatarBaseAsset : DefaultInventoryAsset
#else
    public class DefaultAvatarBaseAsset : DefaultInventoryAsset
#endif
    {
        public List<string> Tags;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultAnimationLibraryAsset : DefaultInventoryAsset
#else
    public class DefaultAnimationLibraryAsset : DefaultInventoryAsset
#endif
    {
        public string MoodsTag;
        public List<string> ProtocolTags;
        public List<DefaultAnimationChildAsset> ChildAssets;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultAnimationChildAsset
#else
    public class DefaultAnimationChildAsset
#endif
    {
        public string AssetId;
        public string ProtocolTag;

        public DefaultAnimationChildAsset(Services.Model.ChildAsset childAsset)
        {
            AssetId = childAsset.Guid;
            ProtocolTag = childAsset.ProtocolTag;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal class PipelineData
#else
    public class PipelineData
#endif
    {
        public long AssetVersion;
        public string PipelineVersion;
        public string ParentId;
        public bool UniversalAvailable;
        public string UniversalBuildVersion;
        public string AssetAddress;

        // Parameterless constructor for JSON deserialization
        public PipelineData()
        {
        }

        public PipelineData(PipelineItemV2 pipelineItem)
        {
            AssetVersion = pipelineItem.AssetVersion.HasValue ? pipelineItem.AssetVersion.Value : 0;
            PipelineVersion = pipelineItem.PipelineVersion;
            ParentId = pipelineItem.ParentId;
            UniversalAvailable = pipelineItem.UniversalAvailable.HasValue ? pipelineItem.UniversalAvailable.Value : false;
            UniversalBuildVersion = pipelineItem.UniversalBuildVersion;
            AssetAddress = pipelineItem.AssetAddress;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AssetType
#else
    public enum AssetType
#endif
    {
        WardrobeGear,
        AvatarBase,
        AvatarMakeup,
        Flair,
        AvatarEyes,
        ColorPreset,
        ImageLibrary,
        AnimationLibrary,
        Avatar,
        Decor,
        ModelLibrary
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarBaseCategory
#else
    public enum AvatarBaseCategory
#endif
    {
        None = 0,
        Lips = 1,
        Jaw = 2,
        Nose = 3,
        Eyes = 4,
        Brow = 5
    }
}
