#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FMODAnimationEventEditorDriver
{
    private static double _lastTime;

    static FMODAnimationEventEditorDriver()
    {
        EditorApplication.update += OnEditorUpdate;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnEditorUpdate()
    {
        if (Application.isPlaying) return;

        // Pump the FMOD editor system every frame so audio actually processes
        FMODUnity.EditorUtils.System.update();

        if (!AnimationMode.InAnimationMode())
        {
            _lastAnimationTime = -1f; // reset so there's no stale crossing on re-entry
            return;
        }

        Animator[] animators = GameObject.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var animator in animators)
        {
            if (animator == null) continue;

            TryFireCrossedEvents(animator);
        }
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

    private static void TryFireCrossedEvents(Animator animator)
    {
        if (animator.layerCount == 0) return;

        float currentTime = GetAnimationWindowCurrentTime();

        for (int i = 0; i < animator.layerCount; i++)
        {
            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(i);

            foreach (var clipInfo in clipInfos)
            {
                AnimationClip clip = clipInfo.clip;
                if (clip == null || clip.events.Length == 0) continue;

                float previousTime = _lastAnimationTime < 0f ? currentTime : _lastAnimationTime;

                foreach (var evt in clip.events)
                {
                    bool crossed = evt.time > previousTime && evt.time <= currentTime;
                    if (!crossed) continue;

                    FireEvent(evt);
                }
            }
        }

        _lastAnimationTime = currentTime;
    }

    private static void FireEvent(AnimationEvent evt)
    {
        if (string.IsNullOrEmpty(evt.functionName)) return;

        //Look through our registered FMOD Preview events and fire it
        SetteteFMODUtilityManager setteteFMODUtilityManagerGameObject = GameObject.FindFirstObjectByType<SetteteFMODUtilityManager>(FindObjectsInactive.Include);
        if(setteteFMODUtilityManagerGameObject != null)
        {
            foreach(var previewEventData in setteteFMODUtilityManagerGameObject.previewAnimationEventDatas)
            {
                if(previewEventData.animationEventFunctionName == evt.functionName &&
                    previewEventData.animationEventStringParameter == evt.stringParameter)
                {
                    FMODEditorPreview.PlayOneShot(previewEventData.previewEvent.Path);
                }
            }
        }
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