using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CancellationToken = System.Threading.CancellationToken;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Naf.Content;
using GnWrappers;
using Genies.ServiceManagement;
using Genies.Ugc;
using Genies.Ugc.CustomHair;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Gender types for avatar body configuration
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum GenderType
#else
    public enum GenderType
#endif
    {
        Male,
        Female,
        Androgynous
    }

    /// <summary>
    /// Body size types for avatar body configuration
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BodySize
#else
    public enum BodySize
#endif
    {
        Skinny,
        Medium,
        Heavy
    }

    /// <summary>
    /// Wardrobe subcategory types for filtering wearable assets
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum WardrobeSubcategory
#else
    public enum WardrobeSubcategory
#endif
    {
        none,
        hair,
        eyebrows,
        eyelashes,
        facialHair,
        underwearTop,
        hoodie,
        shirt,
        jacket,
        dress,
        pants,
        shorts,
        skirt,
        underwearBottom,
        socks,
        shoes,
        bag,
        bracelet,
        earrings,
        glasses,
        hat,
        mask,
        watch,
        all
    }

    /// <summary>
    /// Body statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BodyStats
#else
    public enum BodyStats
#endif
    {
        NeckThickness,
        ShoulderBroadness,
        ChestBustline,
        ArmsThickness,
        WaistThickness,
        BellyFullness,
        HipsThickness,
        LegsThickness,
        HipSize
    }

    /// <summary>
    /// Eyebrow statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum EyeBrowsStats
#else
    public enum EyeBrowsStats
#endif
    {
        Thickness,          // Thickness of the eyebrows
        Length,             // Length of the eyebrows
        VerticalPosition,   // Vertical position of the eyebrows
        Spacing             // Spacing between eyebrows
    }

    /// <summary>
    /// Eye statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum EyeStats
#else
    public enum EyeStats
#endif
    {
        Size,               // Size of the eyes
        VerticalPosition,   // Vertical position of the eyes
        Spacing,            // Spacing between eyes
        Rotation,           // Rotation of the eyes
    }

    /// <summary>
    /// Jaw statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum JawStats
#else
    public enum JawStats
#endif
    {
        Width,   // Width of the jaw
        Length   // Length of the jaw
    }

    /// <summary>
    /// Lip statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum LipsStats
#else
    public enum LipsStats
#endif
    {
        Width,             // Width of the lips
        Fullness,          // Fullness/thickness of the lips
        VerticalPosition   // Vertical position of the lips
    }

    /// <summary>
    /// Nose statistics that can be modified on the avatar
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum NoseStats
#else
    public enum NoseStats
#endif
    {
        Width,             // Width of the nose
        Length,            // Length of the nose
        VerticalPosition,  // Vertical position of the nose
        Tilt,              // Tilt/angle of the nose
        Projection         // Projection/protrusion of the nose
    }

    /// <summary>
    /// Identifies which IAvatarFeatureStat struct type to use (Body, EyeBrows, Eyes, Jaw, Lips, Nose).
    /// Used to request all stat values for that category from GetAvatarFeatureStats.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarFeatureStatType
#else
    public enum AvatarFeatureStatType
#endif
    {
        All,
        Body,
        EyeBrows,
        Eyes,
        Jaw,
        Lips,
        Nose
    }

    /// <summary>
    /// Unified enum for all avatar feature stats. Used as keys for GetAvatarFeatureStats.
    /// Category-prefixed to avoid name clashes (e.g. Width exists in Jaw, Lips, Nose).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarFeatureStat
#else
    public enum AvatarFeatureStat
#endif
    {
        // Body (9)
        Body_NeckThickness,
        Body_ShoulderBroadness,
        Body_ChestBustline,
        Body_ArmsThickness,
        Body_WaistThickness,
        Body_BellyFullness,
        Body_HipsThickness,
        Body_LegsThickness,
        Body_HipSize,
        // EyeBrows (4)
        EyeBrows_Thickness,
        EyeBrows_Length,
        EyeBrows_VerticalPosition,
        EyeBrows_Spacing,
        // Eyes (4)
        Eyes_Size,
        Eyes_VerticalPosition,
        Eyes_Spacing,
        Eyes_Rotation,
        // Jaw (2)
        Jaw_Width,
        Jaw_Length,
        // Lips (3)
        Lips_Width,
        Lips_Fullness,
        Lips_VerticalPosition,
        // Nose (5)
        Nose_Width,
        Nose_Length,
        Nose_VerticalPosition,
        Nose_Tilt,
        Nose_Projection
    }

    /// <summary>
    /// Hair/flair type for color modification (hair, facial hair, eyebrows, or eyelashes).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum HairType
#else
    public enum HairType
#endif
    {
        Hair,        // Regular hair on the head
        FacialHair,  // Facial hair (beard, mustache, etc.)
        Eyebrows,    // Eyebrow hair
        Eyelashes    // Eyelash hair
    }

    /// <summary>
    /// Wearable category types (non-hair wardrobe subcategories). Excludes hair, eyebrows, eyelashes, facialHair.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum WearablesCategory
#else
    public enum WearablesCategory
#endif
    {
        Hoodie,
        Shirt,
        Dress,
        Pants,
        Shorts,
        Skirt,
        Shoes,
        Earrings,
        Glasses,
        Hat,
        Mask
    }

    /// <summary>
    /// Subset of WearablesCategory for user wearable asset APIs (e.g. GetUserWearablesByCategoryAsync).
    /// Maps to WearablesCategory via ToWearablesCategory when calling internal APIs.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum UserWearablesCategory
#else
    public enum UserWearablesCategory
#endif
    {
        Hoodie,
        Shirt,
        Pants,
        Shorts
    }

    /// <summary>
    /// Avatar feature category for GetDefaultAvatarFeaturesByCategory. Wrapper for Genies.Inventory.AvatarBaseCategory for callers that cannot reference Inventory.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarFeatureCategory
#else
    public enum AvatarFeatureCategory
#endif
    {
        None = 0,
        Lips = 1,
        Jaw = 2,
        Nose = 3,
        Eyes = 4,
        Brow = 5
    }

    /// <summary>
    /// Source type for color preset retrieval (All, User, or Default).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ColorSource
#else
    public enum ColorSource
#endif
    {
        All,      // Returns both user and default colors
        User,     // Returns only user-created custom colors
        Default   // Returns only default/preset colors
    }

    /// <summary>
    /// Asset type for wearable asset retrieval (All, User, or Default).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum RequestType
#else
    public enum RequestType
#endif
    {
        All,      // Returns both user and default wearable assets
        User,     // Returns only user-owned wearable assets
        Default   // Returns only default wearable assets
    }

    /// <summary>
    /// Type of color preset to retrieve.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ColorType
#else
    public enum ColorType
#endif
    {
        Eyes,           // Eye colors
        Hair,           // Regular hair colors
        FacialHair,     // Facial hair colors
        Skin,           // Skin colors
        Eyebrow,        // Eyebrow colors
        Eyelash,        // Eyelash colors
        MakeupStickers, // Stickers colors
        MakeupLipstick, // Lipstick colors
        MakeupFreckles, // Freckles colors
        MakeupFaceGems, // Facegems colors
        MakeupEyeshadow,// Eye shadow colors
        MakeupBlush     // Blush colors
    }

    /// <summary>
    /// Type of user color preset to retrieve.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum UserColorType
#else
    public enum UserColorType
#endif
    {
        Hair,        // Regular hair colors
        Eyebrow,     // Eyebrow colors
        Eyelash      // Eyelash colors
    }

    /// <summary>
    /// Identifies which IColor type to get (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor).
    /// Used with GetColorAsync to return the corresponding color value from the avatar.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarColorKind
#else
    public enum AvatarColorKind
#endif
    {
        Hair,           // HairColor
        FacialHair,     // FacialHairColor
        EyeBrows,       // EyeBrowsColor
        EyeLash,        // EyeLashColor
        Skin,           // SkinColor
        Eyes,           // EyeColor
        MakeupStickers, // Stickers (Makeup)
        MakeupLipstick, // Lipstick (Makeup)
        MakeupFreckles, // Freckles (Makeup)
        MakeupFaceGems, // FaceGems (Makeup)
        MakeupEyeshadow,// EyeShadow (Makeup)
        MakeupBlush     // Blush (Makeup)
    }

    /// <summary>
    /// Public makeup category for GetDefaultMakeupByCategoryAsync. Mirrors Genies.MakeupPresets.MakeupPresetCategory
    /// so the API is usable from assemblies that cannot reference the internal type.
    /// </summary>
    public enum MakeupCategory
    {
        None = -1,
        Stickers = 0,
        Lipstick = 1,
        Freckles = 2,
        FaceGems = 3,
        Eyeshadow = 4,
        Blush = 5,
    }

    /// <summary>
    /// Maps public MakeupCategory to the representation used by the internal default inventory API (lowercase string)
    /// and to the integer value used by EquipMakeupColorCommand (avoids referencing MakeupPresetCategory when it is internal).
    /// </summary>
    internal static class MakeupCategoryMapper
    {
        internal static string ToInternal(MakeupCategory category) => category.ToString().ToLowerInvariant();

        /// <summary>
        /// Maps Core MakeupCategory to the integer value used by EquipMakeupColorCommand (avoids referencing MakeupPresetCategory when it is internal).
        /// </summary>
        internal static int ToMakeupPresetCategoryInt(MakeupCategory category)
        {
            return (int)category;
        }
    }

    /// <summary>
    /// Single attribute (name/value) in a native avatar body preset. Wraps GSkelModValue for callers that cannot reference Genies.Avatars.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct NativeAvatarBodyPresetAttribute
