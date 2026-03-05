using UnityEditor;
using UnityEngine;

namespace Genies.Naf.Editor
{
    [CustomEditor(typeof(NafSettings))]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NafSettingsEditor : UnityEditor.Editor
#else
    public class NafSettingsEditor : UnityEditor.Editor
#endif
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (target is not NafSettings settings)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (GUILayout.Button("Apply"))
            {
                settings.Apply();
            }
        }
    }
}