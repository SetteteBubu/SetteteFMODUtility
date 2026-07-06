using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SetteteProximityArea))]
public class SetteteProximityAreaEditor : Editor
{
    private const string DescriptionText =
        "Automatically turns an FMOD event on or off depending on whether a listener is within " +
        "the event's attenuation curve max distance.\n\n" +
        "This tool can also be used to enable or disable third-party scripts based on listener " +
        "proximity. In that case you still need to assign the FMOD Event field, since the script " +
        "relies on it to read the attenuation curve range \u2014 just set \"Start Stop Event " +
        "Automatically\" to false if the audio itself is played by another script.";

    private bool _descriptionFoldout = true;

    private SerializedProperty _fmodEvent;
    private SerializedProperty _startStopEventAutomatically;
    private SerializedProperty _resynchFmodEventAttenuationCurve;
    private SerializedProperty _resynchFmodEventAttenuationCurveRate;

    private SerializedProperty _drawGizmo;
    private SerializedProperty _gizmoColor;

    private SerializedProperty _onAnyListenerEnter;
    private SerializedProperty _onAllListenersExit;

    private void OnEnable()
    {
        _fmodEvent = serializedObject.FindProperty("fmodEvent");
        _startStopEventAutomatically = serializedObject.FindProperty("startStopEventAutomatically");
        _resynchFmodEventAttenuationCurve = serializedObject.FindProperty("resynchFmodEventAttenuationCurve");
        _resynchFmodEventAttenuationCurveRate = serializedObject.FindProperty("resynchFmodEventAttenuationCurveRate");

        _drawGizmo = serializedObject.FindProperty("drawGizmo");
        _gizmoColor = serializedObject.FindProperty("gizmoColor");

        _onAnyListenerEnter = serializedObject.FindProperty("OnAnyListenerEnter");
        _onAllListenersExit = serializedObject.FindProperty("OnAllListenersExit");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Description (collapsible) ───────────────────────────────
        _descriptionFoldout = EditorGUILayout.Foldout(_descriptionFoldout, "Description", true);
        if (_descriptionFoldout)
        {
            EditorGUILayout.HelpBox(DescriptionText, MessageType.None);
        }

        EditorGUILayout.Space(8);

        // ── Error message, right above "FMOD Event" ─────────────────
        SerializedProperty pathProp = _fmodEvent.FindPropertyRelative("Path");
        bool hasEvent = pathProp != null && !string.IsNullOrEmpty(pathProp.stringValue);

        if (!hasEvent)
        {
            EditorGUILayout.HelpBox(
                "No FMOD Event assigned. The proximity area needs one to read the attenuation curve range.",
                MessageType.Error);
        }

        // ── FMOD Event ───────────────────────────────────────────────
        // (the "FMOD Event" header is drawn automatically from the [Header] attribute)
        EditorGUILayout.PropertyField(_fmodEvent);
        EditorGUILayout.PropertyField(_startStopEventAutomatically);
        EditorGUILayout.PropertyField(_resynchFmodEventAttenuationCurve);
        using (new EditorGUI.DisabledScope(!_resynchFmodEventAttenuationCurve.boolValue))
        {
            EditorGUILayout.PropertyField(_resynchFmodEventAttenuationCurveRate);
        }

        // ── Gizmo ──────────────────────────────────────────────────
        EditorGUILayout.PropertyField(_drawGizmo);
        using (new EditorGUI.DisabledScope(!_drawGizmo.boolValue))
        {
            EditorGUILayout.PropertyField(_gizmoColor);
        }

        // ── Events ───────────────────────────────────────────────────
        EditorGUILayout.PropertyField(_onAnyListenerEnter);
        EditorGUILayout.PropertyField(_onAllListenersExit);

        serializedObject.ApplyModifiedProperties();
    }
}
