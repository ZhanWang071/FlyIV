using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Customization.Framework;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "EyeColorItemPickerDataSource", menuName = "Genies/Customizer/DataSource/EyeColorItemPickerDataSource")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class EyeColorItemPickerDataSource : ColorItemPickerDataSource
#else
    public class EyeColorItemPickerDataSource : ColorItemPickerDataSource
#endif
    {
        /// <summary>
        /// The event name to dispatch to analytics
        /// </summary>
        protected override string ColorAnalyticsEventName => CustomizationAnalyticsEvents.EyeColorPresetClickEvent;

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                // Use the same config as ColorPresetCustomizationController uses for eye colors
                var config = UIDataProviderConfigs.DefaultAvatarEyesConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStarted);
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ColorPresetCustomizationStopped);
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        public override async UniTask<Ref<SimpleColorUiData>> GetDataForIndexAsync(int index)
        {
            // Ensure _ids is populated (normally happens in InitializeAndGetCountAsync)
            if (_ids == null || _ids.Count == 0)
            {
                _ids = await GetUIProvider<ColoredInventoryAsset, SimpleColorUiData>().GetAllAssetIds(
                    pageSize: InventoryConstants.DefaultPageSize);
            }

            return await GetDataForIndexBaseAsync<ColoredInventoryAsset, SimpleColorUiData>(index, "EyeColorItemPicker");
        }

        /// <summary>
        /// Creates the command to equip the eye color.
        /// </summary>
        protected override UniTask<ICommand> CreateEquipCommandAsync(SimpleColorUiData colorData, CancellationToken cancellationToken)
        {
            // Update avatar eye color using EquipNativeAvatarAssetCommand (same as ColorPresetCustomizationController)
            return UniTask.FromResult<ICommand>(new EquipNativeAvatarAssetCommand(colorData.AssetId, CurrentCustomizableAvatar));
        }

        /// <summary>
        /// Determines if the editable icon should be shown. Eye colors don't show editable icon.
        /// </summary>
        protected override bool ShouldShowEditableIcon(SimpleColorUiData colorData, bool isEquipped)
        {
            // Eye colors don't show editable icon
            return false;
        }

        /// <summary>
        /// Gets which color is equipped by comparing the actual color values.
        /// </summary>
        /// <remarks>Compares the avatar's current equipped color with each preset's color to find a match.
        /// Falls back to base implementation if data isn't cached yet.</remarks>
        /// <returns>the index of the UI item. -1 if none is selected.</returns>
        public override int GetCurrentSelectedIndex()
        {
            if (_ids == null)
            {
                return -1;
            }

            var currentSkinColor = CurrentCustomizableAvatar.GetColor(GenieColor.Skin) ?? Color.black;
            const float colorTolerance = 0.01f;

            // First try to match by color comparison using cached data
            for (var index = 0; index < _ids.Count; index++)
            {
                bool hasData = TryGetLoadedData(index, out Ref<SimpleColorUiData> dataRef);

                // If ref is dead or not cached, try to reload it with fire-and-forget
                if (!hasData || !dataRef.IsAlive || dataRef.Item == null)
                {
                    GetDataForIndexAsync(index).Forget();
                    continue;
                }

                var presetColor = dataRef.Item.InnerColor;
                if (ColorsMatch(currentSkinColor, presetColor, colorTolerance))
                {
                    return index;
                }
            }

            // Fallback to base implementation (asset ID check)
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }
    }
}

