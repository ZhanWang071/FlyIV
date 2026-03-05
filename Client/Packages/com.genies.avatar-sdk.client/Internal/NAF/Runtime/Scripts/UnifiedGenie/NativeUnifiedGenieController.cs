using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using Genies.Utilities;
using GnWrappers;
using Newtonsoft.Json;
using UnityEngine;

using CancellationToken = System.Threading.CancellationToken;
using Object = UnityEngine.Object;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NativeUnifiedGenieController : ISpeciesGenieController
#else
    public sealed class NativeUnifiedGenieController : ISpeciesGenieController
#endif
    {
#if GENIES_SDK
        private const string _femaleShapeAssetId = "Static/BlendShapeContainer_body_female";
        private const string _maleShapeAssetId   = "Static/BlendShapeContainer_body_male";
#else
        private const string _femaleShapeAssetId = "AvatarBase/recmdZ4C4enmt630";
        private const string _maleShapeAssetId = "AvatarBase/recmdZ4c4ENEO817";

#endif
        public IGenie Genie => NativeBuilder.NativeGenie;

        /**
         * The native genie builder instance that this controller manages. This is a low-level class, and you should not
         * access it unless you know what you are doing.
         */
        public NativeGenieBuilder NativeBuilder { get; }

        public ContainerService      ContainerService    => _containerServiceRef.Item;
        public Ref<ContainerService> ContainerServiceRef => _containerServiceRef.New();

        /**
         * A service used to fetch the parameters dictionary for a given asset ID. Used when loading an avatar
         * definition since it won't contain any parameters.
         */
        public IAssetParamsService AssetParamsService;

        private readonly GenieLoadAndEquipSerializer _loadAndEquipSerializer;

        private Ref<ContainerService> _containerServiceRef;

        public NativeUnifiedGenieController(
            NativeGenieBuilder    nativeBuilder,
            IAssetParamsService   assetParamsService,
            Ref<ContainerService> containerServiceRef = default
        ) {
            NativeBuilder        = nativeBuilder;
            AssetParamsService   = assetParamsService;
            _containerServiceRef = containerServiceRef;

            NativeBuilder.EnsureAwake();

            if (!_containerServiceRef.IsAlive)
            {
                _containerServiceRef = CreateRef.FromDisposable(new ContainerService(NafAssetResolverConfig.Default));
            }

            _loadAndEquipSerializer = new GenieLoadAndEquipSerializer(_containerServiceRef.New(), NativeBuilder, latestOpWins: true);
        }

        /// <summary>
        /// Overrides the container service used by this controller to load assets.
        /// </summary>
        public void SetContainerService(Ref<ContainerService> containerService)
        {
            if (_containerServiceRef == containerService)
            {
                return;
            }

            if (!containerService.IsAlive)
            {
                throw new Exception("ContainerService reference is not alive.");
            }

            _containerServiceRef.Dispose();
            _containerServiceRef = containerService;
        }

        public UniTask EquipAssetAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            return _loadAndEquipSerializer.LoadAndEquipAsync(assetId, parameters, cancellationToken).SuppressCancellationThrow();
        }

        public UniTask UnequipAssetAsync(string assetId, CancellationToken cancellationToken = default)
        {
            return _loadAndEquipSerializer.UnequipAsync(assetId, cancellationToken).SuppressCancellationThrow();
        }

        /**
         * Equips the given assets. Use this method if you want to equip multiple assets at once, with maximum performance.
         */
        public UniTask EquipAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets, CancellationToken cancellationToken = default)
        {
            var requests = assets.Select(asset => new ContainerAssetRequest(asset.assetId, asset.parameters));
            return _loadAndEquipSerializer.LoadAndEquipAsync(requests, cancellationToken).SuppressCancellationThrow();
        }

        public UniTask UnequipAssetsAsync(IEnumerable<string> assetIds, CancellationToken cancellationToken = default)
        {
            return _loadAndEquipSerializer.UnequipAsync(assetIds, cancellationToken).SuppressCancellationThrow();
        }

        /**
         * Sets the equipped assets to the given ones (this clears the current assets first, and then equips). Use this
         * method if you want to replace current assets with maximum performance (only one avatar rebuild will take place).
         */
        public UniTask SetEquippedAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets, CancellationToken cancellationToken = default)
        {
            var requests = assets.Select(asset => new ContainerAssetRequest(asset.assetId, asset.parameters));
            return _loadAndEquipSerializer.LoadAndSetAsync(requests, cancellationToken).SuppressCancellationThrow();
        }

        public bool IsAssetEquipped(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return false;
            }

            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                // Temporary work around to check if Entity Name contains AssetID. We
                // will do this until Inventory package switches to using UniversalID
                if (entity.Name().Contains(assetId))
                {
                    return true;
                }
            }

            return false;
        }

        public UniTask SetColorAsync(string colorId, Color color)
        {
            NativeBuilder.SetColor(colorId, color);
            NativeBuilder.RebuildColors();
            return UniTask.CompletedTask;
        }

        public UniTask SetColorsAsync(IEnumerable<GenieColorEntry> colors)
        {
            foreach (GenieColorEntry entry in colors)
            {
                if (entry.Value.HasValue)
                {
                    NativeBuilder.SetColor(entry.ColorId, entry.Value.Value);
                }
                else
                {
                    NativeBuilder.UnsetColor(entry.ColorId);
                }
            }

            NativeBuilder.RebuildColors();
            return UniTask.CompletedTask;
        }

        public Color? GetColor(string colorId)
        {
            return NativeBuilder.GetColor(colorId);
        }

        public UniTask UnsetColorAsync(string colorId)
        {
            NativeBuilder.UnsetColor(colorId);
            return NativeBuilder.RebuildAsync(); // unsetting colors requires a full rebuild (which will be quick anyways since we just changed a color)
        }

        public UniTask UnsetAllColorsAsync()
        {
            NativeBuilder.UnsetAllColors();
            return NativeBuilder.RebuildAsync();
        }

        public bool IsColorAvailable(string colorId)
        {
            return NativeBuilder.ColorAttributeExists(colorId);
        }

        public void SetBodyAttribute(string attributeId, float weight)
        {
            NativeBuilder.SetShapeAttributeWeight(attributeId, weight);
            NativeBuilder.RebuildSkeletonOffset();
        }

        public float GetBodyAttribute(string attributeId)
        {
            return NativeBuilder.GetShapeAttributeWeight(attributeId);
        }

        public void SetBodyPreset(BodyAttributesPreset preset)
        {
            NativeBuilder.SetShapeAttributes(preset);
            NativeBuilder.RebuildSkeletonOffset();
        }