#else
    public struct NativeAvatarBodyPresetAttribute
#endif
    {
        public string Name;
        public float Value;
    }

    /// <summary>
    /// Wrapper for native avatar body preset data returned by GetNativeAvatarBodyPresetDataAsync. Mirrors GSkelModifierPreset for callers that cannot reference Genies.Avatars.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NativeAvatarBodyPresetInfo
#else
    public class NativeAvatarBodyPresetInfo
#endif
    {
        public string Name { get; set; }
        public string StartingBodyVariation { get; set; }
        public List<NativeAvatarBodyPresetAttribute> Attributes { get; set; }

        public NativeAvatarBodyPresetInfo()
        {
            Attributes = new List<NativeAvatarBodyPresetAttribute>();
        }

        public NativeAvatarBodyPresetInfo(string name, string startingBodyVariation, List<NativeAvatarBodyPresetAttribute> attributes)
        {
            Name = name ?? string.Empty;
            StartingBodyVariation = startingBodyVariation ?? string.Empty;
            Attributes = attributes ?? new List<NativeAvatarBodyPresetAttribute>();
        }

        /// <summary>
        /// Creates a NativeAvatarBodyPresetInfo from a GSkelModifierPreset.
        /// </summary>
        internal static NativeAvatarBodyPresetInfo FromPreset(GSkelModifierPreset preset)
        {
            if (preset == null)
            {
                return null;
            }

            var attributes = preset.GSkelModValues?
                .Select(g => new NativeAvatarBodyPresetAttribute { Name = g.Name, Value = g.Value })
                .ToList() ?? new List<NativeAvatarBodyPresetAttribute>();
            return new NativeAvatarBodyPresetInfo(preset.Name, preset.StartingBodyVariation, attributes);
        }
    }

    /// <summary>
    /// Information about a color preset (either default or custom).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ColorPresetInfo
#else
    public class ColorPresetInfo
