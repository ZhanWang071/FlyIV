using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Avatars;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Looks.Customization.Commands;
using Genies.MegaEditor;
using Genies.Naf;
using Genies.Refs;
using Genies.Ugc;
using Genies.UI.Widgets;
using Genies.Utilities;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Controller for switching the body type of a unified genie.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BodyPresetCustomizationController : BaseCustomizationController, IItemPickerDataSource
#else
    public class BodyPresetCustomizationController : BaseCustomizationController, IItemPickerDataSource
#endif
    {
        [SerializeField]
        private List<BodyPresetData> _thumbnailData;

        public SkinColorItemPickerDataSource dataSource;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _defaultCellSize = new Vector2(87.5f, 95.8f);
            _customizer = customizer;
            if (dataSource != null)
            {
                dataSource.Initialize(customizer);
            }
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BodyTypeCustomizationStarted);

            AddListeners();

            if (dataSource != null)
            {
                dataSource.StartCustomization();
                _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
            }

            _customizer.View.PrimaryItemPicker.Show(this).Forget();
            ScrollToSelectedItemInSecondaryPicker(dataSource).Forget();
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BodyTypeCustomizationStopped);

            RemoveListeners();

            if (dataSource != null)
            {
                _customizer.View.EditOrDeleteController.DeactivateButtonsImmediately();
                _customizer.View.SecondaryItemPicker.Hide();
                dataSource.StopCustomization();
            }

            _customizer.View.PrimaryItemPicker.Hide();
        }

        private void AddListeners()
        {
            if (dataSource != null && _customizer?.View?.EditOrDeleteController != null)
            {
                _customizer.View.EditOrDeleteController.OnEditClicked += EditCustomSkinColorData;
                _customizer.View.EditOrDeleteController.OnDeleteClicked += DeleteCustomSkinColorData;
                _customizer.View.SecondaryItemPicker.OnScroll += CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            }
        }

        private void RemoveListeners()
        {
            if (_customizer?.View?.EditOrDeleteController != null)
            {
                _customizer.View.EditOrDeleteController.OnEditClicked -= EditCustomSkinColorData;
                _customizer.View.EditOrDeleteController.OnDeleteClicked -= DeleteCustomSkinColorData;
                _customizer.View.SecondaryItemPicker.OnScroll -= CloseEditOrDeleteButtonsWhenCrossingLeftMargin;
            }
        }

        private void EditCustomSkinColorData()
        {
            CurrentCustomColorViewState = CustomColorViewState.Edit;
            _customizer.GoToEditItemNode();
        }

        private async void DeleteCustomSkinColorData()
        {
            if (dataSource == null)
            {
                return;
            }

            // The overall logic of deleting is to first update visuals (avatar skin color, UI) for immediate feedback,
            // while deleting the data in the backend async.
            var deletedDataId = dataSource.CurrentLongPressColorData.AssetId; // this Id is same for AssetId, and UiData.AssetId

            // Trigger the animation of closing the edit and delete button and forget.
            _customizer.View.EditOrDeleteController.DisableAndDeactivateButtons().Forget();

            // For avatar skin color change, equip the next color available in the UI list.
            // Currently we should have the preset ones always available (since they are non-editable), so next will always be available.
            // In the future, we might want to make the preset ones editable, in which case if next is not available, equip the previous one.
            // If previous is not available (that means we only have one in the list before deleting), set to the default.
            var nextIndexToEquip = dataSource.CurrentLongPressIndex + 1;
            Ref<SimpleColorUiData> nextUiDataRef = await dataSource.GetDataForIndexAsync(nextIndexToEquip); // this can be sync if the data exists in the cache

            // Set the current skin color data to the next item
            if (nextUiDataRef.Item?.InnerColor != null)
            {
                dataSource.CurrentSkinColorData = new SkinColorData { BaseColor = nextUiDataRef.Item.InnerColor};
            }

            // Update avatar skin color
            await SetSkinColorUsingCommandAsync(nextUiDataRef.Item.AssetId);

            // Delete the data in the backend
            //await SkinColorServiceInstance.DeleteCustomSkinAsync(deletedDataId);

            // Dispose current data source, reload data from backend, and reinitialize
            dataSource.Dispose();
            await dataSource.InitializeAndGetCountAsync(InventoryConstants.DefaultPageSize, new());

            // Call the picker show the updated view
            _customizer.View.SecondaryItemPicker.Show(dataSource).Forget();
        }

        private static async UniTask SetSkinColorUsingCommandAsync(string colorId)
        {
            await CurrentCustomizableAvatar.UnsetColorAsync(GenieColor.Skin);
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

        public override void Dispose()
        {
            base.Dispose();
            if (dataSource != null)
            {
                dataSource.Dispose();
            }
            foreach (var data in _thumbnailData)
            {
                data.Dispose();
            }
        }

        public override void OnUndoRedo()
        {
            _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.CustomizeCTA, noneSelectedDelegate: CustomizeSelectedAsync);
        }
        private UniTask<bool> CustomizeSelectedAsync(CancellationToken cancellationToken)
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ChaosBodyShapeCustomSelectEvent);
            _customizer.GoToEditItemNode();
            return UniTask.FromResult(true);
        }

        public int GetCurrentSelectedIndex()
        {
            var currPreset = CurrentCustomizableAvatar.GetBodyPreset();

            for (int i = 0; i < _thumbnailData.Count; i++)
            {
                var targetPreset = _thumbnailData[i].Preset;
                if (currPreset.EqualsVisually(targetPreset))
                {

                    return i;
                }
            }

            return -1;
        }

        // Pagination support (default implementation - no pagination for body presets yet)
        public bool HasMoreItems => false;
        public bool IsLoadingMore => false;
        public UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken) => UniTask.FromResult(false);

        public UniTask<int> InitializeAndGetCountAsync(int? pageSize, CancellationToken cancellationToken)
        {
            IsInitialized = true;
            return UniTask.FromResult(_thumbnailData.Count);
        }

        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            var data = _thumbnailData[index];
            if (wasSelected)
            {
                return true;
            }

            //Create command for changing body asset
            var command = new SetNativeAvatarBodyPresetCommand(data.Preset, CurrentCustomizableAvatar);

            //Execute the command
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Analytics
            var props = new AnalyticProperties();
            props.AddProperty("currentBodyType", $"{data.PresetName}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.UserGenderChangedEvent, props);

            //Register the command for undo/redo
            _customizer.RegisterCommand(command);

            return true;
        }

        public UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            var thumbnail = _thumbnailData[index].LoadThumbnail();
            var asGeneric = view as BodyTypeItemPickerCellView;
            if (asGeneric == null)
            {
                return UniTask.FromResult(false);
            }

            asGeneric.thumbnail.sprite = thumbnail;
            asGeneric.SetDebuggingAssetLabel(_thumbnailData[index].PresetName);
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Ui data for body types.
        /// </summary>
        [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
        internal class BodyPresetData
#else
        public class BodyPresetData
#endif
        {
            [AssetPath.Attribute(typeof(GSkelModifierPreset), AssetPath.PathType.Resources)]
            [SerializeField]
            private string _bodyDataPath;
            [NonSerialized]
            private GSkelModifierPreset _bodyDataSO;

            /// <summary>
            /// The preset
            /// </summary>
            public GSkelModifierPreset Preset
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO;
                }
            }

            /// <summary>
            /// The body preset's name (femaleHeavy, femaleSkinny, etc)
            /// </summary>
            public string PresetName
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO.Name;
                }
            }

            /// <summary>
            /// body variation name (female, male, etc)
            /// </summary>
            public string BodyName
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO.StartingBodyVariation;
                }
            }

            [AssetPath.Attribute(typeof(GSkelModifierPresetIcon), AssetPath.PathType.Resources)]
            [SerializeField]
            private string _uiDataPath;
            private GSkelModifierPresetIcon _uiDataSO;

            private Sprite _thumbnail;

            public Sprite LoadThumbnail()
            {
                if (_thumbnail == null)
                {
                    _uiDataSO = AssetPath.Load<GSkelModifierPresetIcon>(_uiDataPath);
                    _thumbnail = _uiDataSO.Icon;
                }

                return _thumbnail;
            }

            /// <summary>
            /// the gender preset (female, male, androgynous, etc)
            /// </summary>
            public GSkelPresetGender PresetGender
            {
                get
                {
                    if (_uiDataSO == null)
                    {
                        _uiDataSO = AssetPath.Load<GSkelModifierPresetIcon>(_uiDataPath);
                    }

                    return _uiDataSO.FilterGender;
                }
            }

            public void Dispose()
            {
                if (_thumbnail != null)
                {
                    Resources.UnloadAsset(_uiDataSO);
                }
                if (_bodyDataSO != null)
                {
                    Resources.UnloadAsset(_bodyDataSO);
                }
            }
        }
    }
}