#region Deprecated methods for retrocompatibility with the current MegaEditor
        public UniTask SetBodyPresetAsync(GSkelModifierPreset preset, CancellationToken cancellationToken = default)
        {
            // ensure that the operation is serialized with the asset equip/unequip operations
            return _loadAndEquipSerializer.SerializedExecutor.ExecuteAsync(OperationAsync, cancellationToken).SuppressCancellationThrow();

            async UniTask OperationAsync(CancellationToken cancellationToken)
            {
                string bodyVariationAssetId = null;
                if (preset.StartingBodyVariation == UnifiedBodyVariation.Female)
                {
                    bodyVariationAssetId = _femaleShapeAssetId;
                }
                else if (preset.StartingBodyVariation == UnifiedBodyVariation.Male)
                {
                    bodyVariationAssetId = _maleShapeAssetId;
                }

                if (!string.IsNullOrEmpty(bodyVariationAssetId))
                {
                    Dictionary<string, string> parameters = await AssetParamsService.FetchParamsAsync(bodyVariationAssetId);
                    using Entity asset = await ContainerService.CombinableAsset.LoadAsync(bodyVariationAssetId, parameters, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (asset is not null && !asset.IsNull())
                    {
                        NativeBuilder.AddEntity(asset);
                    }
                }

                foreach (GSkelModValue value in preset.GSkelModValues)
                {
                    NativeBuilder.SetShapeAttributeWeight(value.Name, value.Value);
                }

                if (!string.IsNullOrEmpty(bodyVariationAssetId))
                {
                    await NativeBuilder.RebuildAsync();
                    // if already rebuilt, cancellation is too late
                }
                else
                {
                    NativeBuilder.RebuildSkeletonOffset();
                }
            }
        }

        public GSkelModifierPreset GetBodyPreset()
        {
            GSkelModifierPreset preset = ScriptableObject.CreateInstance<GSkelModifierPreset>();
            preset.StartingBodyVariation = GetBodyVariation();
            List<string> attributes = NativeBuilder.GetExistingShapeAttributes();
            preset.GSkelModValues ??= new List<GSkelModValue>(attributes.Count);

            foreach (string attribute in attributes)
            {
                preset.GSkelModValues.Add(new GSkelModValue
                {
                    Name = attribute,
                    Value = NativeBuilder.GetShapeAttributeWeight(attribute),
                });
            }

            return preset;
        }

        public string GetBodyVariation()
        {
            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                string assetId = entity.Name();

                if (assetId == _femaleShapeAssetId)
                {
                    return UnifiedBodyVariation.Female;
                }

                if (assetId == _maleShapeAssetId)
                {
                    return UnifiedBodyVariation.Male;
                }
            }

            return null;
        }
