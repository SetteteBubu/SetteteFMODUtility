#if UNITY_EDITOR
using FMOD.Studio;
using FMODUnity;

public static class FMODEditorPreview
{
    public static void PlayOneShot(string eventPath)
    {
#if !FMOD_LEGACY_API
        EditorEventRef editorEvent = EventManager.EventFromPath(eventPath);
        SetteteFMODUtilityManager.LoadFMODPreviewBanks();
        var instance = FMODUnity.EditorUtils.PreviewEvent(editorEvent, null);
        instance.release();
#else
    try
    {
        FMODUnity.RuntimeManager.StudioSystem.getEvent(eventPath, out FMOD.Studio.EventDescription desc);
        if (desc.isValid())
        {
            desc.createInstance(out FMOD.Studio.EventInstance instance);
            if (instance.isValid())
            {
                instance.start();
                instance.release();
            }
        }
    }
    catch (System.Exception e)
    {
        Debug.LogWarning($"[FMODEditorPreview] Could not play event '{eventPath}': {e.Message}");
    }
#endif
    }

    /*public static EventInstance StartLoop(string eventPath)
    {
        EditorEventRef editorEvent = EventManager.EventFromPath(eventPath);

        SetteteFMODUtilityManager.LoadFMODPreviewBanks();

        var instance = FMODUnity.EditorUtils.PreviewEvent(editorEvent, null);

        return instance;
    }

    public static void StopLoop(EventInstance instance)
    {
        FMODUnity.EditorUtils.PreviewStop(instance);
    }*/
}
#endif