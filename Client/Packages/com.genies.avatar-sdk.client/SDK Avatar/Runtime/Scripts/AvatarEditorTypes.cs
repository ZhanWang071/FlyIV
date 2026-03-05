using System;
using System.Collections.Generic;
using Genies.AvatarEditor.Core;
using Genies.Refs;
using UnityEngine;

namespace Genies.Sdk
{
    /// <summary>
    /// Wrapper for wearable asset information
    /// </summary>
    [Serializable]
    public class WearableAssetInfo : IDisposable
    {
        private readonly string _assetId;
        private readonly string _name;
        private readonly string _category;
        private readonly Sprite _icon;
        private readonly Action _onDisposed;

        public string AssetId { get => _assetId; }
        public string Name { get => _name; }
        public string Category { get => _category; }
        public Sprite Icon { get => _icon; }

        public WearableAssetInfo(string assetId, string name, string category, Sprite icon, Action onDisposed)
        {
            _assetId = assetId;
            _name = name;
            _category = category;
            _icon = icon;
            _onDisposed = onDisposed;
        }

        public void Dispose()
        {
            _onDisposed?.Invoke();
        }

        /// <summary>
        /// Converts SDK WearableAssetInfo to internal AvatarEditor Core WearableAssetInfo type.
        /// </summary>
        internal static Genies.AvatarEditor.Core.WearableAssetInfo ToInternal(WearableAssetInfo sdkAsset)
        {
            var ret = new Genies.AvatarEditor.Core.WearableAssetInfo();
            ret.AssetId = sdkAsset.AssetId;
            ret.Name = sdkAsset.Name;
            ret.Category = sdkAsset.Category;
            // When Core disposes ret.Icon (RemoveSpriteReference), run the SDK's onDisposed callback.
            ret.Icon = sdkAsset.Icon != null
                ? CreateRef.FromAny(sdkAsset.Icon, _ => sdkAsset._onDisposed?.Invoke())
                : default;

            return ret;
        }

        /// <summary>
        /// Creates from the internal AvatarEditor WearableAssetInfo type
        /// </summary>
        internal static WearableAssetInfo FromInternal(Genies.AvatarEditor.Core.WearableAssetInfo internalAsset)
        {
            return new WearableAssetInfo(
                internalAsset.AssetId,
                internalAsset.Name,
                internalAsset.Category,
                internalAsset.Icon.Item,
                internalAsset.Dispose
            );
        }

        /// <summary>
        /// Converts a list from internal types to SDK types
        /// </summary>
        internal static List<WearableAssetInfo> FromInternalList(List<Genies.AvatarEditor.Core.WearableAssetInfo> internalList)
        {
            var result = new List<WearableAssetInfo>();
            foreach (var item in internalList)
            {
                result.Add(FromInternal(item));
            }

            return result;
        }
    }

    /// <summary>
    /// Wrapper for default avatar base asset information (avatar features). Converts to/from Genies.AvatarEditor.Core.AvatarFeaturesInfo.
    /// </summary>
    [Serializable]
    public class AvatarFeaturesInfo : IDisposable
    {
        private readonly string _assetId;
        private readonly List<string> _subCategories;
        private readonly Sprite _icon;
        private readonly Action _onDisposed;

        public string AssetId { get => _assetId; }
        public List<string> SubCategories { get => _subCategories; }
        public Sprite Icon { get => _icon; }

        public AvatarFeaturesInfo(string assetId, List<string> subCategories, Sprite icon, Action onDisposed)
        {
            _assetId = assetId;
            _subCategories = subCategories;
            _icon = icon;
            _onDisposed = onDisposed;
        }

        public void Dispose()
        {
            _onDisposed?.Invoke();
        }

        /// <summary>
        /// Creates from the internal Core AvatarFeaturesInfo type.
        /// </summary>
        internal static AvatarFeaturesInfo FromInternal(Genies.AvatarEditor.Core.AvatarFeaturesInfo internalAsset)
        {
            return new AvatarFeaturesInfo(
                internalAsset.AssetId,
                internalAsset.SubCategories,
                internalAsset.Icon.Item,
                internalAsset.Dispose
            );
        }

        /// <summary>
        /// Converts a list from internal Core AvatarFeaturesInfo to SDK AvatarFeaturesInfo.
        /// </summary>
        internal static List<AvatarFeaturesInfo> FromInternalList(List<Genies.AvatarEditor.Core.AvatarFeaturesInfo> internalList)
        {
            var result = new List<AvatarFeaturesInfo>();
            foreach (var item in internalList)
            {
                result.Add(FromInternal(item));
            }

            return result;
        }

        /// <summary>
        /// Converts this SDK type to the internal Core AvatarFeaturesInfo type.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarFeaturesInfo ToInternal(AvatarFeaturesInfo sdkAsset)
        {
            if (sdkAsset == null)
            {
                return null;
            }

            return new Genies.AvatarEditor.Core.AvatarFeaturesInfo
            {
                AssetId = sdkAsset.AssetId,
                SubCategories = sdkAsset.SubCategories
            };
        }