#endregion

        public void ResetAllBodyAttributes()
        {
            NativeBuilder.ResetShapeAttributeWeights();
            NativeBuilder.RebuildSkeletonOffset();
        }

        public bool IsBodyAttributeAvailable(string attributeId)
        {
            return NativeBuilder.ShapeAttributeExists(attributeId);
        }

        public UniTask EquipTattooAsync(MegaSkinTattooSlot slot, string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            return _loadAndEquipSerializer.SerializedExecutor.ExecuteAsync(Operation, cancellationToken).SuppressCancellationThrow();

            async UniTask Operation(CancellationToken cancellationToken)
            {
                using GnWrappers.Texture texture = await ContainerService.Texture.LoadAsync(assetId, parameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                NativeBuilder.SetTattoo(slot, texture);
                NativeBuilder.RebuildTattoos();
            }
        }

        public UniTask UnequipTattooAsync(MegaSkinTattooSlot slot)
        {
            NativeBuilder.UnsetTattoo(slot);
            NativeBuilder.RebuildTattoos();
            return UniTask.CompletedTask;
        }

        public UniTask UnequipAllTattoosAsync()
        {
            NativeBuilder.UnsetAllTattoos();
            NativeBuilder.RebuildTattoos();
            return UniTask.CompletedTask;
        }

        public bool IsTattooEquipped(MegaSkinTattooSlot slot, string assetId)
        {
            return NativeBuilder.IsTattooEquipped(slot, assetId);
        }

        public string GetEquippedTattoo(MegaSkinTattooSlot slot)
        {
            using GnWrappers.Texture texture = NativeBuilder.GetTattoo(slot);
            if (texture is null || texture.IsNull())
            {
                return null;
            }

            return texture.Name();
        }

        public AvatarDefinition GetDefinitionType()
        {
            var definition = new AvatarDefinition();

            // gather all equipped assets
            AddEquippedAssetIds(definition.equippedAssetIds);

            // gather all colors
            List<string> colorIds = NativeBuilder.GetExistingColorAttributes();
            foreach (string colorId in colorIds)
            {
                Color? color = NativeBuilder.GetColor(colorId);
                if (color.HasValue)
                {
                    definition.colors.Add(colorId, color.Value);
                }
            }

            // gather all body attributes
            List<string> attributeIds = NativeBuilder.GetExistingShapeAttributes();
            foreach (string attributeId in attributeIds)
            {
                float weight = NativeBuilder.GetShapeAttributeWeight(attributeId);
                if (weight != 0.0f) // only include attributes that have a non-zero weight, which is the default
                {
                    definition.bodyAttributes.Add(attributeId, weight);
                }
            }

            // gather all equipped tattoos
            var tattooSlots = Enum.GetValues(typeof(MegaSkinTattooSlot)) as MegaSkinTattooSlot[];
            foreach (MegaSkinTattooSlot slot in tattooSlots)
            {
                using GnWrappers.Texture texture = NativeBuilder.TattooEditor.GetTattoo(slot);
                if (texture is null || texture.IsNull())
                {
                    continue;
                }

                string assetId = texture.Name();
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] found an equipped tattoo ({slot.ToString()}) with a null or empty ID. It won't be included in the avatar definition...");
                    continue;
                }

                definition.equippedTattooIds.Add(slot, assetId);
            }

            return definition;
        }

        public string GetDefinition()
        {
            var definition = GetDefinitionType();

            try
            {
                string json = JsonConvert.SerializeObject(definition);
                return json;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] failed to serialize avatar definition:\n{exception}");
                return null;
            }
        }

        /// <summary>
        /// Pre-caches assets required for an avatar definition without building the avatar.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// Assets are disposed after loading to free memory while keeping them cached in ContainerApi.
        /// </summary>
        /// <param name="definition">The avatar definition containing the assets to precache.</param>
        public async UniTask PrecacheAssetsAsync(AvatarDefinition definition)
        {
            using var loader = new AvatarDefinitionAssetLoader(AssetParamsService, _containerServiceRef.New());
            await loader.PreloadAssetsAsync(definition);
        }

        public async UniTask SetDefinitionAsync(AvatarDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null)
            {
                return;
            }

            // we can't remove the async modifier from this method because otherwise the process span would not correctly measure the operation
            using var processSpanSetDefinition = new ProcessSpan(ProcessIds.SetDefinitionAsync);
            await _loadAndEquipSerializer.SerializedExecutor.ExecuteAsync(Operation, cancellationToken).SuppressCancellationThrow();

            async UniTask Operation(CancellationToken cancellationToken)
            {
                // get currently equipped entities and keep them alive until we finish, so we avoid unnecessary reloads
                using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();

                using var processSpanAssetsAndTattoos = new ProcessSpan(ProcessIds.SetDefinitionAsyncAssetsAndTattoos, processSpanSetDefinition);

                // load all assets and tattoos in parallel using shared utility
                using var loader = new AvatarDefinitionAssetLoader(AssetParamsService, _containerServiceRef.New());
                using var assets = await loader.LoadDisposableAssetsAsync(definition, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // set loaded assets and tattoos
                NativeBuilder.ClearEntities();
                NativeBuilder.UnsetAllTattoos();

                if (assets.Entities is { Length: > 0 })
                {
                    NativeBuilder.AddEntities(assets.Entities);
                }

                if (assets.Tattoos is { Length: > 0 })
                {
                    foreach ((MegaSkinTattooSlot slot, GnWrappers.Texture tattoo) in assets.Tattoos)
                    {
                        if (tattoo is not null)
                        {
                            NativeBuilder.TattooEditor.SetTattoo(slot, tattoo);
                        }
                    }
                }

                processSpanAssetsAndTattoos.Dispose();

                // set all the colors
                NativeBuilder.UnsetAllColors();
                if (definition.colors is { Count: > 0 })
                {
                    using (new ProcessSpan(ProcessIds.SetDefinitionAsyncSetColor, processSpanSetDefinition))
                    {
                        foreach ((string colorId, Color color) in definition.colors)
                        {
                            NativeBuilder.SetColor(colorId, color);
                        }
                    }
                }

                // set all the body attributes
                NativeBuilder.ResetShapeAttributeWeights();
                if (definition.bodyAttributes is { Count: > 0 })
                {
                    using (new ProcessSpan(ProcessIds.SetDefinitionAsyncSetBodyAttr, processSpanSetDefinition))
                    {
                        foreach ((string attributeId, float weight) in definition.bodyAttributes)
                        {
                            NativeBuilder.SetShapeAttributeWeight(attributeId, weight);
                        }
                    }
                }

                using (new ProcessSpan(ProcessIds.SetDefinitionAsyncRebuildAvatar, processSpanSetDefinition))
                {
                    // rebuild the native genie
                    await NativeBuilder.RebuildAsync();
                }
            }
        }

        public UniTask SetDefinitionAsync(string definition)
        {
            return SetDefinitionAsync(definition, CancellationToken.None);
        }

        public UniTask SetDefinitionAsync(string definition, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(definition))
            {
                return UniTask.CompletedTask;
            }

            AvatarDefinition avatar;
            try
            {
                avatar = JsonConvert.DeserializeObject<AvatarDefinition>(definition);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] failed to deserialize avatar definition:\n{exception}");
                return UniTask.CompletedTask;
            }

            return SetDefinitionAsync(avatar, cancellationToken);
        }

        public void AddEquippedAssetIds(ICollection<string> results)
        {
            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                string assetId = entity.Name();
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] found an equipped asset with a null or empty asset ID");
                    continue;
                }

                results.Add(assetId);
            }
        }

        public List<string> GetEquippedAssetIds()
        {
            var results = new List<string>();
            AddEquippedAssetIds(results);
            return results;
        }

        public void Dispose()
        {
            // dispose NativeBuilder first
            NativeGenie genie = null;
            if (NativeBuilder)
            {
                genie = NativeBuilder.NativeGenie;
                NativeBuilder.Dispose();
            }

            // then the NativeGenie
            GameObject root = null;
            if (genie)
            {
                root = genie.Root;
                genie.Dispose();
            }

            _containerServiceRef.Dispose();
            _loadAndEquipSerializer.Dispose();

            if (root)
            {
                Object.Destroy(root);
            }
        }
    }
}
