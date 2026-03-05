using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Data source for the hair editing view and controls. Maintains UI icon data and hair color data.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "EyebrowColorItemPickerDataSource", menuName = "Genies/Customizer/DataSource/EyebrowColorItemPickerDataSource")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class EyebrowColorItemPickerDataSource : FlairColorItemPickerDataSource
#else
    public class EyebrowColorItemPickerDataSource : FlairColorItemPickerDataSource
#endif
    {
    }
}
