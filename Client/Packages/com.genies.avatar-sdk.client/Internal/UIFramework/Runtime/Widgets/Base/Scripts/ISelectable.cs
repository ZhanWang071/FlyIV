namespace Genies.UI.Widgets
{
    /// <summary>
    /// Interface for UI widgets that can be selected and deselected.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ISelectable
#else
    public interface ISelectable
#endif
    {
        /// <summary>
        /// Gets or sets a value indicating whether this widget is currently selected.
        /// </summary>
        bool IsSelected { get; set; }
    }
}