        /// <summary>
        /// Converts a list from SDK AvatarFeaturesInfo to internal Core AvatarFeaturesInfo.
        /// </summary>
        internal static List<Genies.AvatarEditor.Core.AvatarFeaturesInfo> ToInternal(List<AvatarFeaturesInfo> sdkList)
        {
            if (sdkList == null)
            {
                return null;
            }

            var result = new List<Genies.AvatarEditor.Core.AvatarFeaturesInfo>(sdkList.Count);
            foreach (var item in sdkList)
            {
                var internalItem = ToInternal(item);
                if (internalItem != null)
                {
                    result.Add(internalItem);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Wrapper for default asset information from the inventory service (e.g. makeup, tattoos). Converts to/from Genies.AvatarEditor.Core.AvatarItemInfo.
    /// </summary>
    [Serializable]
    public class AvatarMakeupInfo : IDisposable
    {
        private readonly string _assetId;
        private readonly Sprite _icon;
        private readonly Action _onDisposed;

        public string AssetId { get => _assetId; }
        public Sprite Icon { get => _icon; }

        public void Dispose()
        {
            _onDisposed?.Invoke();
        }

        public AvatarMakeupInfo(string assetId, Sprite icon, Action onDisposed)
        {
            _assetId = assetId;
            _icon = icon;
            _onDisposed = onDisposed;
        }

        /// <summary>
        /// Converts this SDK type to the internal Core AvatarItemInfo type.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarMakeupInfo ToInternal(AvatarMakeupInfo item)
        {
            if (item == null)
            {
                return null;
            }

            return new Genies.AvatarEditor.Core.AvatarMakeupInfo
            {
                AssetId = item.AssetId
            };
        }

        /// <summary>
        /// Creates from the internal Core AvatarItemInfo type.
        /// </summary>
        internal static AvatarMakeupInfo FromInternal(Genies.AvatarEditor.Core.AvatarMakeupInfo internalAsset)
        {
            return new AvatarMakeupInfo(
                internalAsset.AssetId,
                internalAsset.Icon.Item,
                internalAsset.Dispose
            );
        }

        /// <summary>
        /// Converts a list from internal Core AvatarItemInfo to SDK AvatarItemInfo.
        /// </summary>
        internal static List<AvatarMakeupInfo> FromInternalList(List<Genies.AvatarEditor.Core.AvatarMakeupInfo> internalList)
        {
            var result = new List<AvatarMakeupInfo>();
            if (internalList == null)
            {
                return result;
            }

            foreach (var item in internalList)
            {
                result.Add(FromInternal(item));
            }
            return result;
        }
    }

    /// <summary>
    /// Wrapper for default asset information from the inventory service (e.g. makeup, tattoos). Converts to/from Genies.AvatarEditor.Core.AvatarItemInfo.
    /// </summary>
    [Serializable]
    public class AvatarTattooInfo : IDisposable
    {
        private readonly string _assetId;
        private readonly Sprite _icon;
        private readonly Action _onDisposed;

        public string AssetId { get => _assetId; }
        public Sprite Icon { get => _icon; }

        public void Dispose()
        {
            _onDisposed?.Invoke();
        }

        public AvatarTattooInfo(string assetId, Sprite icon, Action onDisposed)
        {
            _assetId = assetId;
            _icon = icon;
            _onDisposed = onDisposed;
        }

        /// <summary>
        /// Converts this SDK type to the internal Core AvatarItemInfo type.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarTattooInfo ToInternal(AvatarTattooInfo item)
        {
            if (item == null)
            {
                return null;
            }

            return new Genies.AvatarEditor.Core.AvatarTattooInfo
            {
                AssetId = item.AssetId
            };
        }

        /// <summary>
        /// Creates from the internal Core AvatarItemInfo type.
        /// </summary>
        internal static AvatarTattooInfo FromInternal(Genies.AvatarEditor.Core.AvatarTattooInfo internalAsset)
        {
            return new AvatarTattooInfo(
                internalAsset.AssetId,
                internalAsset.Icon.Item,
                internalAsset.Dispose
            );
        }

        /// <summary>
        /// Converts a list from internal Core AvatarItemInfo to SDK AvatarItemInfo.
        /// </summary>
        internal static List<AvatarTattooInfo> FromInternalList(List<Genies.AvatarEditor.Core.AvatarTattooInfo> internalList)
        {
            var result = new List<AvatarTattooInfo>();
            if (internalList == null)
            {
                return result;
            }

            foreach (var item in internalList)
            {
                result.Add(FromInternal(item));
            }
            return result;
        }
    }

    /// <summary>
    /// Single attribute (name/value) in a native avatar body preset. Mirrors GSkelModValue for SDK use.
    /// </summary>
    [Serializable]
    public struct NativeAvatarBodyPresetAttribute
    {
        private string _name;
        private float _valueField;

        public NativeAvatarBodyPresetAttribute(string name, float value)
        {
            _name = name;
            _valueField = value;
        }

        public string Name { get => _name; }
        public float Value { get => _valueField; }
    }

    /// <summary>
    /// Native avatar body preset data returned by GetNativeAvatarBodyPresetAsync. Mirrors GSkelModifierPreset for SDK use.
    /// </summary>
    [Serializable]
    public class NativeAvatarBodyPresetInfo
    {
        private readonly string _name;
        private readonly string _startingBodyVariation;
        private readonly List<NativeAvatarBodyPresetAttribute> _attributes;

        public string Name { get => _name; }
        public string StartingBodyVariation { get => _startingBodyVariation; }
        public List<NativeAvatarBodyPresetAttribute> Attributes { get => _attributes; }

        public NativeAvatarBodyPresetInfo(string name, string startingBodyVariation, List<NativeAvatarBodyPresetAttribute> attributes)
        {
            _name = name;
            _startingBodyVariation = startingBodyVariation;
            _attributes = attributes ?? new List<NativeAvatarBodyPresetAttribute>();
        }

        internal static class NativeAvatarBodyPresetInfoMapper
        {
            internal static NativeAvatarBodyPresetInfo FromInternal(Genies.AvatarEditor.Core.NativeAvatarBodyPresetInfo core)
            {
                if (core == null)
                {
                    return null;
                }

                var attributes = core.Attributes?.ConvertAll(a => new NativeAvatarBodyPresetAttribute(a.Name, a.Value))
                                 ?? new List<NativeAvatarBodyPresetAttribute>();
                return new NativeAvatarBodyPresetInfo(core.Name, core.StartingBodyVariation, attributes);
            }

            internal static Genies.AvatarEditor.Core.NativeAvatarBodyPresetInfo ToInternal(NativeAvatarBodyPresetInfo sdk)
            {
                if (sdk == null)
                {
                    return null;
                }

                var attributes = sdk.Attributes?.ConvertAll(a => new Genies.AvatarEditor.Core.NativeAvatarBodyPresetAttribute { Name = a.Name, Value = a.Value })
                                 ?? new List<Genies.AvatarEditor.Core.NativeAvatarBodyPresetAttribute>();
                return new Genies.AvatarEditor.Core.NativeAvatarBodyPresetInfo(sdk.Name, sdk.StartingBodyVariation, attributes);
            }
        }
    }

    /// <summary>
    /// Public interface for avatar color types used with SetColorAsync and returned by GetColorAsync.
    /// Use CreateHairColor, CreateSkinColor, etc. to build instances.
    /// </summary>
    public interface IAvatarColor
    {
        AvatarColorKind Kind { get; }
        Color[] Hexes { get; }
        string AssetId { get; }
        bool IsCustom { get; }
    }

    /// <summary>
    /// Hair color (base + R,G,B gradient). Use CreateHairColor or GetColorAsync(AvatarColorKind.Hair).
    /// </summary>
    public struct HairColor : IAvatarColor
    {
        private readonly Color _base;
        private readonly Color _r, _g, _b;
        public HairColor(Color baseColor, Color colorR, Color colorG, Color colorB) { _base = baseColor; _r = colorR; _g = colorG; _b = colorB; }
        public AvatarColorKind Kind => AvatarColorKind.Hair;
        public Color[] Hexes => new[] { _base, _r, _g, _b };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Facial hair color (base + R,G,B gradient). Use CreateFacialHairColor or GetColorAsync(AvatarColorKind.FacialHair).
    /// </summary>
    public struct FacialHairColor : IAvatarColor
    {
        private readonly Color _base;
        private readonly Color _r, _g, _b;
        public FacialHairColor(Color baseColor, Color colorR, Color colorG, Color colorB) { _base = baseColor; _r = colorR; _g = colorG; _b = colorB; }
        public AvatarColorKind Kind => AvatarColorKind.FacialHair;
        public Color[] Hexes => new[] { _base, _r, _g, _b };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Eyebrow color (base + R,G,B). Use CreateEyeBrowsColor or GetColorAsync(AvatarColorKind.EyeBrows).
    /// </summary>
    public struct EyeBrowsColor : IAvatarColor
    {
        private readonly Color _base;
        private readonly Color _base2;
        public EyeBrowsColor(Color baseColor, Color baseColor2) { _base = baseColor; _base2 = baseColor2; }
        public AvatarColorKind Kind => AvatarColorKind.EyeBrows;
        public Color[] Hexes => new[] { _base, _base2 };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Eyelash color (base + R,G,B). Use CreateEyeLashColor or GetColorAsync(AvatarColorKind.EyeLash).
    /// </summary>
    public struct EyeLashColor : IAvatarColor
    {
        private readonly Color _base;
        private readonly Color _base2;
        public EyeLashColor(Color baseColor, Color baseColor2) { _base = baseColor; _base2 = baseColor2; }
        public AvatarColorKind Kind => AvatarColorKind.EyeLash;
        public Color[] Hexes => new[] { _base, _base2 };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Skin color (single color). Use CreateSkinColor or GetColorAsync(AvatarColorKind.Skin).
    /// </summary>
    public struct SkinColor : IAvatarColor
    {
        private readonly Color _color;
        public SkinColor(Color color) { _color = color; }
        public AvatarColorKind Kind => AvatarColorKind.Skin;
        public Color[] Hexes => new[] { _color };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Eye color by asset ID (equipped via outfit). Use CreateEyeColor or GetColorAsync(AvatarColorKind.Eyes).
    /// </summary>
    public struct EyeColor : IAvatarColor
    {
        private readonly string _assetId;
        private readonly Color _base1;
        private readonly Color _base2;
        public EyeColor(string assetId, Color baseColor1, Color baseColor2) { _assetId = assetId; _base1 = baseColor1; _base2 = baseColor2; }
        public AvatarColorKind Kind => AvatarColorKind.Eyes;
        public Color[] Hexes => new[] { _base1, _base2 };
        public string AssetId => _assetId;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Makeup color (base + R,G,B gradient). Use MakeupColor or GetColorAsync(AvatarColorKind.Makeup).
    /// </summary>
    public struct MakeupColor : IAvatarColor
    {
        private readonly Color _base;
        private readonly Color _r, _g, _b;
        private readonly AvatarMakeupCategory _category;

        public MakeupColor(Color baseColor, Color colorR, Color colorG, Color colorB)
            : this(AvatarMakeupCategory.Stickers, baseColor, colorR, colorG, colorB) { }

        public MakeupColor(AvatarMakeupCategory category, Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            _category = category;
            _base = baseColor;
            _r = colorR;
            _g = colorG;
            _b = colorB;
        }

        /// <summary>
        /// Creates a MakeupColor from an AvatarColorKind (e.g. MakeupLipstick, MakeupBlush). Uses AvatarColorKindMakeupCategoryMapper to get the category.
        /// </summary>
        public MakeupColor(AvatarColorKind colorKind, Color baseColor, Color colorR, Color colorG, Color colorB)
            : this(AvatarColorKindMakeupCategoryMapper.ToMakeupCategory(colorKind), baseColor, colorR, colorG, colorB)
        {
        }

        /// <summary>
        /// The makeup category this color applies to (e.g. Lipstick, Blush, Eyeshadow).
        /// </summary>
        public AvatarMakeupCategory Category => _category;

        /// <summary>
        /// The color kind for this makeup (e.g. MakeupLipstick, MakeupBlush). Derived from Category via AvatarColorKindMakeupCategoryMapper.
        /// </summary>
        public AvatarColorKind Kind => AvatarColorKindMakeupCategoryMapper.ToAvatarColorKind(_category);
        public Color[] Hexes => new[] { _base, _r, _g, _b };
        public string AssetId => null;
        public bool IsCustom => false;
    }

    /// <summary>
    /// Converts ColorType and color data into an IAvatarColor suitable for SetColorAsync.
    /// If colors is null or empty, returns an IAvatarColor with clear color (Color.clear).
    /// </summary>
    public static class ColorMapper
    {
        /// <summary>
        /// Converts a ColorType and list of color values into an IAvatarColor instance suitable for SetColorAsync.
        /// </summary>
        /// <param name="colorType">The color kind (Eyes, Hair, FacialHair, Skin, Eyebrow, Eyelash). Makeup is not supported and will throw.</param>
        /// <param name="colors">Color values: one for Skin, four (base, R, G, B) for Hair/FacialHair/Eyebrow/Eyelash. If null or empty, a clear IAvatarColor is returned.</param>
        /// <param name="assetId">Required when colorType is Eyes and colors is non-empty. When colors is empty, Eyes returns EyeColor with empty assetId. Ignored for other types.</param>
        /// <returns>An IAvatarColor of the appropriate concrete type.</returns>
        public static IAvatarColor ToIColorValue(ColorType colorType, List<Color> colors, string assetId = null)
        {
            bool isEmpty = colors == null || colors.Count == 0;
            Color clear = Color.clear;

            switch (colorType)
            {
                case ColorType.Skin:
                    return new SkinColor(isEmpty ? clear : colors[0]);

                case ColorType.Hair:
                case ColorType.FacialHair:
                case ColorType.Eyebrow:
                case ColorType.Eyelash:
                case ColorType.Eyes:
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
                        if (colorType == ColorType.Eyes)
                        {
                            return new EyeColor(assetId ?? string.Empty, c0, c1);
                        }

                        if (colorType == ColorType.Hair)
                        {
                            return new HairColor(c0, c1, c2, c3);
                        }

                        if (colorType == ColorType.FacialHair)
                        {
                            return new FacialHairColor(c0, c1, c2, c3);
                        }

                        if (colorType == ColorType.Eyebrow)
                        {
                            return new EyeBrowsColor(c0, c1);
                        }

                        if (colorType == ColorType.Eyelash)
                        {
                            return new EyeLashColor(c0, c1);
                        }

                        return new MakeupColor(c0, c1, c2, c3);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Unsupported ColorType.");
            }
        }
    }

    /// <summary>
    /// Asset types available in the avatar system
    /// </summary>
    public enum AssetType
    {
        WardrobeGear = 0,
        AvatarBase = 1,
        AvatarMakeup = 2,
        Flair = 3,
        AvatarEyes = 4,
        ColorPreset = 5,
        ImageLibrary = 6,
        AnimationLibrary = 7,
        Avatar = 8,
        Decor = 9,
        ModelLibrary = 10
    }

    /// <summary>
    /// Avatar feature category for GetDefaultAvatarFeaturesByCategory (e.g. Eyes, Jaw, Lips, Nose). Mirrors Genies.Inventory.AvatarBaseCategory for SDK use.
    /// </summary>
    public enum AvatarFeatureCategory
    {
        Lips = 1,
        Jaw = 2,
        Nose = 3,
        Eyes = 4
    }

    /// <summary>
    /// Makeup category for GetDefaultMakeupByCategoryAsync (e.g. Stickers, Lipstick, Freckles, FaceGems, Eyeshadow, Blush). Wrapper for Genies.AvatarEditor.Core.MakeupCategory for SDK use.
    /// </summary>
    public enum AvatarMakeupCategory
    {
        Stickers = 0,
        Lipstick = 1,
        Freckles = 2,
        FaceGems = 3,
        Eyeshadow = 4,
        Blush = 5
    }

    /// <summary>
    /// Maps between AvatarColorKind (makeup variants) and AvatarMakeupCategory for use in MakeupColor and color APIs.
    /// </summary>
    public static class AvatarColorKindMakeupCategoryMapper
    {
        /// <summary>
        /// Converts AvatarColorKind to AvatarMakeupCategory when the kind is a makeup kind (MakeupStickers, MakeupLipstick, etc.).
        /// Returns AvatarMakeupCategory.None for non-makeup kinds.
        /// </summary>
        public static AvatarMakeupCategory ToMakeupCategory(AvatarColorKind colorKind)
        {
            return colorKind switch
            {
                AvatarColorKind.MakeupStickers => AvatarMakeupCategory.Stickers,
                AvatarColorKind.MakeupLipstick => AvatarMakeupCategory.Lipstick,
                AvatarColorKind.MakeupFreckles => AvatarMakeupCategory.Freckles,
                AvatarColorKind.MakeupFaceGems => AvatarMakeupCategory.FaceGems,
                AvatarColorKind.MakeupEyeshadow => AvatarMakeupCategory.Eyeshadow,
                AvatarColorKind.MakeupBlush => AvatarMakeupCategory.Blush,
                _ => AvatarMakeupCategory.Stickers
            };
        }


        /// <summary>
        /// Converts AvatarMakeupCategory to the corresponding AvatarColorKind for use with SetColorAsync/GetColorAsync.
        /// Returns AvatarColorKind.MakeupLipstick for None (default makeup kind).
        /// </summary>
        public static AvatarColorKind ToAvatarColorKind(AvatarMakeupCategory category)
        {
            return category switch
            {
                AvatarMakeupCategory.Stickers => AvatarColorKind.MakeupStickers,
                AvatarMakeupCategory.Lipstick => AvatarColorKind.MakeupLipstick,
                AvatarMakeupCategory.Freckles => AvatarColorKind.MakeupFreckles,
                AvatarMakeupCategory.FaceGems => AvatarColorKind.MakeupFaceGems,
                AvatarMakeupCategory.Eyeshadow => AvatarColorKind.MakeupEyeshadow,
                AvatarMakeupCategory.Blush => AvatarColorKind.MakeupBlush,
                _ => AvatarColorKind.MakeupLipstick
            };
        }
    }

    /// <summary>
    /// Gender types for avatar body configuration
    /// </summary>
    public enum GenderType
    {
        Male,
        Female,
        Androgynous
    }

    /// <summary>
    /// Body size types for avatar body configuration
    /// </summary>
    public enum BodySize
    {
        Skinny,
        Medium,
        Heavy
    }

    /// <summary>
    /// Wardrobe subcategory types for filtering wearable assets
    /// </summary>
    public enum WardrobeSubcategory
    {
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
    /// Wearable category types (non-hair wardrobe subcategories). Excludes hair, eyebrows, eyelashes, facialHair.
    /// </summary>
    public enum WearablesCategory
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
    /// Maps to WearablesCategory when calling internal APIs.
    /// </summary>
    public enum UserWearablesCategory
    {
        Hoodie,
        Shirt,
        Pants,
        Shorts
    }

    /// <summary>
    /// Eyebrow statistics that can be modified on the avatar
    /// </summary>
    public enum EyeBrowsStats
    {
        Thickness,          // Thickness of the eyebrows
        Length,             // Length of the eyebrows
        VerticalPosition,   // Vertical position of the eyebrows
        Spacing             // Spacing between eyebrows
    }

    /// <summary>
    /// Eye statistics that can be modified on the avatar
    /// </summary>
    public enum EyeStats
    {
        Size,               // Size of the eyes
        VerticalPosition,   // Vertical position of the eyes
        Spacing,            // Spacing between eyes
        Rotation            // Rotation of the eyes
    }

    /// <summary>
    /// Jaw statistics that can be modified on the avatar
    /// </summary>
    public enum JawStats
    {
        Width,   // Width of the jaw
        Length   // Length of the jaw
    }

    /// <summary>
    /// Lip statistics that can be modified on the avatar
    /// </summary>
    public enum LipsStats
    {
        Width,             // Width of the lips
        Fullness,          // Fullness/thickness of the lips
        VerticalPosition   // Vertical position of the lips
    }

    /// <summary>
    /// Nose statistics that can be modified on the avatar
    /// </summary>
    public enum NoseStats
    {
        Width,             // Width of the nose
        Length,            // Length of the nose
        VerticalPosition,  // Vertical position of the nose
        Tilt,              // Tilt/angle of the nose
        Projection         // Projection/protrusion of the nose
    }

    /// <summary>
    /// Body statistics that can be modified on the avatar
    /// </summary>
    public enum BodyStats
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
    /// Public interface for avatar feature stats used with ModifyAvatarFeatureStatsAsync. Use CreateEyeBrowsStat, CreateEyeStat, etc. to build instances.
    /// </summary>
    public interface IAvatarFeatureStat
    {
        string GetAttributeId();
        string GetFeatureName();
    }

    /// <summary>Attribute ID constants for avatar feature stats (mirrors GenieBodyAttribute for SDK use).</summary>
    internal static class AvatarFeatureStatAttributeIds
    {
        internal const string BrowThickness = "BrowThickness";
        internal const string BrowLength = "BrowLength";
        internal const string BrowPositionVert = "BrowPositionVert";
        internal const string BrowSpacing = "BrowSpacing";
        internal const string EyeSize = "EyeSize";
        internal const string EyePositionVert = "EyePositionVert";
        internal const string EyeSpacing = "EyeSpacing";
        internal const string EyeTilt = "EyeTilt";
        internal const string JawWidth = "JawWidth";
        internal const string JawLength = "JawLength";
        internal const string LipWidth = "LipWidth";
        internal const string LipFullness = "LipFullness";
        internal const string LipPositionVert = "LipPositionVert";
        internal const string NoseWidth = "NoseWidth";
        internal const string NoseHeight = "NoseHeight";
        internal const string NosePositionVert = "NosePositionVert";
        internal const string NoseTilt = "NoseTilt";
        internal const string NoseProjection = "NoseProjection";
        internal const string WeightArms = "WeightArms";
        internal const string Belly = "Belly";
        internal const string WeightUpperTorso = "WeightUpperTorso";
        internal const string WeightLowerTorso = "WeightLowerTorso";
        internal const string WeightLegs = "WeightLegs";
        internal const string WeightHeadNeck = "WeightHeadNeck";
        internal const string ShoulderSize = "ShoulderSize";
        internal const string Waist = "Waist";
        internal const string HipSize = "HipSize";
    }

    /// <summary>Eyebrow stat for ModifyAvatarFeatureStatsAsync. Use CreateEyeBrowsStat.</summary>
    public struct EyeBrowsStat : IAvatarFeatureStat
    {
        private readonly EyeBrowsStats _value;
        public EyeBrowsStat(EyeBrowsStats value) { _value = value; }
        public EyeBrowsStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            EyeBrowsStats.Thickness => AvatarFeatureStatAttributeIds.BrowThickness,
            EyeBrowsStats.Length => AvatarFeatureStatAttributeIds.BrowLength,
            EyeBrowsStats.VerticalPosition => AvatarFeatureStatAttributeIds.BrowPositionVert,
            EyeBrowsStats.Spacing => AvatarFeatureStatAttributeIds.BrowSpacing,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid eyebrow stat")
        };
        public string GetFeatureName() => "eyebrow";
    }

    /// <summary>Eye stat for ModifyAvatarFeatureStatsAsync. Use CreateEyeStat.</summary>
    public struct EyeStat : IAvatarFeatureStat
    {
        private readonly EyeStats _value;
        public EyeStat(EyeStats value) { _value = value; }
        public EyeStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            EyeStats.Size => AvatarFeatureStatAttributeIds.EyeSize,
            EyeStats.VerticalPosition => AvatarFeatureStatAttributeIds.EyePositionVert,
            EyeStats.Spacing => AvatarFeatureStatAttributeIds.EyeSpacing,
            EyeStats.Rotation => AvatarFeatureStatAttributeIds.EyeTilt,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid eye stat")
        };
        public string GetFeatureName() => "eye";
    }

    /// <summary>Jaw stat for ModifyAvatarFeatureStatsAsync. Use CreateJawStat.</summary>
    public struct JawStat : IAvatarFeatureStat
    {
        private readonly JawStats _value;
        public JawStat(JawStats value) { _value = value; }
        public JawStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            JawStats.Width => AvatarFeatureStatAttributeIds.JawWidth,
            JawStats.Length => AvatarFeatureStatAttributeIds.JawLength,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid jaw stat")
        };
        public string GetFeatureName() => "jaw";
    }

    /// <summary>Lip stat for ModifyAvatarFeatureStatsAsync. Use CreateLipsStat.</summary>
    public struct LipsStat : IAvatarFeatureStat
    {
        private readonly LipsStats _value;
        public LipsStat(LipsStats value) { _value = value; }
        public LipsStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            LipsStats.Width => AvatarFeatureStatAttributeIds.LipWidth,
            LipsStats.Fullness => AvatarFeatureStatAttributeIds.LipFullness,
            LipsStats.VerticalPosition => AvatarFeatureStatAttributeIds.LipPositionVert,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid lip stat")
        };
        public string GetFeatureName() => "lip";
    }

    /// <summary>Nose stat for ModifyAvatarFeatureStatsAsync. Use CreateNoseStat.</summary>
    public struct NoseStat : IAvatarFeatureStat
    {
        private readonly NoseStats _value;
        public NoseStat(NoseStats value) { _value = value; }
        public NoseStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            NoseStats.Width => AvatarFeatureStatAttributeIds.NoseWidth,
            NoseStats.Length => AvatarFeatureStatAttributeIds.NoseHeight,
            NoseStats.VerticalPosition => AvatarFeatureStatAttributeIds.NosePositionVert,
            NoseStats.Tilt => AvatarFeatureStatAttributeIds.NoseTilt,
            NoseStats.Projection => AvatarFeatureStatAttributeIds.NoseProjection,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid nose stat")
        };
        public string GetFeatureName() => "nose";
    }

    /// <summary>Body stat for ModifyAvatarFeatureStatsAsync. Use CreateBodyStat.</summary>
    public struct BodyStat : IAvatarFeatureStat
    {
        private readonly BodyStats _value;
        public BodyStat(BodyStats value) { _value = value; }
        public BodyStats Value => _value;
        public string GetAttributeId() => _value switch
        {
            BodyStats.ArmsThickness => AvatarFeatureStatAttributeIds.WeightArms,
            BodyStats.BellyFullness => AvatarFeatureStatAttributeIds.Belly,
            BodyStats.ChestBustline => AvatarFeatureStatAttributeIds.WeightUpperTorso,
            BodyStats.HipsThickness => AvatarFeatureStatAttributeIds.WeightLowerTorso,
            BodyStats.LegsThickness => AvatarFeatureStatAttributeIds.WeightLegs,
            BodyStats.NeckThickness => AvatarFeatureStatAttributeIds.WeightHeadNeck,
            BodyStats.ShoulderBroadness => AvatarFeatureStatAttributeIds.ShoulderSize,
            BodyStats.WaistThickness => AvatarFeatureStatAttributeIds.Waist,
            BodyStats.HipSize => AvatarFeatureStatAttributeIds.HipSize,
            _ => throw new ArgumentOutOfRangeException(nameof(_value), _value, "Invalid body stat")
        };
        public string GetFeatureName() => "body";
    }

    /// <summary>
    /// Identifies which avatar feature stat category to query (Body, EyeBrows, Eyes, Jaw, Lips, Nose).
    /// Used with GetAllAvatarFeatureStats to retrieve all stat values for that category.
    /// </summary>
    public enum AvatarFeatureStatType
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
    public enum AvatarFeatureStat
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
    /// Identifies which IColor type to get (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor).
    /// Used with GetColorAsync to return the corresponding color value from the avatar.
    /// </summary>
    public enum AvatarColorKind
    {
        Hair,           // HairColor
        FacialHair,     // FacialHairColor
        EyeBrows,       // EyeBrowsColor
        EyeLash,        // EyeLashColor
        Skin,           // SkinColor
        Eyes,           // EyeColor
        MakeupStickers, // Stickers colors
        MakeupLipstick, // Lipstick colors
        MakeupFreckles, // Freckles colors
        MakeupFaceGems, // Facegems colors
        MakeupEyeshadow,// Eye shadow colors
        MakeupBlush     // Blush colors
    }

    /// <summary>
    /// Hair/flair type for color modification (hair, facial hair, eyebrows, or eyelashes).
    /// </summary>
    public enum HairType
    {
        Hair,        // Regular hair on the head
        FacialHair,  // Facial hair (beard, mustache, etc.)
        Eyebrows,    // Eyebrow colors
        Eyelashes    // Eyelash colors
    }

    /// <summary>
    /// Source type for color preset retrieval (All, User, or Default).
    /// </summary>
    public enum ColorSource
    {
        All,      // Returns both user and default colors
        User,     // Returns only user-created custom colors
        Default   // Returns only default/preset colors
    }

    /// <summary>
    /// Source type for wearable asset retrieval (All, User, or Default).
    /// </summary>
    public enum WearableAssetSource
    {
        All,      // Returns both user and default wearable assets
        User,     // Returns only user-owned wearable assets
        Default   // Returns only default wearable assets
    }

    /// <summary>
    /// Type of color preset to retrieve.
    /// </summary>
    public enum ColorType
    {
        Eyes,           // Eye colors
        Hair,           // Regular hair colors
        FacialHair,     // Facial hair colors (beard, mustache, etc.)
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
    public enum UserColorType
    {
        Hair,        // Regular hair colors
        Eyebrow,     // Eyebrow colors
        Eyelash      // Eyelash colors
    }

    /// <summary>
    /// Feature type for avatar feature modification (eyes, jawline, lips, nose, eyebrows, eyelashes, etc.).
    /// </summary>
    public enum FeatureType
    {
        Eyes,        // Eye blend shape assets
        EyeColor,    // Eye color (not yet implemented)
        Jawline,     // Jaw blend shape assets
        Lips,        // Lip blend shape assets
        Nose,        // Nose blend shape assets
        EyeBrows,    // Eyebrow blend shape assets
        EyeLashes   // Eyelash blend shape assets
    }

    /// <summary>
    /// Internal utility class for mapping SDK enums to internal assembly enums.
    /// This provides stable mapping that doesn't rely on enum ordinal positions.
    /// </summary>
    internal static class EnumMapper
    {
        /// <summary>
        /// Maps SDK GenderType to AvatarEditor GenderType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.GenderType ToInternal(GenderType genderType)
        {
            return genderType switch
            {
                GenderType.Male => Genies.AvatarEditor.Core.GenderType.Male,
                GenderType.Female => Genies.AvatarEditor.Core.GenderType.Female,
                GenderType.Androgynous => Genies.AvatarEditor.Core.GenderType.Androgynous,
                _ => throw new System.ArgumentException($"Unknown GenderType: {genderType}")
            };
        }

        /// <summary>
        /// Maps SDK BodySize to AvatarEditor BodySize using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.BodySize ToInternal(BodySize bodySize)
        {
            return bodySize switch
            {
                BodySize.Skinny => Genies.AvatarEditor.Core.BodySize.Skinny,
                BodySize.Medium => Genies.AvatarEditor.Core.BodySize.Medium,
                BodySize.Heavy => Genies.AvatarEditor.Core.BodySize.Heavy,
                _ => throw new System.ArgumentException($"Unknown BodySize: {bodySize}")
            };
        }

        /// <summary>
        /// Maps SDK WardrobeSubcategory to AvatarEditor WardrobeSubcategory using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.WardrobeSubcategory ToInternal(WardrobeSubcategory subcategory)
        {
            return subcategory switch
            {
                WardrobeSubcategory.hair => Genies.AvatarEditor.Core.WardrobeSubcategory.hair,
                WardrobeSubcategory.eyebrows => Genies.AvatarEditor.Core.WardrobeSubcategory.eyebrows,
                WardrobeSubcategory.eyelashes => Genies.AvatarEditor.Core.WardrobeSubcategory.eyelashes,
                WardrobeSubcategory.facialHair => Genies.AvatarEditor.Core.WardrobeSubcategory.facialHair,
                WardrobeSubcategory.underwearTop => Genies.AvatarEditor.Core.WardrobeSubcategory.underwearTop,
                WardrobeSubcategory.hoodie => Genies.AvatarEditor.Core.WardrobeSubcategory.hoodie,
                WardrobeSubcategory.shirt => Genies.AvatarEditor.Core.WardrobeSubcategory.shirt,
                WardrobeSubcategory.jacket => Genies.AvatarEditor.Core.WardrobeSubcategory.jacket,
                WardrobeSubcategory.dress => Genies.AvatarEditor.Core.WardrobeSubcategory.dress,
                WardrobeSubcategory.pants => Genies.AvatarEditor.Core.WardrobeSubcategory.pants,
                WardrobeSubcategory.shorts => Genies.AvatarEditor.Core.WardrobeSubcategory.shorts,
                WardrobeSubcategory.skirt => Genies.AvatarEditor.Core.WardrobeSubcategory.skirt,
                WardrobeSubcategory.underwearBottom => Genies.AvatarEditor.Core.WardrobeSubcategory.underwearBottom,
                WardrobeSubcategory.socks => Genies.AvatarEditor.Core.WardrobeSubcategory.socks,
                WardrobeSubcategory.shoes => Genies.AvatarEditor.Core.WardrobeSubcategory.shoes,
                WardrobeSubcategory.bag => Genies.AvatarEditor.Core.WardrobeSubcategory.bag,
                WardrobeSubcategory.bracelet => Genies.AvatarEditor.Core.WardrobeSubcategory.bracelet,
                WardrobeSubcategory.earrings => Genies.AvatarEditor.Core.WardrobeSubcategory.earrings,
                WardrobeSubcategory.glasses => Genies.AvatarEditor.Core.WardrobeSubcategory.glasses,
                WardrobeSubcategory.hat => Genies.AvatarEditor.Core.WardrobeSubcategory.hat,
                WardrobeSubcategory.mask => Genies.AvatarEditor.Core.WardrobeSubcategory.mask,
                WardrobeSubcategory.watch => Genies.AvatarEditor.Core.WardrobeSubcategory.watch,
                WardrobeSubcategory.all => Genies.AvatarEditor.Core.WardrobeSubcategory.all,
                _ => throw new System.ArgumentException($"Unknown WardrobeSubcategory: {subcategory}")
            };
        }

        /// <summary>
        /// Maps SDK WearablesCategory to AvatarEditor WearablesCategory using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.WearablesCategory ToInternal(WearablesCategory category)
        {
            return category switch
            {
                WearablesCategory.Hoodie => Genies.AvatarEditor.Core.WearablesCategory.Hoodie,
                WearablesCategory.Shirt => Genies.AvatarEditor.Core.WearablesCategory.Shirt,
                WearablesCategory.Dress => Genies.AvatarEditor.Core.WearablesCategory.Dress,
                WearablesCategory.Pants => Genies.AvatarEditor.Core.WearablesCategory.Pants,
                WearablesCategory.Shorts => Genies.AvatarEditor.Core.WearablesCategory.Shorts,
                WearablesCategory.Skirt => Genies.AvatarEditor.Core.WearablesCategory.Skirt,
                WearablesCategory.Shoes => Genies.AvatarEditor.Core.WearablesCategory.Shoes,
                WearablesCategory.Earrings => Genies.AvatarEditor.Core.WearablesCategory.Earrings,
                WearablesCategory.Glasses => Genies.AvatarEditor.Core.WearablesCategory.Glasses,
                WearablesCategory.Hat => Genies.AvatarEditor.Core.WearablesCategory.Hat,
                WearablesCategory.Mask => Genies.AvatarEditor.Core.WearablesCategory.Mask,
                _ => throw new System.ArgumentException($"Unknown WearableCategory: {category}")
            };
        }

        /// <summary>
        /// Maps SDK UserWearablesCategory to AvatarEditor UserWearablesCategory using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.UserWearablesCategory ToInternal(UserWearablesCategory category)
        {
            return category switch
            {
                UserWearablesCategory.Hoodie => Genies.AvatarEditor.Core.UserWearablesCategory.Hoodie,
                UserWearablesCategory.Shirt => Genies.AvatarEditor.Core.UserWearablesCategory.Shirt,
                UserWearablesCategory.Pants => Genies.AvatarEditor.Core.UserWearablesCategory.Pants,
                UserWearablesCategory.Shorts => Genies.AvatarEditor.Core.UserWearablesCategory.Shorts,
                _ => throw new System.ArgumentException($"Unknown UserWearablesCategory: {category}")
            };
        }

        /// <summary>
        /// Maps SDK AvatarFeatureCategory to AvatarEditor Core AvatarFeatureCategory using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarFeatureCategory ToInternal(AvatarFeatureCategory category)
        {
            return category switch
            {
                AvatarFeatureCategory.Lips => Genies.AvatarEditor.Core.AvatarFeatureCategory.Lips,
                AvatarFeatureCategory.Jaw => Genies.AvatarEditor.Core.AvatarFeatureCategory.Jaw,
                AvatarFeatureCategory.Nose => Genies.AvatarEditor.Core.AvatarFeatureCategory.Nose,
                AvatarFeatureCategory.Eyes => Genies.AvatarEditor.Core.AvatarFeatureCategory.Eyes,
                _ => throw new System.ArgumentException($"Unknown AvatarFeatureCategory: {category}")
            };
        }

        /// <summary>
        /// Maps SDK AvatarMakeupCategory to AvatarEditor Core MakeupCategory.
        /// </summary>
        internal static Genies.AvatarEditor.Core.MakeupCategory ToInternal(AvatarMakeupCategory category)
        {
            return category switch
            {
                AvatarMakeupCategory.Stickers => Genies.AvatarEditor.Core.MakeupCategory.Stickers,
                AvatarMakeupCategory.Lipstick => Genies.AvatarEditor.Core.MakeupCategory.Lipstick,
                AvatarMakeupCategory.Freckles => Genies.AvatarEditor.Core.MakeupCategory.Freckles,
                AvatarMakeupCategory.FaceGems => Genies.AvatarEditor.Core.MakeupCategory.FaceGems,
                AvatarMakeupCategory.Eyeshadow => Genies.AvatarEditor.Core.MakeupCategory.Eyeshadow,
                AvatarMakeupCategory.Blush => Genies.AvatarEditor.Core.MakeupCategory.Blush,
                _ => throw new System.ArgumentException($"Unknown AvatarMakeupCategory: {category}")
            };
        }

        /// <summary>
        /// Maps AvatarEditor Core MakeupCategory to SDK AvatarMakeupCategory (e.g. when creating MakeupColor from Core IColor).
        /// </summary>
        internal static AvatarMakeupCategory FromInternal(Genies.AvatarEditor.Core.MakeupCategory category)
        {
            return category switch
            {
                Genies.AvatarEditor.Core.MakeupCategory.Stickers => AvatarMakeupCategory.Stickers,
                Genies.AvatarEditor.Core.MakeupCategory.Lipstick => AvatarMakeupCategory.Lipstick,
                Genies.AvatarEditor.Core.MakeupCategory.Freckles => AvatarMakeupCategory.Freckles,
                Genies.AvatarEditor.Core.MakeupCategory.FaceGems => AvatarMakeupCategory.FaceGems,
                Genies.AvatarEditor.Core.MakeupCategory.Eyeshadow => AvatarMakeupCategory.Eyeshadow,
                Genies.AvatarEditor.Core.MakeupCategory.Blush => AvatarMakeupCategory.Blush,
                _ => throw new System.ArgumentException($"Unknown MakeupCategory: {category}")
            };
        }

        /// <summary>
        /// Maps SDK EyeBrowsStats to AvatarEditor EyeBrowsStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.EyeBrowsStats ToInternal(EyeBrowsStats stat)
        {
            return stat switch
            {
                EyeBrowsStats.Thickness => Genies.AvatarEditor.Core.EyeBrowsStats.Thickness,
                EyeBrowsStats.Length => Genies.AvatarEditor.Core.EyeBrowsStats.Length,
                EyeBrowsStats.VerticalPosition => Genies.AvatarEditor.Core.EyeBrowsStats.VerticalPosition,
                EyeBrowsStats.Spacing => Genies.AvatarEditor.Core.EyeBrowsStats.Spacing,
                _ => throw new System.ArgumentException($"Unknown EyeBrowsStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK EyeStats to AvatarEditor EyeStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.EyeStats ToInternal(EyeStats stat)
        {
            return stat switch
            {
                EyeStats.Size => Genies.AvatarEditor.Core.EyeStats.Size,
                EyeStats.VerticalPosition => Genies.AvatarEditor.Core.EyeStats.VerticalPosition,
                EyeStats.Spacing => Genies.AvatarEditor.Core.EyeStats.Spacing,
                EyeStats.Rotation => Genies.AvatarEditor.Core.EyeStats.Rotation,
                _ => throw new System.ArgumentException($"Unknown EyeStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK JawStats to AvatarEditor JawStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.JawStats ToInternal(JawStats stat)
        {
            return stat switch
            {
                JawStats.Width => Genies.AvatarEditor.Core.JawStats.Width,
                JawStats.Length => Genies.AvatarEditor.Core.JawStats.Length,
                _ => throw new System.ArgumentException($"Unknown JawStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK LipsStats to AvatarEditor LipsStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.LipsStats ToInternal(LipsStats stat)
        {
            return stat switch
            {
                LipsStats.Width => Genies.AvatarEditor.Core.LipsStats.Width,
                LipsStats.Fullness => Genies.AvatarEditor.Core.LipsStats.Fullness,
                LipsStats.VerticalPosition => Genies.AvatarEditor.Core.LipsStats.VerticalPosition,
                _ => throw new System.ArgumentException($"Unknown LipsStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK NoseStats to AvatarEditor NoseStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.NoseStats ToInternal(NoseStats stat)
        {
            return stat switch
            {
                NoseStats.Width => Genies.AvatarEditor.Core.NoseStats.Width,
                NoseStats.Length => Genies.AvatarEditor.Core.NoseStats.Length,
                NoseStats.VerticalPosition => Genies.AvatarEditor.Core.NoseStats.VerticalPosition,
                NoseStats.Tilt => Genies.AvatarEditor.Core.NoseStats.Tilt,
                NoseStats.Projection => Genies.AvatarEditor.Core.NoseStats.Projection,
                _ => throw new System.ArgumentException($"Unknown NoseStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK AvatarFeatureStatType to AvatarEditor AvatarFeatureStatType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarFeatureStatType ToInternal(AvatarFeatureStatType statType)
        {
            return statType switch
            {
                AvatarFeatureStatType.All => Genies.AvatarEditor.Core.AvatarFeatureStatType.All,
                AvatarFeatureStatType.Body => Genies.AvatarEditor.Core.AvatarFeatureStatType.Body,
                AvatarFeatureStatType.EyeBrows => Genies.AvatarEditor.Core.AvatarFeatureStatType.EyeBrows,
                AvatarFeatureStatType.Eyes => Genies.AvatarEditor.Core.AvatarFeatureStatType.Eyes,
                AvatarFeatureStatType.Jaw => Genies.AvatarEditor.Core.AvatarFeatureStatType.Jaw,
                AvatarFeatureStatType.Lips => Genies.AvatarEditor.Core.AvatarFeatureStatType.Lips,
                AvatarFeatureStatType.Nose => Genies.AvatarEditor.Core.AvatarFeatureStatType.Nose,
                _ => throw new System.ArgumentException($"Unknown AvatarFeatureStatType: {statType}")
            };
        }

        /// <summary>
        /// Maps AvatarEditor AvatarFeatureStat to SDK AvatarFeatureStat using explicit name-based mapping.
        /// </summary>
        internal static AvatarFeatureStat FromInternal(Genies.AvatarEditor.Core.AvatarFeatureStat stat)
        {
            return stat switch
            {
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_NeckThickness => AvatarFeatureStat.Body_NeckThickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ShoulderBroadness => AvatarFeatureStat.Body_ShoulderBroadness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ChestBustline => AvatarFeatureStat.Body_ChestBustline,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ArmsThickness => AvatarFeatureStat.Body_ArmsThickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_WaistThickness => AvatarFeatureStat.Body_WaistThickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_BellyFullness => AvatarFeatureStat.Body_BellyFullness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_HipsThickness => AvatarFeatureStat.Body_HipsThickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_LegsThickness => AvatarFeatureStat.Body_LegsThickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Body_HipSize => AvatarFeatureStat.Body_HipSize,
                Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Thickness => AvatarFeatureStat.EyeBrows_Thickness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Length => AvatarFeatureStat.EyeBrows_Length,
                Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_VerticalPosition => AvatarFeatureStat.EyeBrows_VerticalPosition,
                Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Spacing => AvatarFeatureStat.EyeBrows_Spacing,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Size => AvatarFeatureStat.Eyes_Size,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_VerticalPosition => AvatarFeatureStat.Eyes_VerticalPosition,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Spacing => AvatarFeatureStat.Eyes_Spacing,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Rotation => AvatarFeatureStat.Eyes_Rotation,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Jaw_Width => AvatarFeatureStat.Jaw_Width,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Jaw_Length => AvatarFeatureStat.Jaw_Length,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_Width => AvatarFeatureStat.Lips_Width,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_Fullness => AvatarFeatureStat.Lips_Fullness,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_VerticalPosition => AvatarFeatureStat.Lips_VerticalPosition,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Width => AvatarFeatureStat.Nose_Width,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Length => AvatarFeatureStat.Nose_Length,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_VerticalPosition => AvatarFeatureStat.Nose_VerticalPosition,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Tilt => AvatarFeatureStat.Nose_Tilt,
                Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Projection => AvatarFeatureStat.Nose_Projection,
                _ => throw new System.ArgumentException($"Unknown AvatarFeatureStat: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK AvatarFeatureStat to AvatarEditor AvatarFeatureStat using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarFeatureStat ToInternal(AvatarFeatureStat stat)
        {
            return stat switch
            {
                AvatarFeatureStat.Body_NeckThickness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_NeckThickness,
                AvatarFeatureStat.Body_ShoulderBroadness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ShoulderBroadness,
                AvatarFeatureStat.Body_ChestBustline => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ChestBustline,
                AvatarFeatureStat.Body_ArmsThickness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_ArmsThickness,
                AvatarFeatureStat.Body_WaistThickness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_WaistThickness,
                AvatarFeatureStat.Body_BellyFullness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_BellyFullness,
                AvatarFeatureStat.Body_HipsThickness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_HipsThickness,
                AvatarFeatureStat.Body_LegsThickness => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_LegsThickness,
                AvatarFeatureStat.Body_HipSize => Genies.AvatarEditor.Core.AvatarFeatureStat.Body_HipSize,
                AvatarFeatureStat.EyeBrows_Thickness => Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Thickness,
                AvatarFeatureStat.EyeBrows_Length => Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Length,
                AvatarFeatureStat.EyeBrows_VerticalPosition => Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_VerticalPosition,
                AvatarFeatureStat.EyeBrows_Spacing => Genies.AvatarEditor.Core.AvatarFeatureStat.EyeBrows_Spacing,
                AvatarFeatureStat.Eyes_Size => Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Size,
                AvatarFeatureStat.Eyes_VerticalPosition => Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_VerticalPosition,
                AvatarFeatureStat.Eyes_Spacing => Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Spacing,
                AvatarFeatureStat.Eyes_Rotation => Genies.AvatarEditor.Core.AvatarFeatureStat.Eyes_Rotation,
                AvatarFeatureStat.Jaw_Width => Genies.AvatarEditor.Core.AvatarFeatureStat.Jaw_Width,
                AvatarFeatureStat.Jaw_Length => Genies.AvatarEditor.Core.AvatarFeatureStat.Jaw_Length,
                AvatarFeatureStat.Lips_Width => Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_Width,
                AvatarFeatureStat.Lips_Fullness => Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_Fullness,
                AvatarFeatureStat.Lips_VerticalPosition => Genies.AvatarEditor.Core.AvatarFeatureStat.Lips_VerticalPosition,
                AvatarFeatureStat.Nose_Width => Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Width,
                AvatarFeatureStat.Nose_Length => Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Length,
                AvatarFeatureStat.Nose_VerticalPosition => Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_VerticalPosition,
                AvatarFeatureStat.Nose_Tilt => Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Tilt,
                AvatarFeatureStat.Nose_Projection => Genies.AvatarEditor.Core.AvatarFeatureStat.Nose_Projection,
                _ => throw new System.ArgumentException($"Unknown AvatarFeatureStat: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK AvatarColorKind to AvatarEditor AvatarColorKind using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.AvatarColorKind ToInternal(AvatarColorKind colorKind)
        {
            return colorKind switch
            {
                AvatarColorKind.Hair => Genies.AvatarEditor.Core.AvatarColorKind.Hair,
                AvatarColorKind.FacialHair => Genies.AvatarEditor.Core.AvatarColorKind.FacialHair,
                AvatarColorKind.EyeBrows => Genies.AvatarEditor.Core.AvatarColorKind.EyeBrows,
                AvatarColorKind.EyeLash => Genies.AvatarEditor.Core.AvatarColorKind.EyeLash,
                AvatarColorKind.Skin => Genies.AvatarEditor.Core.AvatarColorKind.Skin,
                AvatarColorKind.Eyes => Genies.AvatarEditor.Core.AvatarColorKind.Eyes,
                AvatarColorKind.MakeupStickers => Genies.AvatarEditor.Core.AvatarColorKind.MakeupStickers,
                AvatarColorKind.MakeupLipstick => Genies.AvatarEditor.Core.AvatarColorKind.MakeupLipstick,
                AvatarColorKind.MakeupFreckles => Genies.AvatarEditor.Core.AvatarColorKind.MakeupFreckles,
                AvatarColorKind.MakeupFaceGems => Genies.AvatarEditor.Core.AvatarColorKind.MakeupFaceGems,
                AvatarColorKind.MakeupEyeshadow => Genies.AvatarEditor.Core.AvatarColorKind.MakeupEyeshadow,
                AvatarColorKind.MakeupBlush => Genies.AvatarEditor.Core.AvatarColorKind.MakeupBlush,
                _ => throw new System.ArgumentException($"Unknown AvatarColorKind: {colorKind}")
            };
        }
        /// <summary>
        /// Maps AvatarEditor AvatarColorKind to SDK AvatarColorKind using explicit name-based mapping.
        /// </summary>
        internal static AvatarColorKind FromInternal(Genies.AvatarEditor.Core.AvatarColorKind colorKind)
        {
            return colorKind switch
            {
                Genies.AvatarEditor.Core.AvatarColorKind.Hair => AvatarColorKind.Hair,
                Genies.AvatarEditor.Core.AvatarColorKind.FacialHair => AvatarColorKind.FacialHair,
                Genies.AvatarEditor.Core.AvatarColorKind.EyeBrows => AvatarColorKind.EyeBrows,
                Genies.AvatarEditor.Core.AvatarColorKind.EyeLash => AvatarColorKind.EyeLash,
                Genies.AvatarEditor.Core.AvatarColorKind.Skin => AvatarColorKind.Skin,
                Genies.AvatarEditor.Core.AvatarColorKind.Eyes => AvatarColorKind.Eyes,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupStickers => AvatarColorKind.MakeupStickers,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupLipstick => AvatarColorKind.MakeupLipstick,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupFreckles => AvatarColorKind.MakeupFreckles,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupFaceGems => AvatarColorKind.MakeupFaceGems,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupEyeshadow => AvatarColorKind.MakeupEyeshadow,
                Genies.AvatarEditor.Core.AvatarColorKind.MakeupBlush => AvatarColorKind.MakeupBlush,
                _ => throw new System.ArgumentException($"Unknown AvatarColorKind: {colorKind}")
            };
        }

        /// <summary>
        /// Maps SDK BodyStats to AvatarEditor BodyStats using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.BodyStats ToInternal(BodyStats stat)
        {
            return stat switch
            {
                BodyStats.NeckThickness => Genies.AvatarEditor.Core.BodyStats.NeckThickness,
                BodyStats.ShoulderBroadness => Genies.AvatarEditor.Core.BodyStats.ShoulderBroadness,
                BodyStats.ChestBustline => Genies.AvatarEditor.Core.BodyStats.ChestBustline,
                BodyStats.ArmsThickness => Genies.AvatarEditor.Core.BodyStats.ArmsThickness,
                BodyStats.WaistThickness => Genies.AvatarEditor.Core.BodyStats.WaistThickness,
                BodyStats.BellyFullness => Genies.AvatarEditor.Core.BodyStats.BellyFullness,
                BodyStats.HipsThickness => Genies.AvatarEditor.Core.BodyStats.HipsThickness,
                BodyStats.LegsThickness => Genies.AvatarEditor.Core.BodyStats.LegsThickness,
                BodyStats.HipSize => Genies.AvatarEditor.Core.BodyStats.HipSize,
                _ => throw new System.ArgumentException($"Unknown BodyStats: {stat}")
            };
        }

        /// <summary>
        /// Maps SDK HairType to AvatarEditor HairType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.HairType ToInternal(HairType hairType)
        {
            return hairType switch
            {
                HairType.Hair => Genies.AvatarEditor.Core.HairType.Hair,
                HairType.FacialHair => Genies.AvatarEditor.Core.HairType.FacialHair,
                HairType.Eyebrows => Genies.AvatarEditor.Core.HairType.Eyebrows,
                HairType.Eyelashes => Genies.AvatarEditor.Core.HairType.Eyelashes,
                _ => throw new System.ArgumentException($"Unknown HairType: {hairType}")
            };
        }

        /// <summary>
        /// Maps SDK ColorSource to AvatarEditor ColorSource using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.ColorSource ToInternal(ColorSource source)
        {
            return source switch
            {
                ColorSource.All => Genies.AvatarEditor.Core.ColorSource.All,
                ColorSource.User => Genies.AvatarEditor.Core.ColorSource.User,
                ColorSource.Default => Genies.AvatarEditor.Core.ColorSource.Default,
                _ => throw new System.ArgumentException($"Unknown ColorSource: {source}")
            };
        }

        /// <summary>
        /// Maps SDK WearableAssetSource to AvatarEditor RequestType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.RequestType ToInternal(WearableAssetSource source)
        {
            return source switch
            {
                WearableAssetSource.All => Genies.AvatarEditor.Core.RequestType.All,
                WearableAssetSource.User => Genies.AvatarEditor.Core.RequestType.User,
                WearableAssetSource.Default => Genies.AvatarEditor.Core.RequestType.Default,
                _ => throw new System.ArgumentException($"Unknown WearableAssetSource: {source}")
            };
        }

        /// <summary>
        /// Maps SDK ColorType to AvatarEditor ColorType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.ColorType ToInternal(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Eyes => Genies.AvatarEditor.Core.ColorType.Eyes,
                ColorType.Hair => Genies.AvatarEditor.Core.ColorType.Hair,
                ColorType.FacialHair => Genies.AvatarEditor.Core.ColorType.FacialHair,
                ColorType.Skin => Genies.AvatarEditor.Core.ColorType.Skin,
                ColorType.Eyebrow => Genies.AvatarEditor.Core.ColorType.Eyebrow,
                ColorType.Eyelash => Genies.AvatarEditor.Core.ColorType.Eyelash,
                ColorType.MakeupStickers => Genies.AvatarEditor.Core.ColorType.MakeupStickers,
                ColorType.MakeupLipstick => Genies.AvatarEditor.Core.ColorType.MakeupLipstick,
                ColorType.MakeupFreckles => Genies.AvatarEditor.Core.ColorType.MakeupFreckles,
                ColorType.MakeupFaceGems => Genies.AvatarEditor.Core.ColorType.MakeupFaceGems,
                ColorType.MakeupEyeshadow => Genies.AvatarEditor.Core.ColorType.MakeupEyeshadow,
                ColorType.MakeupBlush => Genies.AvatarEditor.Core.ColorType.MakeupBlush,

                _ => throw new System.ArgumentException($"Unknown ColorType: {colorType}")
            };
        }

        /// <summary>
        /// Maps SDK UserColorType to AvatarEditor UserColorType (for GetUserColorsAsync).
        /// </summary>
        internal static Genies.AvatarEditor.Core.UserColorType ToInternal(UserColorType colorType)
        {
            return colorType switch
            {
                UserColorType.Hair => Genies.AvatarEditor.Core.UserColorType.Hair,
                UserColorType.Eyebrow => Genies.AvatarEditor.Core.UserColorType.Eyebrow,
                UserColorType.Eyelash => Genies.AvatarEditor.Core.UserColorType.Eyelash,
                _ => throw new System.ArgumentException($"Unknown UserColorType: {colorType}")
            };
        }

        /// <summary>
        /// Maps SDK UserColorType to AvatarEditor ColorType using explicit name-based mapping.
        /// </summary>
        internal static Genies.AvatarEditor.Core.ColorType ToInternalUser(UserColorType colorType)
        {
            return colorType switch
            {
                UserColorType.Hair => Genies.AvatarEditor.Core.ColorType.Hair,
                UserColorType.Eyebrow => Genies.AvatarEditor.Core.ColorType.Eyebrow,
                UserColorType.Eyelash => Genies.AvatarEditor.Core.ColorType.Eyelash,
                _ => throw new System.ArgumentException($"Unknown UserColorType: {colorType}")
            };
        }

        /// <summary>
        /// Maps SDK AssetType to Inventory AssetType using explicit name-based mapping.
        /// </summary>
        internal static Genies.Inventory.AssetType ToInternal(AssetType assetType)
        {
            return assetType switch
            {
                AssetType.WardrobeGear => Genies.Inventory.AssetType.WardrobeGear,
                AssetType.AvatarBase => Genies.Inventory.AssetType.AvatarBase,
                AssetType.AvatarMakeup => Genies.Inventory.AssetType.AvatarMakeup,
                AssetType.Flair => Genies.Inventory.AssetType.Flair,
                AssetType.AvatarEyes => Genies.Inventory.AssetType.AvatarEyes,
                AssetType.ColorPreset => Genies.Inventory.AssetType.ColorPreset,
                AssetType.ImageLibrary => Genies.Inventory.AssetType.ImageLibrary,
                AssetType.AnimationLibrary => Genies.Inventory.AssetType.AnimationLibrary,
                AssetType.Avatar => Genies.Inventory.AssetType.Avatar,
                AssetType.Decor => Genies.Inventory.AssetType.Decor,
                AssetType.ModelLibrary => Genies.Inventory.AssetType.ModelLibrary,
                _ => throw new System.ArgumentException($"Unknown AssetType: {assetType}")
            };
        }

        /// <summary>
        /// Maps Inventory AssetType to SDK AssetType using explicit name-based mapping.
        /// </summary>
        internal static AssetType FromInternal(Genies.Inventory.AssetType assetType)
        {
            return assetType switch
            {
                Genies.Inventory.AssetType.WardrobeGear => AssetType.WardrobeGear,
                Genies.Inventory.AssetType.AvatarBase => AssetType.AvatarBase,
                Genies.Inventory.AssetType.AvatarMakeup => AssetType.AvatarMakeup,
                Genies.Inventory.AssetType.Flair => AssetType.Flair,
                Genies.Inventory.AssetType.AvatarEyes => AssetType.AvatarEyes,
                Genies.Inventory.AssetType.ColorPreset => AssetType.ColorPreset,
                Genies.Inventory.AssetType.ImageLibrary => AssetType.ImageLibrary,
                Genies.Inventory.AssetType.AnimationLibrary => AssetType.AnimationLibrary,
                Genies.Inventory.AssetType.Avatar => AssetType.Avatar,
                Genies.Inventory.AssetType.Decor => AssetType.Decor,
                Genies.Inventory.AssetType.ModelLibrary => AssetType.ModelLibrary,
                _ => throw new System.ArgumentException($"Unknown AssetType: {assetType}")
            };
        }

        /// <summary>
        /// Converts a list of Core IColor instances into a list of equivalent SDK IAvatarColor instances.
        /// </summary>
        /// <param name="iColors">The list of Core IColor (e.g. from GetDefaultColorsAsync). Can be null or empty; returns an empty list when null.</param>
        /// <returns>A new list of IAvatarColor in the same order; empty list when input is null or empty.</returns>
        public static List<IAvatarColor> FromIColors(List<Genies.AvatarEditor.Core.IColor> iColors)
        {
            if (iColors == null || iColors.Count == 0)
            {
                return new List<IAvatarColor>();
            }

            var result = new List<IAvatarColor>(iColors.Count);
            foreach (var iColor in iColors)
            {
                result.Add(FromIColor(iColor));
            }
            return result;
        }

        /// <summary>
        /// Converts an SDK IAvatarColor into Genies.AvatarEditor.Core.IColor (e.g. for passing to Core SetColorAsync or internal APIs).
        /// </summary>
        /// <param name="sdkColor">The SDK IAvatarColor (e.g. from CreateHairColor, GetColorAsync, or color list APIs).</param>
        /// <returns>A Genies.AvatarEditor.Core.IColor of the matching Core type with the same Kind, Hexes, AssetId, Name, IsCustom, and Order.</returns>
        public static Genies.AvatarEditor.Core.IColor ToIColor(IAvatarColor sdkColor)
        {
            if (sdkColor == null)
            {
                throw new ArgumentNullException(nameof(sdkColor));
            }

            var hexes = sdkColor.Hexes;
            Color c0 = (hexes != null && hexes.Length > 0) ? hexes[0] : Color.clear;
            Color c1 = (hexes != null && hexes.Length > 1) ? hexes[1] : c0;
            Color c2 = (hexes != null && hexes.Length > 2) ? hexes[2] : c0;
            Color c3 = (hexes != null && hexes.Length > 3) ? hexes[3] : c0;
            string assetId = sdkColor.AssetId ?? string.Empty;

            switch (sdkColor.Kind)
            {
                case AvatarColorKind.Hair:
                    var hair = new Genies.AvatarEditor.Core.HairColor(c0, c1, c2, c3);
                    hair.IsCustom = sdkColor.IsCustom;
                    return hair;
                case AvatarColorKind.FacialHair:
                    var facialHair = new Genies.AvatarEditor.Core.FacialHairColor(c0, c1, c2, c3);
                    facialHair.IsCustom = sdkColor.IsCustom;
                    return facialHair;
                case AvatarColorKind.EyeBrows:
                    var brows = new Genies.AvatarEditor.Core.EyeBrowsColor(c0, c1);
                    brows.IsCustom = sdkColor.IsCustom;
                    return brows;
                case AvatarColorKind.EyeLash:
                    var lash = new Genies.AvatarEditor.Core.EyeLashColor(c0, c1);
                    lash.IsCustom = sdkColor.IsCustom;
                    return lash;
                case AvatarColorKind.Skin:
                    var skin = new Genies.AvatarEditor.Core.SkinColor(c0);
                    skin.IsCustom = sdkColor.IsCustom;
                    return skin;
                case AvatarColorKind.Eyes:
                    var eye = new Genies.AvatarEditor.Core.EyeColor(assetId, c0, c1);
                    eye.IsCustom = sdkColor.IsCustom;
                    return eye;
                case AvatarColorKind.MakeupStickers:
                case AvatarColorKind.MakeupLipstick:
                case AvatarColorKind.MakeupFreckles:
                case AvatarColorKind.MakeupFaceGems:
                case AvatarColorKind.MakeupEyeshadow:
                case AvatarColorKind.MakeupBlush:
                    var coreCategory = EnumMapper.ToInternal(AvatarColorKindMakeupCategoryMapper.ToMakeupCategory(sdkColor.Kind));
                    var makeup = new Genies.AvatarEditor.Core.MakeupColor(coreCategory, c0, c1, c2, c3);
                    makeup.IsCustom = sdkColor.IsCustom;
                    return makeup;
                default:
                    throw new ArgumentException($"Unsupported AvatarColorKind: {sdkColor.Kind}.", nameof(sdkColor));
            }
        }

        /// <summary>
        /// Converts a Core IColor (e.g. from AvatarEditorSDK.GetColorAsync) into the equivalent SDK IAvatarColor.
        /// </summary>
        /// <param name="iColor">The Core IColor (HairColor, FacialHairColor, EyeBrowsColor, EyeLashColor, SkinColor, or EyeColor from Genies.AvatarEditor.Core).</param>
        /// <returns>An IAvatarColor of the matching SDK type with the same Hexes and AssetId.</returns>
        public static IAvatarColor FromIColor(Genies.AvatarEditor.Core.IColor iColor)
        {
            if (iColor == null)
            {
                throw new ArgumentNullException(nameof(iColor));
            }

            var hexes = iColor.Hexes;
            var assetId = iColor.AssetId ?? string.Empty;
            Color c0 = (hexes != null && hexes.Length > 0) ? hexes[0] : Color.clear;
            Color c1 = (hexes != null && hexes.Length > 1) ? hexes[1] : c0;
            Color c2 = (hexes != null && hexes.Length > 2) ? hexes[2] : c0;
            Color c3 = (hexes != null && hexes.Length > 3) ? hexes[3] : c0;

            switch (iColor)
            {
                case Genies.AvatarEditor.Core.HairColor _:
                    return new HairColor(c0, c1, c2, c3);
                case Genies.AvatarEditor.Core.FacialHairColor _:
                    return new FacialHairColor(c0, c1, c2, c3);
                case Genies.AvatarEditor.Core.EyeBrowsColor _:
                    return new EyeBrowsColor(c0, c1);
                case Genies.AvatarEditor.Core.EyeLashColor _:
                    return new EyeLashColor(c0, c1);
                case Genies.AvatarEditor.Core.SkinColor _:
                    return new SkinColor(c0);
                case Genies.AvatarEditor.Core.EyeColor _:
                    return new EyeColor(assetId, c0, c1);
                case Genies.AvatarEditor.Core.MakeupColor coreMakeup:
                    return new MakeupColor(EnumMapper.FromInternal(coreMakeup.Category), c0, c1, c2, c3);
                default:
                    throw new ArgumentException($"Unsupported IColor runtime type: {iColor?.GetType().Name ?? "null"}.", nameof(iColor));
            }
        }
    }
}
