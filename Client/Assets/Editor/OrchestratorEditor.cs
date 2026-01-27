using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Orchestrator))]
public class OrchestratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Orchestrator script = (Orchestrator)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Reset Conversation"))
        {
            script.ResetConversation();
        }
    }
}