using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Assets.Services;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Handles customizing the avatar blendshapes (nose, lips, etc..)
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeCustomizationController", menuName = "Genies/Customizer/Controllers/Blend Shape Customization Controller")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BlendShapeCustomizationController : InventoryCustomizationController, IItemPickerDataSource
#else
    public class BlendShapeCustomizationController : InventoryCustomizationController, IItemPickerDataSource
#endif
    {
        [SerializeField] private AvatarBaseCategory _blendShapeSubcategory;

        /// <summary>
        /// Connected Chaos customization node's controller
        /// Used to reset all custom vector values when equipped preset has changed
        /// </summary>
        [SerializeField] private FaceVectorCustomizationController _chaosCustomizer;

        /// <summary>
        /// Eye color data source for the SecondaryItemPicker (only used when subcategory is Eyes)
        /// </summary>
        public EyeColorItemPickerDataSource dataSource;

        private string _stringSubcategory;

        private readonly Dictionary<string, AvatarBaseCategory> _subcategoryMap = new()
        {
            { "eyes", AvatarBaseCategory.Eyes },
            { "jaw", AvatarBaseCategory.Jaw },
            { "lips", AvatarBaseCategory.Lips },
            { "nose", AvatarBaseCategory.Nose },
        };

        private string _lastSelectedBlendShape = "None";

        private CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransactionName => CustomInstrumentationOperations.CreateNewLookTransaction;
        private string _categorySpan;
        private string _previousSpan;
        private bool _isEditable;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            InitializeUIProvider(
                UIDataProviderConfigs.DefaultAvatarBaseConfig,
                ServiceManager.Get<IAssetsService>()
            );

            _customizer = customizer;
            _stringSubcategory = _blendShapeSubcategory.ToString().ToLower();
            _isEditable = false;

            if (dataSource != null)
            {
                dataSource.Initialize(customizer);
            }

            _loadedData = new();
            _ids = new();
            return UniTask.FromResult(true);
        }

        public async UniTask<int> InitializeAndGetCountAsync(int? pageSize, CancellationToken cancellationToken)
        {
            return await InitializeAndGetCountBaseAsync(cancellationToken, null, _stringSubcategory);
        }

        public override void StartCustomization()
        {
            _categorySpan = _InstrumentationManager.StartChildSpanUnderTransaction(_RootTransactionName,
                nameof(BlendShapeCustomizationController), $"open face - {_stringSubcategory} category");

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeCustomizationStarted);

            AddListeners();

            // Show eye color picker in SecondaryItemPicker when customizing eyes
            if (dataSource != null && _blendShapeSubcategory == AvatarBaseCategory.Eyes)
            {
                dataSource.StartCustomization();
                _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
            }

            //Aim the camera at the body area
            ActivateCamera();
            ShowPrimaryPicker(this);

            // Scroll to selected eye color
            if (_blendShapeSubcategory == AvatarBaseCategory.Eyes)
            {
                ScrollToSelectedItemInSecondaryPicker(dataSource).Forget();
            }
        }

        public override void StopCustomization()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _InstrumentationManager.FinishChildSpan(_categorySpan);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeCustomizationStopped);

            RemoveListeners();

            if (dataSource != null && _blendShapeSubcategory == AvatarBaseCategory.Eyes)
            {
                _customizer.View.EditOrDeleteController.DeactivateButtonsImmediately();
                _customizer.View.SecondaryItemPicker.Hide();
                dataSource.StopCustomization();
            }

            //Aim the camera at the body area
            ResetCamera();
            HidePrimaryPicker();
        }

        public override void OnUndoRedo()
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            RefreshPrimaryPickerSelection();
        }

        private void AddListeners()
        {
            if (dataSource != null && _blendShapeSubcategory == AvatarBaseCategory.Eyes && _customizer?.View?.EditOrDeleteController != null)
            {
                _customizer.View.EditOrDeleteController.OnEditClicked += EditCustomEyeColorData;
                _customizer.View.EditOrDeleteController.OnDeleteClicked += DeleteCustomEyeColorData;
                _customizer.View.SecondaryItemPicker.OnScroll += CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            }
        }

        private void RemoveListeners()
        {
            if (_customizer?.View?.EditOrDeleteController != null)
            {
                _customizer.View.EditOrDeleteController.OnEditClicked -= EditCustomEyeColorData;
                _customizer.View.EditOrDeleteController.OnDeleteClicked -= DeleteCustomEyeColorData;
                _customizer.View.SecondaryItemPicker.OnScroll -= CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            }
        }

        private void EditCustomEyeColorData()
        {
            CurrentCustomColorViewState = CustomColorViewState.Edit;
            _customizer.GoToEditItemNode();
        }

        private async void DeleteCustomEyeColorData()
        {
            if (dataSource == null)
            {
                return;
            }

            // The overall logic of deleting is to first update visuals (avatar eye color, UI) for immediate feedback,
            // while deleting the data in the backend async.
            var deletedDataId = dataSource.CurrentLongPressColorData.AssetId; // this Id is same for AssetId, and UiData.AssetId

            // Trigger the animation of closing the edit and delete button and forget.
            _customizer.View.EditOrDeleteController.DisableAndDeactivateButtons().Forget();

            // For avatar eye color change, equip the next color available in the UI list.
            // Currently we should have the preset ones always available (since they are non-editable), so next will always be available.
            // In the future, we might want to make the preset ones editable, in which case if next is not available, equip the previous one.
            // If previous is not available (that means we only have one in the list before deleting), set to the default.
            var nextIndexToEquip = dataSource.CurrentLongPressIndex + 1;
            Ref<SimpleColorUiData> nextUiDataRef = await dataSource.GetDataForIndexAsync(nextIndexToEquip); // this can be sync if the data exists in the cache

            // Update avatar eye color
            await SetEyeColorUsingCommandAsync(nextUiDataRef.Item.AssetId);

            // Delete the data in the backend
            //await EyeColorServiceInstance.DeleteCustomEyeColorAsync(deletedDataId);

            // Dispose current data source, reload data from backend, and reinitialize
            dataSource.Dispose();
            await dataSource.InitializeAndGetCountAsync(InventoryConstants.DefaultPageSize, new());

            // Call the picker show the updated view
            _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
        }

        private static async UniTask SetEyeColorUsingCommandAsync(string colorId)
        {
            ICommand command = new EquipNativeAvatarAssetCommand(colorId, CurrentCustomizableAvatar);
            await command.ExecuteAsync(new CancellationTokenSource().Token);
        }

        private void CloseEditOrDeleteButtonsWhenCrossingLeftMargin()
        {
            var editOrDeleteController = _customizer.View.EditOrDeleteController;
            if (editOrDeleteController.IsActive && editOrDeleteController.transform.localPosition.x < -120)
            {
                editOrDeleteController.DeactivateButtonsImmediately();
            }
        }


        public int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(id => CurrentCustomizableAvatar.IsAssetEquipped($"{id}"));
        }

        // Pagination support - properties inherited from base class
        // public bool HasMoreItems and public bool IsLoadingMore are in base class

        public async UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken)
        {
            return await LoadMoreItemsBaseAsync(cancellationToken, null, _stringSubcategory);
        }


        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        private async UniTask<Ref<BasicInventoryUiData>> GetDataForIndexAsync(int index)
        {
            return await GetDataForIndexBaseAsync<BasicInventoryUiData>(index, "BlendShapeCustomization");
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.CustomizeCTA, noneSelectedDelegate: CustomizeSelectedAsync);
        }

        private UniTask<bool> CustomizeSelectedAsync(CancellationToken cancellationToken)
        {
            if (_subcategoryMap.TryGetValue(_stringSubcategory, out var category))
            {
                CurrentDnaCustomizationViewState = category;
            }
            else
            {
                CrashReporter.LogError($"Invalid Customization Selection '{_stringSubcategory}'");
            }

            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ChaosFaceCustomSelectEvent);
            _customizer.RemoveLastSelectedChildForCurrentNode();
            _customizer.GoToCreateItemNode(); //Nipun
            _customizer.GoToEditItemNode();
            return UniTask.FromResult(true);
        }

        private async UniTask<bool> NoneSelectedAsync(CancellationToken cancellationToken)
        {
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            var props = new AnalyticProperties();
            props.AddProperty("LastSelectedBlendShape", _lastSelectedBlendShape);
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.NoBlendShapeSelected, props);

            var equippedId = _ids.FirstOrDefault(id => CurrentCustomizableAvatar.IsAssetEquipped($"{id}"));
            if (string.IsNullOrEmpty(equippedId))
            {
                return false;
            }

            var unequipCmd = new UnequipNativeAvatarAssetCommand($"{equippedId}", CurrentCustomizableAvatar);
            await unequipCmd.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _customizer.RegisterCommand(unequipCmd);
            return true;
        }

        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            if (TryGetLoadedData<BasicInventoryUiData>(index, out var data) is false)
            {
                return false;
            }

            // performance monitoring
            string currentPoseSpan = _InstrumentationManager.StartChildSpanUnderSpan(_categorySpan,data.Item.AssetId, $"{_stringSubcategory} asset id");
            _InstrumentationManager.FinishChildSpan(_previousSpan);
            _previousSpan = currentPoseSpan;

            if (wasSelected && _isEditable)
            {
                _customizer.GoToEditItemNode();
                return true;
            }

            if (!wasSelected && _chaosCustomizer != null)
            {
                _chaosCustomizer.ResetAllValues();
            }

            var command = new EquipNativeAvatarAssetCommand($"{data.Item.AssetId}", CurrentCustomizableAvatar);
            await command.ExecuteAsync(cancellationToken);
            _lastSelectedBlendShape = data.Item?.DisplayName;

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Fire analytics
            var props = new AnalyticProperties();
            props.AddProperty("name", $"{data.Item?.DisplayName}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BlendShapeClickEvent, props);

            _customizer.RegisterCommand(command);

            return true;
        }

        /// <summary>
        /// Initialize the cell view when its visible.
        /// </summary>
        /// <param name="view"> The view to initialize </param>
        /// <param name="index"> Cell index </param>
        /// <param name="isSelected"> If its already selected </param>
        /// <param name="cancellationToken"> The cancellation token </param>
        /// <returns></returns>
        public async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            return await InitializeCellViewBaseAsync<BasicInventoryUiData>(view, index, isSelected, cancellationToken);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (dataSource != null)
            {
                dataSource.Dispose();
            }
            _categorySpan = null;
            _previousSpan = null;
        }
    }
}
