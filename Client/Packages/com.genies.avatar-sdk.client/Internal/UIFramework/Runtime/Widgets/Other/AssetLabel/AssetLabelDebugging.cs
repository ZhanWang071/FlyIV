using System;
using UnityEngine;

namespace Genies.UI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AssetLabelDebugging
#else
    public static class AssetLabelDebugging
#endif
    {
        private const string EnabledPlayerPrefsKey = "assetLabelsEnabled";

        public static event Action<bool> EnabledStateChanged;
        public static event Action<string> AssetLabelClicked;

        /// <summary>
        /// Enables asset label debugging. It is used by the <see cref="DebuggingAssetLabel"/> UI component
        /// to show/hide itself.
        /// </summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledPlayerPrefsKey, 0) == 1;
            set
            {
                if (Enabled == value)
                {
                    return;
                }

                PlayerPrefs.SetInt(EnabledPlayerPrefsKey, value ? 1 : 0);
                EnabledStateChanged?.Invoke(value);
            }
        }

        public static void NotifyAssetLabelClicked(string label)
        {
            AssetLabelClicked?.Invoke(label);
            GUIUtility.systemCopyBuffer = label;
        }
    }
}
