#if UNITY_EDITOR
using UnityEditor;
using FMODUnity;
using FMOD.Studio;

[CustomEditor(typeof(SetteteProximityArea))]
public class SettetePrximityAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var component = (SetteteProximityArea)target;
        var eventField = serializedObject.FindProperty("fmodEvent");

        // Grab the path string from the EventReference struct
        var pathProp = eventField.FindPropertyRelative("Path");
        string path = pathProp?.stringValue ?? "";

        EditorGUILayout.Space(8);

        if (component.gameObject.activeInHierarchy)
        {
            // ── No event referenced ──────────────────────────────
            if (string.IsNullOrEmpty(path))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ No FMOD event referenced. Settete Proximity Area can't work without this.",
                    MessageType.Warning
                );
                return;
            }

            // ── Event referenced — check if it's 3D ─────────────
            try
            {
                // In editor we use EditorUtils.System to inspect the event
                EventDescription desc;

                if (EditorApplication.isPlaying)
                {
                    RuntimeManager.StudioSystem.getEvent(path, out desc);
                }
                else
                {
                    EditorUtils.System.getEvent(path, out desc);
                }

                desc.is3D(out bool is3D);

                if (!is3D)
                {
                    EditorGUILayout.HelpBox(
                        "❌ Referenced event is not 3D. Please use a 3D event or the Settete Proximity Area won't work.",
                        MessageType.Error
                    );
                }
                else
                {
                    desc.getMinMaxDistance(out float min, out float max);
                    EditorGUILayout.HelpBox(
                        $"✅ 3D event detected. Attenuation range: {min:F1}m – {max:F1}m  |  Sphere radius: {max:F1}m",
                        MessageType.Info
                    );
                }
            }
            catch
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Could not query FMOD event description. Make sure FMOD banks are loaded in the editor.",
                    MessageType.Warning
                );
            }
        }
    }
}
#endif