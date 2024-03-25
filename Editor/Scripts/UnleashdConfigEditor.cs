namespace Multiscription.Unleashd
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(UnleashdConfig))]
    [CanEditMultipleObjects]
    public class UnleashdConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UnleashdConfig unleashdConfig = (UnleashdConfig) target;

            if (unleashdConfig.trialDurationMinutes <= 0 && unleashdConfig.trialDurationHours <= 0 && unleashdConfig.trialDurationDays <= 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("NB : Ingame trial not enabled!", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(20);
            if (GUILayout.Button("Open Unleashd Developer Portal"))
            {
                Application.OpenURL("https://developer.unleashd.com/projects");
            }
        }
    }
}
