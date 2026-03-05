namespace Genies.UIFramework
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum PopupType
#else
    public enum PopupType
#endif
    {
        /// <summary>
        /// Save and exit popup with 2 buttons
        /// </summary>
        SaveAndExit = 0,
        /// <summary>
        /// Notification popup with 2 buttons
        /// </summary>
        Notification = 1,
        /// <summary>
        /// Connection lost popup with 1 button
        /// </summary>
        ConnectionLost = 2
    }
}
