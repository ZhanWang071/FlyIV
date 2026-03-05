using System;
using UnityEngine;

namespace Genies.Customization.Framework.ItemPicker
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class LayoutConfigBase
#else
    public class LayoutConfigBase
#endif
    {
        public RectOffset padding;
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class HorizontalOrVerticalLayoutConfig : LayoutConfigBase
#else
    public class HorizontalOrVerticalLayoutConfig : LayoutConfigBase
#endif
    {
        public float spacing = 8;
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GridLayoutConfig : LayoutConfigBase
#else
    public class GridLayoutConfig : LayoutConfigBase
#endif
    {
        public Vector2 spacing = new Vector2(8, 8);
        public int columnCount = 4;
        public Vector2 cellSize = new Vector2(88, 96);
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ItemPickerLayoutConfig
#else
    public class ItemPickerLayoutConfig
#endif
    {
        public HorizontalOrVerticalLayoutConfig horizontalOrVerticalLayoutConfig;
        public GridLayoutConfig gridLayoutConfig;
    }
}