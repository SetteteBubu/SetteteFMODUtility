#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FMODAnimationEventEditorDriver
{
    private static SetteteFMODUtilityManager _editorManager;

    private static SetteteFMODUtilityManager GetEditorManager()
    {
        if (_editorManager != null) return _editorManager;
        _editorManager = Object.FindFirstObjectByType<SetteteFMODUtilityManager>(
            FindObjectsInactive.Include);
        return _editorManager;
    }

    static FMODAnimationEventEditorDriver()
    {
        EditorApplication.update += OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.sceneOpened += (_, __) => _editorManager = null;
        EditorSceneManager.sceneClosing += (_, __) => _editorManager = null;
    }

    private static void OnEditorUpdate()
    {
        if (Application.isPlaying) return;

        if (GetEditorManager() == null || !GetEditorManager().enableAnimationEventsPreview)
        {
            return;
        }

        // Pump the FMOD editor system every frame so audio actually processes
#if !FMOD_LEGACY_API
        FMODUnity.EditorUtils.System.update();
#else
        FMODUnity.RuntimeManager.CoreSystem.update();
#endif

        if (!AnimationMode.InAnimationMode())
        {
            _lastAnimationTime = -1f; // reset so there's no stale crossing on re-entry
            return;
        }

        TryFireCrossedEvents();
    }

    private static float GetAnimationWindowCurrentTime()
    {
        // Grab the AnimationWindow instance via non-public API
        var animWindowType = typeof(Editor).Assembly
            .GetType("UnityEditor.AnimationWindow");

        // FindObjectsOfTypeAll works for EditorWindows too
        var windows = Resources.FindObjectsOfTypeAll(animWindowType);
        if (windows == null || windows.Length == 0) return 0f;

        AnimationWindow window = (AnimationWindow)windows[0];
        return window.time;
    }

    private static float _lastAnimationTime = -1f;

    private static void TryFireCrossedEvents()
    {
        float currentTime = GetAnimationWindowCurrentTime();
        AnimationClip clip = GetAnimationWindowCurrentClip();

        if (clip == null || clip.events.Length == 0)
        {
            _lastAnimationTime = currentTime;
            return;
        }

        float previousTime = _lastAnimationTime < 0f ? currentTime : _lastAnimationTime;

        foreach (var evt in clip.events)
        {
            bool crossed = evt.time > previousTime && evt.time <= currentTime;
            if (!crossed) continue;
            FireEvent(evt);
        }

        _lastAnimationTime = currentTime;
    }

    private static AnimationClip GetAnimationWindowCurrentClip()
    {
        var animWindowType = typeof(Editor).Assembly
            .GetType("UnityEditor.AnimationWindow");

        var windows = Resources.FindObjectsOfTypeAll(animWindowType);
        if (windows == null || windows.Length == 0) return null;

        var window = windows[0];

        var clipProperty = animWindowType.GetProperty("animationClip",
            BindingFlags.Public | BindingFlags.Instance);
        if (clipProperty == null) return null;

        return clipProperty.GetValue(window) as AnimationClip;
    }

    private static void FireEvent(AnimationEvent evt)
    {
        if (string.IsNullOrEmpty(evt.functionName)) return;
        if (evt.functionName != SetteteFMODUtilityManager.AnimationEventsFunctionNameConstant) return;

        FMODEditorPreview.PlayOneShot(evt.stringParameter);
    }

    private static void OnBeforeAssemblyReload()
    {
        // Unsubscribe to avoid stale delegates after recompile
        EditorApplication.update -= OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

        // Stop all FMOD instances so nothing is left orphaned
        //FMODLoopRegistry.StopAll();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // ExitingEditMode fires before the domain reload that enters Play Mode
        //if (state == PlayModeStateChange.ExitingEditMode)
        //FMODLoopRegistry.StopAll();
    }
}
#endif