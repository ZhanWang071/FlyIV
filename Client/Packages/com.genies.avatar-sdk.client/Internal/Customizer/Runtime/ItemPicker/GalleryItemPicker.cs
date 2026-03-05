using Genies.UIFramework;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;

namespace Genies.Customization.Framework.ItemPicker
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GalleryItemPicker : ScrollingItemPicker
#else
    public class GalleryItemPicker : ScrollingItemPicker
#endif
    {
        public AdjustGridLayoutCellSize adjustGridLayoutCellSize;
        public GeniesButton CollapseButton;

        public new virtual async UniTask Show(IItemPickerDataSource dataSource)
        {
            if (dataSource == null)
            {
                Hide();
                return;
            }

            try
            {
                SetGridLayoutCellSize(dataSource);
            }
            catch (System.Exception ex)
            {
                CrashReporter.LogError($"[GalleryItemPicker] Error setting grid layout cell size: {ex.Message}\n{ex.StackTrace}");
            }

            await base.Show(dataSource);
        }

        public void SetGridLayoutCellSize(IItemPickerDataSource dataSource)
        {
            try
            {
                var gridLayoutConfig = dataSource.GetLayoutConfig().gridLayoutConfig;
                adjustGridLayoutCellSize.SetSize(gridLayoutConfig.cellSize.x, gridLayoutConfig.cellSize.y);
            }
            catch (System.Exception ex)
            {
                CrashReporter.LogError($"[GalleryItemPicker] Error in SetGridLayoutCellSize: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public new virtual void Hide()
        {
            base.Hide();
        }
    }
}