#endif
    {
        /// <summary>
        /// Asset ID or instance ID of the color preset.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Display name of the color preset.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Category of the color preset (e.g., "hair", "skin", "eyebrows").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// List of color values (Unity Colors).
        /// </summary>
        public List<Color> Colors { get; set; }

        /// <summary>
        /// Whether this is a custom (user-created) color or a default preset.
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// Order/priority for display.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Interface for avatar color types that can be applied to the avatar.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IColor
#else
    public interface IColor
#endif
    {
        /// <summary>
        /// Array of color values for this color type.
        /// </summary>
        Color[] Hexes { get; }

        /// <summary>
        /// Optional asset ID associated with this color (for preset colors).
        /// </summary>
        string AssetId { get; }

        bool IsCustom { get; set; }
    }

    /// <summary>
    /// Hair color representation with base, R, G, and B components for gradient effects.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct HairColor : IColor
#else
    public struct HairColor : IColor
#endif
    {
        private readonly Color _base;
        private readonly Color _r;
        private readonly Color _g;
        private readonly Color _b;

        /// <summary>
        /// Creates a new hair color with the specified components and hair type.
        /// </summary>
        /// <param name="hairType">The type of hair (Hair or FacialHair)</param>
        /// <param name="baseColor">The base hair color</param>
        /// <param name="colorR">The red component of the hair color gradient</param>
        /// <param name="colorG">The green component of the hair color gradient</param>
        /// <param name="colorB">The blue component of the hair color gradient</param>
        public HairColor(Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            _base = baseColor;
            _r = colorR;
            _g = colorG;
            _b = colorB;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// Array containing the base, R, G, and B color components.
        /// </summary>
        public Color[] Hexes => new[] { _base, _r, _g, _b };

        /// <summary>
        /// Asset ID for preset hair colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Hair color representation with base, R, G, and B components for gradient effects.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct FacialHairColor : IColor
#else
    public struct FacialHairColor : IColor
#endif
    {
        private readonly Color _base;
        private readonly Color _r;
        private readonly Color _g;
        private readonly Color _b;

        /// <summary>
        /// Creates a new hair color with the specified components and hair type.
        /// </summary>
        /// <param name="hairType">The type of hair (Hair or FacialHair)</param>
        /// <param name="baseColor">The base hair color</param>
        /// <param name="colorR">The red component of the hair color gradient</param>
        /// <param name="colorG">The green component of the hair color gradient</param>
        /// <param name="colorB">The blue component of the hair color gradient</param>
        public FacialHairColor(Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            _base = baseColor;
            _r = colorR;
            _g = colorG;
            _b = colorB;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// Array containing the base, R, G, and B color components.
        /// </summary>
        public Color[] Hexes => new[] { _base, _r, _g, _b };

        /// <summary>
        /// Asset ID for preset hair colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Eyebrow color representation with base, R, G, and B components for gradient effects.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct EyeBrowsColor : IColor
#else
    public struct EyeBrowsColor : IColor
#endif
    {
        private readonly Color _base1;
        private readonly Color _base2;

        /// <summary>
        /// Creates a new eyebrow color with the specified components.
        /// </summary>
        /// <param name="baseColor1">The first base eyebrow color</param>
        /// <param name="baseColor2">The second base eyebrow color</param>
        public EyeBrowsColor(Color baseColor1, Color baseColor2)
        {
            _base1 = baseColor1;
            _base2 = baseColor2;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// Array containing the base, R, G, and B color components.
        /// </summary>
        public Color[] Hexes => new[] { _base1, _base2 };

        /// <summary>
        /// Asset ID for preset colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Eyelash color representation with base, R, G, and B components for gradient effects.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct EyeLashColor : IColor
#else
    public struct EyeLashColor : IColor
#endif
    {
        private readonly Color _base1;
        private readonly Color _base2;

        /// <summary>
        /// Creates a new eyelash color with the specified components.
        /// </summary>
        /// <param name="baseColor1">The first base eyelash color</param>
        /// <param name="baseColor2">The second base eyelash color</param>
        public EyeLashColor(Color baseColor1, Color baseColor2)
        {
            _base1 = baseColor1;
            _base2 = baseColor2;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// Array containing the base, R, G, and B color components.
        /// </summary>
        public Color[] Hexes => new[] { _base1, _base2 };

        /// <summary>
        /// Asset ID for preset colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Skin color representation (single color for avatar skin).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SkinColor : IColor
#else
    public struct SkinColor : IColor
#endif
    {
        private readonly Color _color;
        private bool _isCustom;
        private string _name;
        private int _order;

        /// <summary>
        /// Creates a new skin color with the specified color.
        /// </summary>
        /// <param name="color">The skin color to apply.</param>
        public SkinColor(Color color)
        {
            _color = color;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// Array containing the single skin color.
        /// </summary>
        public Color[] Hexes => new[] { _color };

        /// <summary>
        /// Asset ID for preset colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Eye color representation by asset ID (equipped via outfit/wearable). Implements IColor for use with SetColorAsync.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct EyeColor : IColor
#else
    public struct EyeColor : IColor
#endif
    {
        private readonly string _assetId;
        private readonly Color _base1;
        private readonly Color _base2;

        /// <summary>
        /// Creates a new eye color with the specified asset ID (wearable/outfit asset ID for eye color).
        /// </summary>
        /// <param name="assetId">The asset ID of the eye color to equip.</param>
        public EyeColor(string assetId, Color baseColor1, Color baseColor2)
        {
            _assetId = assetId;
            _base1 = baseColor1;
            _base2 = baseColor2;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// No color hex values; eye color is applied by equipping the asset. Returns an empty array.
        /// </summary>
        public Color[] Hexes => new[] { _base1, _base2 };

        /// <summary>
        /// The asset ID of the eye color to equip (passed as wearableId to EquipOutfitAsync).
        /// </summary>
        public string AssetId => _assetId;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    /// <summary>
    /// Makeup color representation with base, R, G, and B components for gradient effects.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct MakeupColor : IColor
#else
    public struct MakeupColor : IColor
#endif
    {
        private readonly Color _base;
        private readonly Color _r;
        private readonly Color _g;
        private readonly Color _b;
        private readonly MakeupCategory _category;

        /// <summary>
        /// Creates a new Makeup Color with the specified components.
        /// </summary>
        public MakeupColor(Color baseColor, Color colorR, Color colorG, Color colorB)
            : this(MakeupCategory.None, baseColor, colorR, colorG, colorB) { }

        /// <summary>
        /// Creates a new Makeup Color with the specified category and components.
        /// </summary>
        /// <param name="category">The makeup category (e.g. Lipstick, Blush).</param>
        /// <param name="baseColor">The base color</param>
        /// <param name="colorR">The red component of the gradient</param>
        /// <param name="colorG">The green component of the gradient</param>
        /// <param name="colorB">The blue component of the gradient</param>
        public MakeupColor(MakeupCategory category, Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            _category = category;
            _base = baseColor;
            _r = colorR;
            _g = colorG;
            _b = colorB;
            _isCustom = false;
            _name = null;
            _order = 0;
        }

        /// <summary>
        /// The makeup category this color applies to (e.g. Lipstick, Blush, Eyeshadow).
        /// </summary>
        public MakeupCategory Category => _category;

        /// <summary>
        /// Array containing the base, R, G, and B color components.
        /// </summary>
        public Color[] Hexes => new[] { _base, _r, _g, _b };

        /// <summary>
        /// Asset ID for preset hair colors (null for custom colors).
        /// </summary>
        public string AssetId => null;

        private bool _isCustom;
        private string _name;
        private int _order;
        public bool IsCustom { get => _isCustom; set => _isCustom = value; }
        public string Name { get => _name; set => _name = value; }
        public int Order { get => _order; set => _order = value; }
    }

    internal static class AvatarFeatureStatMapping
    {
        internal static AvatarFeatureStat ToAvatarFeatureStat(BodyStats s) => s switch
        {
            BodyStats.NeckThickness => AvatarFeatureStat.Body_NeckThickness,
            BodyStats.ShoulderBroadness => AvatarFeatureStat.Body_ShoulderBroadness,
            BodyStats.ChestBustline => AvatarFeatureStat.Body_ChestBustline,
            BodyStats.ArmsThickness => AvatarFeatureStat.Body_ArmsThickness,
            BodyStats.WaistThickness => AvatarFeatureStat.Body_WaistThickness,
            BodyStats.BellyFullness => AvatarFeatureStat.Body_BellyFullness,
            BodyStats.HipsThickness => AvatarFeatureStat.Body_HipsThickness,
            BodyStats.LegsThickness => AvatarFeatureStat.Body_LegsThickness,
            BodyStats.HipSize => AvatarFeatureStat.Body_HipSize,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        internal static AvatarFeatureStat ToAvatarFeatureStat(EyeBrowsStats s) => s switch
        {
            EyeBrowsStats.Thickness => AvatarFeatureStat.EyeBrows_Thickness,
            EyeBrowsStats.Length => AvatarFeatureStat.EyeBrows_Length,
            EyeBrowsStats.VerticalPosition => AvatarFeatureStat.EyeBrows_VerticalPosition,
            EyeBrowsStats.Spacing => AvatarFeatureStat.EyeBrows_Spacing,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        internal static AvatarFeatureStat ToAvatarFeatureStat(EyeStats s) => s switch
        {
            EyeStats.Size => AvatarFeatureStat.Eyes_Size,
            EyeStats.VerticalPosition => AvatarFeatureStat.Eyes_VerticalPosition,
            EyeStats.Spacing => AvatarFeatureStat.Eyes_Spacing,
            EyeStats.Rotation => AvatarFeatureStat.Eyes_Rotation,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        internal static AvatarFeatureStat ToAvatarFeatureStat(JawStats s) => s switch
        {
            JawStats.Width => AvatarFeatureStat.Jaw_Width,
            JawStats.Length => AvatarFeatureStat.Jaw_Length,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        internal static AvatarFeatureStat ToAvatarFeatureStat(LipsStats s) => s switch
        {
            LipsStats.Width => AvatarFeatureStat.Lips_Width,
            LipsStats.Fullness => AvatarFeatureStat.Lips_Fullness,
            LipsStats.VerticalPosition => AvatarFeatureStat.Lips_VerticalPosition,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        internal static AvatarFeatureStat ToAvatarFeatureStat(NoseStats s) => s switch
        {
            NoseStats.Width => AvatarFeatureStat.Nose_Width,
            NoseStats.Length => AvatarFeatureStat.Nose_Length,
            NoseStats.VerticalPosition => AvatarFeatureStat.Nose_VerticalPosition,
            NoseStats.Tilt => AvatarFeatureStat.Nose_Tilt,
            NoseStats.Projection => AvatarFeatureStat.Nose_Projection,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, null)
        };

        /// <summary>
        /// Returns the body attribute ID string for the given AvatarFeatureStat (for ModifyAvatarFeatureStatsAsync).
        /// </summary>
        internal static string GetAttributeId(AvatarFeatureStat stat) => stat switch
        {
            AvatarFeatureStat.Body_NeckThickness => GenieBodyAttribute.WeightHeadNeck,
            AvatarFeatureStat.Body_ShoulderBroadness => GenieBodyAttribute.ShoulderSize,
            AvatarFeatureStat.Body_ChestBustline => GenieBodyAttribute.WeightUpperTorso,
            AvatarFeatureStat.Body_ArmsThickness => GenieBodyAttribute.WeightArms,
            AvatarFeatureStat.Body_WaistThickness => GenieBodyAttribute.Waist,
            AvatarFeatureStat.Body_BellyFullness => GenieBodyAttribute.Belly,
            AvatarFeatureStat.Body_HipsThickness => GenieBodyAttribute.WeightLowerTorso,
            AvatarFeatureStat.Body_LegsThickness => GenieBodyAttribute.WeightLegs,
            AvatarFeatureStat.Body_HipSize => GenieBodyAttribute.HipSize,
            AvatarFeatureStat.EyeBrows_Thickness => GenieBodyAttribute.BrowThickness,
            AvatarFeatureStat.EyeBrows_Length => GenieBodyAttribute.BrowLength,
            AvatarFeatureStat.EyeBrows_VerticalPosition => GenieBodyAttribute.BrowPositionVert,
            AvatarFeatureStat.EyeBrows_Spacing => GenieBodyAttribute.BrowSpacing,
            AvatarFeatureStat.Eyes_Size => GenieBodyAttribute.EyeSize,
            AvatarFeatureStat.Eyes_VerticalPosition => GenieBodyAttribute.EyePositionVert,
            AvatarFeatureStat.Eyes_Spacing => GenieBodyAttribute.EyeSpacing,
            AvatarFeatureStat.Eyes_Rotation => GenieBodyAttribute.EyeTilt,
            AvatarFeatureStat.Jaw_Width => GenieBodyAttribute.JawWidth,
            AvatarFeatureStat.Jaw_Length => GenieBodyAttribute.JawLength,
            AvatarFeatureStat.Lips_Width => GenieBodyAttribute.LipWidth,
            AvatarFeatureStat.Lips_Fullness => GenieBodyAttribute.LipFullness,
            AvatarFeatureStat.Lips_VerticalPosition => GenieBodyAttribute.LipPositionVert,
            AvatarFeatureStat.Nose_Width => GenieBodyAttribute.NoseWidth,
            AvatarFeatureStat.Nose_Length => GenieBodyAttribute.NoseHeight,
            AvatarFeatureStat.Nose_VerticalPosition => GenieBodyAttribute.NosePositionVert,
            AvatarFeatureStat.Nose_Tilt => GenieBodyAttribute.NoseTilt,
            AvatarFeatureStat.Nose_Projection => GenieBodyAttribute.NoseProjection,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }

    /// <summary>
    /// Static convenience facade for opening and closing the Avatar Editor.
    /// - Auto-initializes required services on first use
    /// - Provides public static methods for opening and closing the editor
    /// - Follows the same pattern as GeniesAvatarsSdk for consistency
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarEditorSDK
#else
    public static class AvatarEditorSDK
#endif
    {
        private static bool IsInitialized =>
            InitializationCompletionSource is not null
            && InitializationCompletionSource.Task.Status == UniTaskStatus.Succeeded;
        private static UniTaskCompletionSource InitializationCompletionSource { get; set; }

        private static IAvatarEditorSdkService CachedService { get; set; }
        private static bool EventsSubscribed { get; set; }

        private static readonly Dictionary<HairType, WardrobeSubcategory> HairTypeToWardrobeSubcategory = new Dictionary<HairType, WardrobeSubcategory>
        {
            { HairType.Hair, WardrobeSubcategory.hair },
            { HairType.FacialHair, WardrobeSubcategory.facialHair },
            { HairType.Eyebrows, WardrobeSubcategory.eyebrows },
            { HairType.Eyelashes, WardrobeSubcategory.eyelashes }
        };

        private static readonly Dictionary<WearablesCategory, WardrobeSubcategory> WearableCategoryToWardrobeSubcategory = new Dictionary<WearablesCategory, WardrobeSubcategory>
        {
            { WearablesCategory.Hoodie, WardrobeSubcategory.hoodie },
            { WearablesCategory.Shirt, WardrobeSubcategory.shirt },
            { WearablesCategory.Dress, WardrobeSubcategory.dress },
            { WearablesCategory.Pants, WardrobeSubcategory.pants },
            { WearablesCategory.Shorts, WardrobeSubcategory.shorts },
            { WearablesCategory.Skirt, WardrobeSubcategory.skirt },
            { WearablesCategory.Shoes, WardrobeSubcategory.shoes },
            { WearablesCategory.Earrings, WardrobeSubcategory.earrings },
            { WearablesCategory.Glasses, WardrobeSubcategory.glasses },
            { WearablesCategory.Hat, WardrobeSubcategory.hat },
            { WearablesCategory.Mask, WardrobeSubcategory.mask },
        };

        /// <summary>
        /// Maps UserWearablesCategory to WearablesCategory for use with WearableCategoryToWardrobeSubcategory and other internal APIs.
        /// </summary>
        private static WearablesCategory ToWearablesCategory(UserWearablesCategory userCategory)
        {
            return userCategory switch
            {
                UserWearablesCategory.Hoodie => WearablesCategory.Hoodie,
                UserWearablesCategory.Shirt => WearablesCategory.Shirt,
                UserWearablesCategory.Pants => WearablesCategory.Pants,
                UserWearablesCategory.Shorts => WearablesCategory.Shorts,
                _ => throw new ArgumentOutOfRangeException(nameof(userCategory), userCategory, "Invalid UserWearablesCategory")
            };
        }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public static event Action EditorOpened = delegate { };

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public static event Action EditorClosed = delegate { };

        /// <summary>
        /// Event raised when an asset is equipped.
        /// Payload: wearableId
        /// </summary>
        public static event Action<string> EquippedAsset = delegate { };

        /// <summary>
        /// Event raised when an asset is unequipped.
        /// Payload: wearableId
        /// </summary>
        public static event Action<string> UnequippedAsset = delegate { };

        /// <summary>
        /// Event raised when a skin color is set.
        /// </summary>
        public static event Action SkinColorSet = delegate { };

        /// <summary>
        /// Event raised when hair colors are set.
        /// </summary>
        public static event Action HairColorSet = delegate { };

        /// <summary>
        /// Event raised when a hair style is equipped.
        /// Payload: hairAssetId
        /// </summary>
        public static event Action<string> HairEquipped = delegate { };

        /// <summary>
        /// Event raised when a hair style is unequipped.
        /// Payload: hairAssetId
        /// </summary>
        public static event Action<string> HairUnequipped = delegate { };

        /// <summary>
        /// Event raised when a tattoo is equipped.
        /// Payload: tattooId
        /// </summary>
        public static event Action<string> TattooEquipped = delegate { };

        /// <summary>
        /// Event raised when a tattoo is unequipped.
        /// Payload: tattooId
        /// </summary>
        public static event Action<string> TattooUnequipped = delegate { };

        /// <summary>
        /// Event raised when a native avatar body preset is applied.
        /// </summary>
        public static event Action BodyPresetSet = delegate { };

        /// <summary>
        /// Event raised when avatar body type is set (gender + body size).
        /// </summary>
        public static event Action BodyTypeSet = delegate { };

        /// <summary>
        /// Event raised when an avatar definition is saved (local or cloud depending on mode).
        /// </summary>
        public static event Action AvatarDefinitionSaved = delegate { };

        /// <summary>
        /// Event raised when an avatar definition is saved locally.
        /// </summary>
        public static event Action AvatarDefinitionSavedLocally = delegate { };

        /// <summary>
        /// Event raised when an avatar definition is saved to cloud.
        /// </summary>
        public static event Action AvatarDefinitionSavedToCloud = delegate { };

        /// <summary>
        /// Event raised when the editor save option is changed.
        /// </summary>
        public static event Action EditorSaveOptionSet = delegate { };

        /// <summary>
        /// Event raised when editor save settings are changed.
        /// </summary>
        public static event Action EditorSaveSettingsSet = delegate { };

        /// <summary>
        /// Event raised when an avatar is loaded for editing.
        /// </summary>
        public static event Action AvatarLoadedForEditing = delegate { };

        #region Initialization / Service Access

        public static async UniTask<bool> InitializeAsync()
        {
            return await AvatarEditorInitializer.Instance.InitializeAsync();
        }

        internal static async UniTask<IAvatarEditorSdkService> GetOrCreateAvatarEditorSdkInstance()
        {
            if (await InitializeAsync() is false)
            {
                CrashReporter.LogError("Avatar editor could not be initialized.");
                return default;
            }

            var service = ServiceManager.Get<IAvatarEditorSdkService>();
            SubscribeToServiceEvents(service);
            return service;
        }

        private static void SubscribeToServiceEvents(IAvatarEditorSdkService service)
        {
            if (service == null)
            {
                return;
            }

            if (ReferenceEquals(service, CachedService)
                && EventsSubscribed)
            {
                return;
            }

            CachedService = service;
            CachedService.EditorOpened += OnEditorOpened;
            CachedService.EditorClosed += OnEditorClosed;

            EventsSubscribed = true;
        }

        private static void OnEditorOpened()
        {
            EditorOpened?.Invoke();
        }

        private static void OnEditorClosed()
        {
            EditorClosed?.Invoke();
        }

        #endregion

        #region Public Static API

        public static async UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.OpenEditorAsync(avatar, camera);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to open avatar editor: {ex.Message}");
            }
        }

        public static async UniTask CloseEditorAsync(bool revertAvatar)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.CloseEditorAsync(revertAvatar);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to close avatar editor: {ex.Message}");
            }
        }

        public static GeniesAvatar GetCurrentActiveAvatar()
        {
            try
            {
                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                return avatarEditorSdkService?.GetCurrentActiveAvatar();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get current active avatar: {ex.Message}");
                return null;
            }
        }

        public static bool IsEditorOpen
        {
            get
            {
                try
                {
                    var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                    return avatarEditorSdkService?.IsEditorOpen ?? false;
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"Failed to get editor open state: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information filtered by categories and asset type. Provides a unified interface for retrieving default, user-owned, or all wearable assets.
        /// </summary>
        /// <param name="assetType">The asset type for wearable assets - All, User, or Default</param>
        /// <param name="categories">List of WardrobeSubcategory enum values to filter by (e.g., WardrobeSubcategory.hoodie, WardrobeSubcategory.hair, etc.). If null or empty, returns all wearables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        private static async UniTask<List<WearableAssetInfo>> GetWearableAssetsByCategoryAsync(RequestType assetType, List<WardrobeSubcategory> categories = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var result = new List<WearableAssetInfo>();

                // Get default assets if assetType is All or Default
                if (assetType == RequestType.All || assetType == RequestType.Default)
                {
                    var defaultAssets = await avatarEditorSdkService.GetDefaultWearableAssetsListByCategoriesAsync(categories ?? new List<WardrobeSubcategory>(), cancellationToken);
                    if (defaultAssets != null)
                    {
                        result.AddRange(defaultAssets);
                    }
                }

                // Get user assets if assetType is All or User
                if (assetType == RequestType.All || assetType == RequestType.User)
                {
                    var userAssets = await avatarEditorSdkService.GetUserWearableAssetsListByCategoriesAsync(categories ?? new List<WardrobeSubcategory>(), cancellationToken);
                    if (userAssets != null)
                    {
                        result.AddRange(userAssets);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable assets by category: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Gets default hair wearable assets for the given hair types. Maps each HairType to its WardrobeSubcategory and returns the result of GetWearableAssetsByCategoryAsync for default assets.
        /// </summary>
        /// <param name="hairTypes">Hair type to fetch default assets for (e.g. Hair, FacialHair, Eyebrows, Eyelashes).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for default hair asset in the requested category.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetDefaultHairAssets(HairType hairType, CancellationToken cancellationToken = default)
        {
            var categories = new List<WardrobeSubcategory>();
            if (HairTypeToWardrobeSubcategory.TryGetValue(hairType, out var subcategory))
            {
                categories.Add(subcategory);
            }

            if (categories.Count == 0)
            {
                return new List<WearableAssetInfo>();
            }

            return await GetWearableAssetsByCategoryAsync(RequestType.Default, categories, cancellationToken);
        }

        /// <summary>
        /// Gets default wearable assets for the given wearable categories (non-hair). Maps each WearablesCategory to its WardrobeSubcategory and returns the result of GetWearableAssetsByCategoryAsync with RequestType.Default.
        /// </summary>
        /// <param name="wearableCategories">List of wearable categories to fetch default assets for (e.g. Hoodie, Shirt, Pants). If null or empty, returns an empty list.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for the requested wearable categories (default assets only).</returns>
        public static async UniTask<List<WearableAssetInfo>> GetDefaultWearablesByCategoryAsync(List<WearablesCategory> wearableCategories, CancellationToken cancellationToken = default)
        {
            if (wearableCategories == null || wearableCategories.Count == 0)
            {
                return new List<WearableAssetInfo>();
            }

            var categories = new List<WardrobeSubcategory>();
            foreach (var wearable in wearableCategories)
            {
                if (WearableCategoryToWardrobeSubcategory.TryGetValue(wearable, out var subcategory))
                {
                    categories.Add(subcategory);
                }
            }

            if (categories.Count == 0)
            {
                return new List<WearableAssetInfo>();
            }

            return await GetWearableAssetsByCategoryAsync(RequestType.Default, categories, cancellationToken);
        }

        /// <summary>
        /// Gets user wearable assets for the given wearable categories (non-hair). Maps each WearablesCategory to its WardrobeSubcategory and returns the result of GetWearableAssetsByCategoryAsync with RequestType.User.
        /// </summary>
        /// <param name="wearableCategories">List of wearable categories to fetch user assets for (e.g. Hoodie, Shirt, Pants). If null or empty, returns an empty list.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for the requested wearable categories (user assets only).</returns>
        public static async UniTask<List<WearableAssetInfo>> GetUserWearablesByCategoryAsync(List<UserWearablesCategory> wearableCategories, CancellationToken cancellationToken = default)
        {
            if (wearableCategories == null || wearableCategories.Count == 0)
            {
                return new List<WearableAssetInfo>();
            }

            var categories = new List<WardrobeSubcategory>();
            foreach (var userCategory in wearableCategories)
            {
                var wearable = ToWearablesCategory(userCategory);
                if (WearableCategoryToWardrobeSubcategory.TryGetValue(wearable, out var subcategory))
                {
                    categories.Add(subcategory);
                }
            }

            if (categories.Count == 0)
            {
                return new List<WearableAssetInfo>();
            }

            return await GetWearableAssetsByCategoryAsync(RequestType.User, categories, cancellationToken);
        }

        /// <summary>
        /// Gets default avatar features data filtered by category from the default inventory service.
        /// </summary>
        /// <param name="category">The AvatarFeatureCategory to filter by (e.g., Eyes, Jaw, Lips, Nose, Brow). None returns all avatar base data.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing asset information, filtered by category if not None.</returns>
        public static async UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(AvatarFeatureCategory category, CancellationToken cancellationToken = default)
        {
            string categoryFilter = category == AvatarFeatureCategory.None ? null : category.ToString();
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetAvatarFeatureAssetInfoListByCategoryAsync(categoryFilter, null, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default avatar features data: {ex.Message}");
                return new List<AvatarFeaturesInfo>();
            }
        }

        public static async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GiveAssetToUserAsync(assetId);
            }
            catch (Exception ex)
            {
                string error = $"Failed to give asset to user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }

        /// <summary>
        /// Gets default (curated) color presets for the specified color type.
        /// </summary>
        /// <param name="colorType">The type of color to retrieve (Hair, FacialHair, Skin, Eyebrow, Eyelash, or Makeup)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of IColor containing asset IDs and color values (default presets only)</returns>
        public static async UniTask<List<IColor>> GetDefaultColorsAsync(ColorType colorType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetDefaultColorsAsync(colorType, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default {colorType} colors: {ex.Message}");
                return new List<IColor>();
            }
        }

        /// <summary>
        /// Gets default avatar makeup assets filtered by category from the default inventory service.
        /// </summary>
        /// <param name="categories">List of MakeupCategory (e.g. Stickers, Lipstick, Freckles, FaceGems, Eyeshadow, Blush). If null or empty, returns all default makeup. None is skipped.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarItemInfo for the requested makeup categories.</returns>
        public static async UniTask<List<AvatarMakeupInfo>> GetDefaultMakeupByCategoryAsync(MakeupCategory category, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetMakeupAssetInfoListByCategoryAsync(new List<MakeupCategory>() {category}, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default makeup: {ex.Message}");
                return new List<AvatarMakeupInfo>();
            }
        }

        /// <summary>
        /// Gets default tattoo assets from the default inventory service (image library category "Tattoos").
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarItemInfo for default tattoos.</returns>
        public static async UniTask<List<AvatarTattooInfo>> GetDefaultTattoosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var tattooAssetInfoList = await avatarEditorSdkService.GetDefaultTattooAssetInfoListAsync(cancellationToken);
                return AvatarTattooInfo.FromTattooAssetInfoList(tattooAssetInfoList);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default tattoos: {ex.Message}");
                return new List<AvatarTattooInfo>();
            }
        }

        /// <summary>
        /// Gets user (custom) color presets for the specified color type. Only Hair and Skin support custom colors.
        /// </summary>
        /// <param name="colorType">The type of user color to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of ColorPresetInfo containing user-defined color presets</returns>
        public static async UniTask<List<IColor>> GetUserColorsAsync(UserColorType colorType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                return await avatarEditorSdkService.GetUserColorsAsync(colorType, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get user {colorType} colors: {ex.Message}");
                return new List<IColor>();
            }
        }

        private static string GetColorTypeCategory(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Eyes => "eyes",
                ColorType.Hair => "hair",
                ColorType.FacialHair => "facialhair",
                ColorType.Skin => "skin",
                ColorType.Eyebrow => "flaireyebrow",
                ColorType.Eyelash => "flaireyelash",
                ColorType.MakeupStickers => "makeup",
                ColorType.MakeupLipstick => "makeup",
                ColorType.MakeupFreckles => "makeup",
                ColorType.MakeupFaceGems => "makeup",
                ColorType.MakeupEyeshadow => "makeup",
                ColorType.MakeupBlush => "makeup",
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Invalid color type")
            };
        }

        /// <summary>
        /// Maps ColorType (makeup only) to MakeupCategory for filtering default color presets and building MakeupColor.
        /// </summary>
        private static MakeupCategory GetMakeupCategoryFromColorType(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.MakeupStickers => MakeupCategory.Stickers,
                ColorType.MakeupLipstick => MakeupCategory.Lipstick,
                ColorType.MakeupFreckles => MakeupCategory.Freckles,
                ColorType.MakeupFaceGems => MakeupCategory.FaceGems,
                ColorType.MakeupEyeshadow => MakeupCategory.Eyeshadow,
                ColorType.MakeupBlush => MakeupCategory.Blush,
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Not a makeup ColorType")
            };
        }

        /// <summary>
        /// Maps AvatarColorKind (makeup only) to MakeupCategory for filtering default color presets and building MakeupColor.
        /// </summary>
        private static MakeupCategory GetMakeupCategoryFromAvatarColorKind(AvatarColorKind colorKind)
        {
            return colorKind switch
            {
                AvatarColorKind.MakeupStickers => MakeupCategory.Stickers,
                AvatarColorKind.MakeupLipstick => MakeupCategory.Lipstick,
                AvatarColorKind.MakeupFreckles => MakeupCategory.Freckles,
                AvatarColorKind.MakeupFaceGems => MakeupCategory.FaceGems,
                AvatarColorKind.MakeupEyeshadow => MakeupCategory.Eyeshadow,
                AvatarColorKind.MakeupBlush => MakeupCategory.Blush,
                _ => throw new ArgumentOutOfRangeException(nameof(colorKind), colorKind, "Not a makeup AvatarColorKind")
            };
        }

        private static string GetUserColorTypeCategory(UserColorType colorType)
        {
            return colorType switch
            {
                UserColorType.Hair => "hair",
                UserColorType.Eyebrow => "eyebrow",
                UserColorType.Eyelash => "eyelash",
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Invalid user color type")
            };
        }

        public static async UniTask EquipWearableAsync(GeniesAvatar avatar, WearableAssetInfo assetInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                if (assetInfo == null || String.IsNullOrEmpty(assetInfo.AssetId))
                {
                    CrashReporter.LogError("Valid asset is required to equip wearable");
                    return;
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipOutfitAsync(avatar, assetInfo.AssetId, cancellationToken);
                EquippedAsset?.Invoke(assetInfo.AssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip wearable: {ex.Message}");
            }
        }

        public static async UniTask EquipWearableByWearableIdAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipOutfitAsync(avatar, wearableId, cancellationToken);
                EquippedAsset?.Invoke(wearableId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip outfit: {ex.Message}");
            }
        }

        public static async UniTask UnEquipWearableAsync(GeniesAvatar avatar, WearableAssetInfo assetInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                if (assetInfo == null || String.IsNullOrEmpty(assetInfo.AssetId))
                {
                    CrashReporter.LogError("Valid asset is required to unequip wearable");
                    return;
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipOutfitAsync(avatar, assetInfo.AssetId, cancellationToken);
                UnequippedAsset?.Invoke(assetInfo.AssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip outfit: {ex.Message}");
            }
        }

        public static async UniTask UnEquipWearableByWearableIdAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipOutfitAsync(avatar, wearableId, cancellationToken);
                UnequippedAsset?.Invoke(wearableId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip outfit: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips makeup on the avatar using the given AvatarItemInfo (e.g. from GetDefaultMakeupByCategoryAsync).
        /// </summary>
        /// <param name="avatar">The avatar to equip the makeup on.</param>
        /// <param name="asset">The avatar item (e.g. makeup) to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public static async UniTask EquipMakeupAsync(GeniesAvatar avatar, AvatarMakeupInfo asset, CancellationToken cancellationToken = default)
        {
            if (asset == null || string.IsNullOrEmpty(asset.AssetId))
            {
                return;
            }

            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipMakeupAsync(avatar, asset.AssetId, cancellationToken);
                EquippedAsset?.Invoke(asset.AssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar makeup: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips makeup on the avatar using the given AvatarItemInfo's AssetId.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the makeup from.</param>
        /// <param name="asset">The avatar item (e.g. makeup) to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public static async UniTask UnEquipMakeupAsync(GeniesAvatar avatar, AvatarMakeupInfo asset, CancellationToken cancellationToken = default)
        {
            if (asset == null || string.IsNullOrEmpty(asset.AssetId))
            {
                return;
            }

            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipMakeupAsync(avatar, asset.AssetId, cancellationToken);
                UnequippedAsset?.Invoke(asset.AssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip makeup: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets hair, eyebrow, eyelash, skin, or eye color on the avatar based on the passed-in IColor (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor).
        /// EyeColor is applied by calling EquipOutfitAsync with the color's AssetId as the wearableId.
        /// </summary>
        /// <param name="avatar">The avatar to modify</param>
        /// <param name="color">The color to apply (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor or EyeColor)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if the color was successfully set, false otherwise</returns>
        public static async UniTask<bool> SetColorAsync(GeniesAvatar avatar, IColor color, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                if (color == null)
                {
                    CrashReporter.LogError("Color cannot be null");
                    return false;
                }

                if (avatar == null)
                {
                    CrashReporter.LogError("Avatar cannot be null");
                    return false;
                }

                switch (color)
                {
                    case HairColor hairColor:
                        if (hairColor.Hexes != null && hairColor.Hexes.Length > 3)
                        {
                            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                            await avatarEditorSdkService.ModifyAvatarHairColorAsync(avatar,
                                HairType.Hair,
                                hairColor.Hexes[0],
                                hairColor.Hexes[1],
                                hairColor.Hexes[2],
                                hairColor.Hexes[3],
                                cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("FacialHairColor must have exactly 4 color values (base, r, g, b)");
                        return false;

                    case FacialHairColor facialhairColor:
                        if (facialhairColor.Hexes != null && facialhairColor.Hexes.Length > 3)
                        {
                            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                            await avatarEditorSdkService.ModifyAvatarHairColorAsync(avatar,
                                HairType.FacialHair,
                                facialhairColor.Hexes[0],
                                facialhairColor.Hexes[1],
                                facialhairColor.Hexes[2],
                                facialhairColor.Hexes[3],
                                cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("FacialHairColor must have exactly 4 color values (base, r, g, b)");
                        return false;

                    case EyeBrowsColor eyeBrowsColor:
                        if (eyeBrowsColor.Hexes != null && eyeBrowsColor.Hexes.Length > 1)
                        {
                            var flairService = await GetOrCreateAvatarEditorSdkInstance();
                            await flairService.ModifyAvatarFlairColorAsync(avatar,
                                HairType.Eyebrows,
                                eyeBrowsColor.Hexes,
                                cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("EyeBrowsColor must have exactly 2 color values");
                        return false;

                    case EyeLashColor eyeLashColor:
                        if (eyeLashColor.Hexes != null && eyeLashColor.Hexes.Length > 1)
                        {
                            var eyelashService = await GetOrCreateAvatarEditorSdkInstance();
                            await eyelashService.ModifyAvatarFlairColorAsync(avatar,
                                HairType.Eyelashes,
                                eyeLashColor.Hexes,
                                cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("EyeLashColor must have exactly 2 color values");
                        return false;

                    case SkinColor skinColor:
                        if (skinColor.Hexes != null && skinColor.Hexes.Length > 0)
                        {
                            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                            await avatarEditorSdkService.SetSkinColorAsync(avatar, skinColor.Hexes[0], cancellationToken);
                            SkinColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("SkinColor must have at least one color value.");
                        return false;

                    case EyeColor eyeColor:
                        if (!string.IsNullOrEmpty(eyeColor.AssetId))
                        {
                            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                            await avatarEditorSdkService.EquipOutfitAsync(avatar, eyeColor.AssetId, cancellationToken);
                            try
                            {
                                if (avatar?.Controller == null)
                                {
                                    CrashReporter.LogError("Avatar and controller are required to set eye color");
                                    return false;
                                }

                                if (string.IsNullOrEmpty(eyeColor.AssetId))
                                {
                                    CrashReporter.LogError("Valid AssetId is required to set eye color");
                                    return false;
                                }

                                var command = new EquipNativeAvatarAssetCommand(eyeColor.AssetId, avatar.Controller);
                                await command.ExecuteAsync(cancellationToken);
                                EquippedAsset?.Invoke(eyeColor.AssetId);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                CrashReporter.LogError($"Failed to set eye color: {ex.Message}");
                                return false;
                            }
                        }
                        CrashReporter.LogError("EyeColor must have a non-empty AssetId.");
                        return false;
                    case MakeupColor makeupColor:
                        if (makeupColor.Hexes != null && makeupColor.Hexes.Length >= 4)
                        {
                            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                            await avatarEditorSdkService.SetMakeupColorAsync(avatar, makeupColor.Category, makeupColor.Hexes, cancellationToken);
                            return true;
                        }
                        CrashReporter.LogError("Makeup color must have exactly 4 values (base, r, g, b)");
                        return false;
                    default:
                        CrashReporter.LogError($"SetColorAsync supports only HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor, not {color.GetType().Name}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set color: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets color on the avatar by color kind and raw data. Use this overload when calling from assemblies that cannot reference IColor (e.g. SDK with MakeupAssetInfo pattern).
        /// </summary>
        public static async UniTask<bool> SetColorDataAsync(GeniesAvatar avatar, AvatarColorKind colorKind, Color[] hexes, string assetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }
                if (avatar == null)
                {
                    CrashReporter.LogError("Avatar cannot be null");
                    return false;
                }
                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                switch (colorKind)
                {
                    case AvatarColorKind.Hair:
                        if (hexes != null && hexes.Length >= 4)
                        {
                            await avatarEditorSdkService.ModifyAvatarHairColorAsync(avatar, HairType.Hair, hexes[0], hexes[1], hexes[2], hexes[3], cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("Hair color must have exactly 4 values (base, r, g, b)");
                        return false;
                    case AvatarColorKind.FacialHair:
                        if (hexes != null && hexes.Length >= 4)
                        {
                            await avatarEditorSdkService.ModifyAvatarHairColorAsync(avatar, HairType.FacialHair, hexes[0], hexes[1], hexes[2], hexes[3], cancellationToken);
                            HairColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("FacialHair color must have exactly 4 values (base, r, g, b)");
                        return false;
                    case AvatarColorKind.EyeBrows:
                        if (hexes != null && hexes.Length > 1)
                        {
                            await avatarEditorSdkService.ModifyAvatarFlairColorAsync(avatar, HairType.Eyebrows, hexes, cancellationToken);
                            return true;
                        }
                        CrashReporter.LogError("EyeBrows color must have exactly 2 values");
                        return false;
                    case AvatarColorKind.EyeLash:
                        if (hexes != null && hexes.Length > 1)
                        {
                            await avatarEditorSdkService.ModifyAvatarFlairColorAsync(avatar, HairType.Eyelashes, hexes, cancellationToken);
                            return true;
                        }
                        CrashReporter.LogError("EyeLash color must have exactly 2 values");
                        return false;
                    case AvatarColorKind.Skin:
                        if (hexes != null && hexes.Length >= 1)
                        {
                            await avatarEditorSdkService.SetSkinColorAsync(avatar, hexes[0], cancellationToken);
                            SkinColorSet?.Invoke();
                            return true;
                        }
                        CrashReporter.LogError("Skin color must have at least one value.");
                        return false;
                    case AvatarColorKind.Eyes:
                        if (!string.IsNullOrEmpty(assetId))
                        {
                            await avatarEditorSdkService.EquipOutfitAsync(avatar, assetId, cancellationToken);
                            var command = new EquipNativeAvatarAssetCommand(assetId, avatar.Controller);
                            await command.ExecuteAsync(cancellationToken);
                            EquippedAsset?.Invoke(assetId);
                            return true;
                        }
                        CrashReporter.LogError("Eye color must have a non-empty AssetId.");
                        return false;
                    case AvatarColorKind.MakeupStickers:
                    case AvatarColorKind.MakeupLipstick:
                    case AvatarColorKind.MakeupFreckles:
                    case AvatarColorKind.MakeupFaceGems:
                    case AvatarColorKind.MakeupEyeshadow:
                    case AvatarColorKind.MakeupBlush:
                        if (hexes != null && hexes.Length >= 4)
                        {
                            MakeupCategory category = GetMakeupCategoryFromAvatarColorKind(colorKind);
                            await avatarEditorSdkService.SetMakeupColorAsync(avatar, category, hexes, cancellationToken);
                            return true;
                        }
                        CrashReporter.LogError("Makeup color must have exactly 4 values (base, r, g, b)");
                        return false;
                    default:
                        CrashReporter.LogError($"Unknown AvatarColorKind: {colorKind}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set color: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Internal helper: converts ColorType and color data to Core IColor for GetDefaultColorsAsync.
        /// Public API: use Genies.Sdk.ColorMapper.ToIColorValue in the SDK for IAvatarColor.
        /// </summary>
        private static IColor ToIColorValueInternal(ColorType colorType, List<Color> colors, string assetId = null)
        {
            bool isEmpty = colors == null || colors.Count == 0;
            Color clear = Color.clear;

            switch (colorType)
            {
                case ColorType.Skin:
                    {
                        return new SkinColor(isEmpty ? clear : colors[0]);
                    }

                case ColorType.Hair:
                case ColorType.FacialHair:
                case ColorType.MakeupStickers:
                case ColorType.MakeupLipstick:
                case ColorType.MakeupFreckles:
                case ColorType.MakeupFaceGems:
                case ColorType.MakeupEyeshadow:
                case ColorType.MakeupBlush:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        Color c2 = (colors != null && colors.Count > 2) ? colors[2] : c0;
                        Color c3 = (colors != null && colors.Count > 3) ? colors[3] : c0;
                        if (colorType == ColorType.Hair)
                        {
                            return new HairColor(c0, c1, c2, c3);
                        }

                        if (colorType == ColorType.FacialHair)
                        {
                            return new FacialHairColor(c0, c1, c2, c3);
                        }
                        return new MakeupColor(c0, c1, c2, c3);
                    }
                case ColorType.Eyebrow:
                case ColorType.Eyelash:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;

                        if (colorType == ColorType.Eyebrow)
                        {
                            return new EyeBrowsColor(c0, c1);
                        }

                        return new EyeLashColor(c0, c1);
                    }
                case ColorType.Eyes:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;

                        return new EyeColor(assetId ?? string.Empty, c0, c1);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Unsupported ColorType.");
            }
        }

        /// <summary>
        /// Internal helper: converts ColorType and color data to Core IColor for GetDefaultColorsAsync.
        /// Public API: use Genies.Sdk.ColorMapper.ToIColorValue in the SDK for IAvatarColor.
        /// </summary>
        private static IColor ToIColorValueInternalUser(UserColorType colorType, List<Color> colors, string assetId = null)
        {
            bool isEmpty = colors == null || colors.Count == 0;
            Color clear = Color.clear;

            switch (colorType)
            {
                case UserColorType.Hair:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        Color c2 = (colors != null && colors.Count > 2) ? colors[2] : c0;
                        Color c3 = (colors != null && colors.Count > 3) ? colors[3] : c0;
                        return new HairColor(c0, c1, c2, c3);
                    }

                case UserColorType.Eyebrow:
                case UserColorType.Eyelash:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;

                        if (colorType == UserColorType.Eyebrow)
                        {
                            return new EyeBrowsColor(c0, c1);
                        }

                        return new EyeLashColor(c0, c1);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Unsupported UserColorType.");
            }
        }

        /// <summary>
        /// Gets the current color from the avatar for the specified color kind (Hair, FacialHair, EyeBrows, EyeLash, Skin, or Eyes).
        /// Returns an IColor instance of the corresponding type (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor).
        /// </summary>
        /// <param name="avatar">The avatar to read the color from</param>
        /// <param name="colorKind">Which IColor type to return (Hair, FacialHair, EyeBrows, EyeLash, Skin, Eyes)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask that completes with the corresponding IColor value. Implementation is placeholder; currently returns default/empty values.</returns>
        public static async UniTask<IColor> GetColorAsync(GeniesAvatar avatar, AvatarColorKind colorKind, CancellationToken cancellationToken = default)
        {
            if (avatar == null)
            {
                CrashReporter.LogError("Avatar cannot be null");
                return null;
            }

            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
            return await avatarEditorSdkService.GetColorAsync(avatar, colorKind, cancellationToken);
        }

        /// <summary>
        /// Gets the current color data (hexes and optional assetId) for the given kind. Use from assemblies that cannot reference IColor (e.g. SDK).
        /// </summary>
        public static async UniTask<(Color[] hexes, string assetId)> GetColorDataAsync(GeniesAvatar avatar, AvatarColorKind colorKind, CancellationToken cancellationToken = default)
        {
            var color = await GetColorAsync(avatar, colorKind, cancellationToken);
            if (color == null)
            {
                return (null, null);
            }
            return (color.Hexes, color.AssetId);
        }

        /// <summary>
        /// Equips a hair style on the avatar using WearableAssetInfo
        /// </summary>
        /// <param name="avatar">The avatar to equip the makeup on.</param>
        /// <param name="asset">The default inventory asset (e.g. makeup) to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public static async UniTask EquipHairAsync(GeniesAvatar avatar, WearableAssetInfo asset, CancellationToken cancellationToken = default)
        {
            if (asset != null && !string.IsNullOrEmpty(asset.AssetId))
            {
                await EquipHairByHairAssetIdAsync(avatar, asset.AssetId, cancellationToken);
            }
        }

        /// <summary>
        /// Equips a hair style on the avatar using hair asset id.
        /// </summary>
        /// <param name="avatar">The avatar to equip the hair style on</param>
        /// <param name="hairAssetId">The ID of the hair asset to equip</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask</returns>
        public static async UniTask EquipHairByHairAssetIdAsync(GeniesAvatar avatar, string hairAssetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipHairAsync(avatar, hairAssetId, cancellationToken);
                HairEquipped?.Invoke(hairAssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip hair style: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a hair style from the avatar.
        /// For Hair/FacialHair: finds the equipped hair asset and unequips it. For Eyebrows/Eyelashes: equips the faceblendshape "none" asset (subcategory brow/eyelash).
        /// </summary>
        /// <param name="avatar">The avatar to unequip the hair style from</param>
        /// <param name="hairType">The type of hair to unequip (Hair, FacialHair, Eyebrows, or Eyelashes—Eyebrows/Eyelashes are no-op)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask</returns>
        public static async UniTask UnEquipHairAsync(GeniesAvatar avatar, HairType hairType, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.UnEquipHairAsync(avatar, hairType, cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip hair style: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a given tattoo on the avatar at the given slot index.
        /// </summary>
        public static async UniTask EquipTattooAsync(GeniesAvatar avatar, AvatarTattooInfo tattooInfo, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.EquipTattooAsync(avatar, tattooInfo, tattooSlot, cancellationToken);
                TattooEquipped?.Invoke(tattooInfo.AssetId);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip tattoo: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips the tattoo on the avatar at the given tattoo slot.
        /// </summary>
        public static async UniTask<string> UnEquipTattooAsync(GeniesAvatar avatar, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                string unequippedTattooId = await avatarEditorSdkService.UnEquipTattooAsync(avatar, tattooSlot, cancellationToken);
                if (!string.IsNullOrEmpty(unequippedTattooId))
                {
                    TattooUnequipped?.Invoke(unequippedTattooId);
                }
                return unequippedTattooId;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip tattoo: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the native avatar body preset from NativeAvatarBodyPresetInfo (wrapper for GSkelModifierPreset).
        /// </summary>
        /// <param name="avatar">The avatar to modify.</param>
        /// <param name="presetInfo">The body preset data (Name, StartingBodyVariation, Attributes). Null or missing name is ignored.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public static async UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, NativeAvatarBodyPresetInfo presetInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (presetInfo == null || string.IsNullOrEmpty(presetInfo.Name))
                {
                    CrashReporter.LogError("Body preset info and name are required.");
                    return;
                }
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var preset = ScriptableObject.CreateInstance<GSkelModifierPreset>();
                preset.Name = presetInfo.Name;
                preset.StartingBodyVariation = presetInfo.StartingBodyVariation ?? string.Empty;
                preset.GSkelModValues = presetInfo.Attributes?.Select(a => new GSkelModValue { Name = a.Name, Value = a.Value }).ToList() ?? new List<GSkelModValue>();

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SetNativeAvatarBodyPresetAsync(avatar, preset, cancellationToken);
                BodyPresetSet?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set body preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current native avatar body preset as NativeAvatarBodyPresetInfo. Returns null if init fails or preset is null.
        /// </summary>
        public static async UniTask<NativeAvatarBodyPresetInfo> GetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, CancellationToken cancellationToken = default)
        {
            if (await InitializeAsync() is false)
            {
                return null;
            }

            var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
            var preset = avatarEditorSdkService.GetNativeAvatarBodyPreset(avatar);
            return NativeAvatarBodyPresetInfo.FromPreset(preset);
        }

        public static async UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SetAvatarBodyTypeAsync(avatar, genderType, bodySize, cancellationToken);
                BodyTypeSet?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar body type: {ex.Message}");
            }
        }

        public static async UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                await avatarEditorSdkService.SaveAvatarDefinitionAsync(avatar);
                AvatarDefinitionSaved?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition: {ex.Message}");
            }
        }

        public static async UniTask SaveAvatarDefinitionLocallyAsync(GeniesAvatar avatar, string profileId = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                avatarEditorSdkService.SaveAvatarDefinitionLocally(avatar, profileId);
                AvatarDefinitionSavedLocally?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition locally: {ex.Message}");
            }
        }

        public static async UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var avatar = await avatarEditorSdkService.LoadFromLocalAvatarDefinitionAsync(profileId, cancellationToken);
                if (avatar == null)
                {
                    CrashReporter.LogError($"Failed to load avatar with profileId: {profileId}");
                    return null;
                }

                AvatarLoadedForEditing?.Invoke();
                return avatar;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar from definition: {ex.Message}");
                return null;
            }
        }

        public static async UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = await GetOrCreateAvatarEditorSdkInstance();
                var avatar = await avatarEditorSdkService.LoadFromLocalGameObjectAsync(profileId, cancellationToken);
                if (avatar == null)
                {
                    CrashReporter.LogError($"Failed to load avatar from game object: {profileId}");
                    return null;
                }

                AvatarLoadedForEditing?.Invoke();
                return avatar;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition: {ex.Message}");
                return null;
            }
        }

        public static async UniTask SetEditorSaveOptionAsync(AvatarSaveOption saveOption)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    CrashReporter.LogError("AvatarEditorSdkService not found. Cannot set save option.");
                    return;
                }

                avatarEditorSdkService.SetEditorSaveOption(saveOption);
                EditorSaveOptionSet?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save option: {ex.Message}");
            }
        }

        public static async UniTask SetEditorSaveOptionAsync(AvatarSaveOption saveOption, string profileId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    throw new NullReferenceException("AvatarEditorSdkService not found");
                }

                avatarEditorSdkService.SetEditorSaveOption(saveOption, profileId);
                EditorSaveOptionSet?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save option: {ex.Message}");
            }
        }

        public static async UniTask SetEditorSaveSettingsAsync(AvatarSaveSettings saveSettings)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    throw new NullReferenceException("AvatarEditorSdkService not found");
                }

                avatarEditorSdkService.SetEditorSaveSettings(saveSettings);
                EditorSaveSettingsSet?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set editor save settings: {ex.Message}");
            }
        }

        public static async UniTask SetSaveAndExitButtonStatusAsync(bool enableSaveButton, bool enableExitButton)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize AvatarEditorSDK");
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    throw new NullReferenceException("AvatarEditorSdkService not found");
                }

                avatarEditorSdkService.SetSaveAndExitButtonStatus(enableSaveButton, enableExitButton);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set Save and Exit ActionBarFlags: {ex.Message}");
            }
        }

        public static void SetSaveAndExitButtonStatus(bool enableSaveButton, bool enableExitButton)
        {
            try
            {
                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    CrashReporter.LogWarning("AvatarEditorSdkService not found. Make sure the SDK is initialized before calling this method.");
                    return;
                }

                avatarEditorSdkService.SetSaveAndExitButtonStatus(enableSaveButton, enableExitButton);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set Save and Exit ActionBarFlags: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets an avatar feature by equipping the specified asset. Provides a unified interface for modifying various facial features.
        /// </summary>
        /// <param name="avatar">The avatar to modify</param>
        /// <param name="feature">AvatarFeaturesInfo; the AssetId is equipped.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        public static async UniTask<bool> SetAvatarFeatureAsync(GeniesAvatar avatar, AvatarFeaturesInfo feature, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar?.Controller == null)
                {
                    CrashReporter.LogError("Avatar and controller are required to set avatar feature");
                    return false;
                }

                if (string.IsNullOrEmpty(feature?.AssetId))
                {
                    CrashReporter.LogError("Valid feature and AssetId is required to set avatar feature");
                    return false;
                }

                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    CrashReporter.LogWarning("AvatarEditorSdkService not found. Make sure the SDK is initialized before calling this method.");
                    return false;
                }

                await avatarEditorSdkService.EquipOutfitAsync(avatar, feature.AssetId, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar feature: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Modifies a single avatar feature stat. Value is clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The avatar to modify.</param>
        /// <param name="stat">The feature stat to set (e.g. AvatarFeatureStat.Nose_Width, AvatarFeatureStat.Body_NeckThickness).</param>
        /// <param name="value">The value to set (clamped between -1.0 and 1.0).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the update was applied, false if avatar/controller invalid or an error occurred.</returns>
        public static bool ModifyAvatarFeatureStat(GeniesAvatar avatar, AvatarFeatureStat stat, float value, CancellationToken cancellationToken = default)
        {
            try
            {
                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                return avatarEditorSdkService != null && avatarEditorSdkService.ModifyAvatarFeatureStat(avatar, stat, Mathf.Clamp(value, -1.0f, 1.0f), cancellationToken);

            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to modify avatar feature stat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Modifies multiple avatar feature stats at once. Values are clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The avatar to modify.</param>
        /// <param name="stats">Dictionary of AvatarFeatureStat to value. Null or empty is a no-op and returns true.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if all updates were applied (or stats was null/empty), false if avatar/controller invalid or an error occurred.</returns>
        public static bool ModifyAvatarFeatureStats(GeniesAvatar avatar, IReadOnlyDictionary<AvatarFeatureStat, float> stats, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar?.Controller == null)
                {
                    CrashReporter.LogError("Avatar and controller are required to modify avatar feature stats");
                    return false;
                }

                if (stats == null || stats.Count == 0)
                {
                    return true;
                }

                cancellationToken.ThrowIfCancellationRequested();
                bool result = true;
                foreach (var kvp in stats)
                {
                    result  &= ModifyAvatarFeatureStat(avatar, kvp.Key, kvp.Value, cancellationToken);
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to modify avatar feature stats: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets current values for a given avatar feature stat type (e.g. all nose stats, all body stats).
        /// </summary>
        /// <param name="avatar">The avatar to read from</param>
        /// <param name="statType">Which IAvatarFeatureStat category to return (Body, EyeBrows, Eyes, Jaw, Lips, Nose)</param>
        /// <returns>Dictionary of AvatarFeatureStat to current value (typically -1.0 to 1.0). Empty if avatar/controller is null.</returns>
        public static Dictionary<AvatarFeatureStat, float> GetAvatarFeatureStats(GeniesAvatar avatar, AvatarFeatureStatType statType)
        {
            try
            {
                var avatarEditorSdkService = ServiceManager.Get<IAvatarEditorSdkService>();
                if (avatarEditorSdkService == null)
                {
                    CrashReporter.LogWarning("AvatarEditorSdkService not found. Make sure the SDK is initialized before calling this method.");
                    return new Dictionary<AvatarFeatureStat, float>();
                }

                return avatarEditorSdkService.GetAvatarFeatureStats(avatar, statType);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get avatar feature stats: {ex.Message}");
                return new Dictionary<AvatarFeatureStat, float>();
            }
        }

        #endregion
    }
}
