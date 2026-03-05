using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Inventory.UIData;
using Genies.Refs;
using Genies.UI.Widgets;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Base class for color item picker data sources (EyeColor, SkinColor, etc.)
    /// Contains common functionality for simple color-based item pickers
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class ColorItemPickerDataSource : CustomizationItemPickerDataSource
#else
    public abstract class ColorItemPickerDataSource : CustomizationItemPickerDataSource
#endif
    {
        [SerializeField]
        protected NoneOrNewCTAController _Cta;

        /// <summary>
        /// The event name to dispatch to analytics (set by derived classes)
        /// </summary>
        protected abstract string ColorAnalyticsEventName { get; }

        protected LongPressCellView _currentLongPressCell; // contains the index
        protected SimpleColorUiData _currentLongPressColorData;

        public SimpleColorUiData CurrentLongPressColorData => _currentLongPressColorData;
        public int CurrentLongPressIndex => _currentLongPressCell != null ? _currentLongPressCell.Index : -1;

        protected virtual Shader _ColorShader => Shader.Find("Genies/ColorPresetIcon");

        // Material properties for the color shader
        private const float Border = -1.81f;
        private static readonly int s_border = Shader.PropertyToID("_Border");
        private static readonly int s_innerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int s_midColor = Shader.PropertyToID("_MidColor");

        public override ItemPickerCtaConfig GetCtaConfig()
        {
            return null;
        }

        public override ItemPickerLayoutConfig GetLayoutConfig()
        {
            return new ItemPickerLayoutConfig()
            {
                horizontalOrVerticalLayoutConfig = new HorizontalOrVerticalLayoutConfig() { padding = new RectOffset(16, 16, 28, 28), spacing = 12, },
                gridLayoutConfig = new GridLayoutConfig() { cellSize = new Vector2(56, 56), columnCount = 5, padding = new RectOffset(16, 16, 24, 8), spacing = new Vector2(16, 16), },
            };
        }

        /// <summary>
        /// Gets which color UI is selected.
        /// </summary>
        /// <returns>the index of the UI item. -1 if none is selected.</returns>
        public override int GetCurrentSelectedIndex()
        {
            return GetCurrentSelectedIndexBase(CurrentCustomizableAvatar.IsAssetEquipped);
        }

        public override async UniTask<int> InitializeAndGetCountAsync(int? pageSize, CancellationToken token)
        {
            int count = await base.InitializeAndGetCountAsync(pageSize, token);

            // Preload all data so we can show which color is selected
            for (int i = 0; i < _ids.Count; i++)
            {
                await GetDataForIndexAsync(i);
            }

            return count;
        }

        /// <summary>
        /// Helper method to find selected index by comparing a single color value.
        /// Handles dead refs by reloading them asynchronously.
        /// </summary>
        protected int GetCurrentSelectedIndexByColor<TUI>(Color currentColor, System.Func<TUI, Color> getColorFunc, float tolerance = 0.01f)
            where TUI : class, IAssetUiData
        {
            if (_ids == null)
            {
                return -1;
            }

            for (var index = 0; index < _ids.Count; index++)
            {
                TryGetLoadedData(index, out Ref<TUI> dataRef);

                var presetColor = getColorFunc(dataRef.Item);
                if (ColorsMatch(currentColor, presetColor, tolerance))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Get cached data if exists else load a new ref.
        /// </summary>
        /// <param name="index"> Item index </param>
        public abstract UniTask<Ref<SimpleColorUiData>> GetDataForIndexAsync(int index);

        /// <summary>
        /// Business logic for what happens when a cell is clicked.
        /// </summary>
        public override async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            //Load the ui data.
            if (TryGetLoadedData<SimpleColorUiData>(index, out var dataRef) is false)
            {
                return false;
            }

            if (!dataRef.IsAlive || dataRef.Item == null)
            {
                return false;
            }

            var longPressCellView = clickedCell as LongPressCellView;

            if (longPressCellView != null && _editOrDeleteController.IsActive)
            {
                //If the edit and delete buttons are present, disable them
                _editOrDeleteController.DisableAndDeactivateButtons().Forget();
            }

            // Update selection and index for the clicked cell.
            clickedCell.ToggleSelected(true);
            clickedCell.Index = index;

            // Execute the command (implemented by derived classes)
            ICommand command = await CreateEquipCommandAsync(dataRef.Item, cancellationToken);
            if (command == null)
            {
                return false;
            }

            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var props = new AnalyticProperties();
            props.AddProperty("name", $"{dataRef.Item.DisplayName}");
            AnalyticsReporter.LogEvent(ColorAnalyticsEventName, props);

            _customizer.RegisterCommand(command);

            return true;
        }

        /// <summary>
        /// Initialize the cell view when its visible.
        /// </summary>
        public override async UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            Ref<SimpleColorUiData> dataRef = await GetDataForIndexAsync(index);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var longPressCellView = view as LongPressCellView;
            if (longPressCellView != null && dataRef.IsAlive && dataRef.Item != null)
            {
                if (longPressCellView.OnLongPress == null)
                {
                    longPressCellView.OnLongPress += OnLongPress;

                    if (longPressCellView.Index < 0)
                    {
                        longPressCellView.Index = index;
                    }
                }

                var colorPresetUiData = dataRef.Item;
                if (colorPresetUiData == null)
                {
                    // maybe add current equipped color data to the material
                    return false;
                }

                //longPressCellView.thumbnail.material = colorPresetUiData.GetIconMaterial();
                longPressCellView.thumbnail.material = GetMaterial(colorPresetUiData.InnerColor);

                bool isEquipped = IsColorEquipped(colorPresetUiData.AssetId);
                bool shouldShowEditableIcon = ShouldShowEditableIcon(colorPresetUiData, isEquipped);

                // since we use long press to edit, we hide the editable icon on the view
                if (shouldShowEditableIcon)
                {
                    longPressCellView.SetShowEditableIcon(colorPresetUiData.IsEditable && !isEquipped);
                }

                longPressCellView.SetDebuggingAssetLabel(dataRef.Item.AssetId);
            }

            return true;
        }

        /// <summary>
        /// Called when a cell is long pressed. Derived classes can override to add custom logic.
        /// </summary>
        protected virtual async void OnLongPress(LongPressCellView longPressCellView)
        {
            if (_currentLongPressCell == longPressCellView && _editOrDeleteController.IsActive)
            {
                return;
            }

            if (longPressCellView.Index < 0)
            {
                return;
            }

            _currentLongPressCell = longPressCellView;

            // Get the current data of the cell that is being long pressed
            Ref<SimpleColorUiData> longPressColorDataRef = await GetDataForIndexAsync(_currentLongPressCell.Index);
            _currentLongPressColorData = longPressColorDataRef.Item;

            // Check if the current color is equipped
            if (IsColorEquipped(_currentLongPressColorData.AssetId))
            {
                Debug.Log($"Return because the color is equipped!");
                return;
            }

            if (!_currentLongPressColorData.IsEditable)
            {
                return;
            }

            // Allow derived classes to perform additional setup before enabling edit
            OnLongPressBeforeEnableEdit(_currentLongPressColorData);

            // Enable Edit
            await _editOrDeleteController.Enable(_currentLongPressCell.gameObject);
        }

        /// <summary>
        /// Called before enabling edit on long press. Derived classes can override to set up color data.
        /// </summary>
        protected virtual void OnLongPressBeforeEnableEdit(SimpleColorUiData colorData)
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Creates the command to equip the color. Must be implemented by derived classes.
        /// </summary>
        protected abstract UniTask<ICommand> CreateEquipCommandAsync(SimpleColorUiData colorData, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a color is currently equipped. Derived classes can override for custom logic.
        /// </summary>
        protected virtual bool IsColorEquipped(string assetId)
        {
            return CurrentCustomizableAvatar.IsAssetEquipped(assetId);
        }

        /// <summary>
        /// Determines if the editable icon should be shown. Derived classes can override.
        /// </summary>
        protected virtual bool ShouldShowEditableIcon(SimpleColorUiData colorData, bool isEquipped)
        {
            return colorData.IsEditable && !isEquipped;
        }

        /// <summary>
        /// Creates a material for displaying the color icon.
        /// </summary>
        protected Material GetMaterial(Color color)
        {
            Material iconMaterial = new Material(_ColorShader);

            var mainColor = color;
            mainColor.a = 1f;

            iconMaterial.SetFloat(s_border, Border);
            iconMaterial.SetColor(s_innerColor, mainColor);
            iconMaterial.SetColor(s_midColor, Color.white);

            return iconMaterial;
        }
    }
}

