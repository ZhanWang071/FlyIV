using UnityEngine;

namespace Genies.Editor.MaterialPresetEditors {
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DrawMaterialInspector : PropertyAttribute {
#else
    public class DrawMaterialInspector : PropertyAttribute {
#endif
        public DrawMaterialInspector() { }
    }
}
