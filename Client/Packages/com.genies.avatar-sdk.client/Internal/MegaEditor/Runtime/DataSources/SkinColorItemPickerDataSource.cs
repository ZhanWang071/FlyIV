using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.Models;
using Genies.Ugc;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SkinColorItemPickerDataSource", menuName = "Genies/Customizer/DataSource/SkinColorItemPickerDataSource")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SkinColorItemPickerDataSource : ColorItemPickerDataSource
#else
    public class SkinColorItemPickerDataSource : ColorItemPickerDataSource
#endif
    {
        /// <summary>
        /// The event name to dispatch to analytics
        /// </summary>
        protected override string ColorAnalyticsEventName => CustomizationAnalyticsEvents.SkinColorPresetClickEvent;

        /// <summary>
        /// Default Skin Color loaded from ColorAsset with the Id to be <see cref="UnifiedDefaults.DefaultSkinColor"/>
        /// </summary>
        private SkinColorData _defaultSkinColorData;
        public SkinColorData PreviousSkinColorData { get; private set; }
        public SkinColorData CurrentSkinColorData { get; set; }

        protected override void ConfigureProvider()
        {
            if (_uiProvider == null)
            {
                var config = UIDataProviderConfigs.SkinColorPresetsConfig;
                SetUIProvider(config, ServiceManager.Get<IAssetsService>());
            }
        }

        public override void StartCustomization()
        {
            CurrentSkinColorData = ColorAssetToSkinColorData(CurrentCustomizableAvatar.GetColor(GenieColor.Skin) ?? Color.black);
            PreviousSkinColorData = CurrentSkinColorData;

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
            _ids ??= await GetUIProvider<ColoredInventoryAsset, SimpleColorUiData>().GetAllAssetIds(
                categories: new List<string>{ ColorPresetType.Skin.ToString().ToLower() },
                pageSize: InventoryConstants.DefaultPageSize);

            return await GetDataForIndexBaseAsync<ColoredInventoryAsset, SimpleColorUiData>(index, "SkinColorItemPicker");
        }

        /// <summary>
        /// Creates the command to equip the skin color.
        /// </summary>
        protected override UniTask<ICommand> CreateEquipCommandAsync(SimpleColorUiData colorData, CancellationToken cancellationToken)
        {
            CurrentSkinColorData = new SkinColorData { BaseColor = colorData.InnerColor };

            // Update avatar skin color
            return UniTask.FromResult<ICommand>(new EquipSkinColorCommand(colorData.InnerColor, CurrentCustomizableAvatar));
        }

        /// <summary>
        /// Checks if a skin color is currently equipped using EquippedSkinColorIds.
        /// </summary>
        protected override bool IsColorEquipped(string assetId)
        {
            return EquippedSkinColorIds.Contains(assetId);
        }

        /// <summary>
        /// Called before enabling edit on long press. Sets the current skin color data.
        /// </summary>
        protected override void OnLongPressBeforeEnableEdit(SimpleColorUiData colorData)
        {
            // Set the current customizable skin color in the global service
            if (colorData?.InnerColor != null)
            {
                CurrentSkinColorData = new SkinColorData { BaseColor = colorData.InnerColor };
            }
        }

        /// <summary>
        /// Gets which skin color UI is selected by comparing the actual color values.
        /// </summary>
        /// <remarks>Compares the avatar's current skin color with each preset's color to find a match.
        /// Falls back to base implementation (IsAssetEquipped check) if data isn't cached yet.</remarks>
        /// <returns>the index of the UI item. -1 if none is selected.</returns>
        public override int GetCurrentSelectedIndex()
        {
            var currentSkinColor = CurrentCustomizableAvatar.GetColor(GenieColor.Skin) ?? Color.black;

            // Use base helper method to find matching color
            var index = GetCurrentSelectedIndexByColor<SimpleColorUiData>(
                currentSkinColor,
                data => data.InnerColor);

            // Fallback to base implementation (asset ID check) if no match found
            return index >= 0 ? index : base.GetCurrentSelectedIndex();
        }

        private static SkinColorData ColorAssetToSkinColorData(Color color)
        {
            return new SkinColorData(){ BaseColor = color,};
        }

    }
}
