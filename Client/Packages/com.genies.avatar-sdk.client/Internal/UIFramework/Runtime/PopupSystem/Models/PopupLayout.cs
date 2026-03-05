namespace Genies.UIFramework {
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum PopupLayout
#else
    public enum PopupLayout
#endif
    {
        /// <summary>
        /// A popup with only 1 button
        /// </summary>
        SingleButton = 0,
        /// <summary>
        /// A popup with 2 buttons
        /// </summary>
        DualButton = 1,
        /// <summary>
        /// A popup with a text input field
        /// </summary>
        InputField = 2,
        /// <summary>
        /// A popup with a custom layout (for later DynamicPopupBuilder use)
        /// </summary>
        Custom = 3
    }
}
