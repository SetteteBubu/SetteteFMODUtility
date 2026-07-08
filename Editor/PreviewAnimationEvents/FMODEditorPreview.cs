#if UNITY_EDITOR
using FMOD.Studio;
using FMODUnity;

public static class FMODEditorPreview
{
    public static void PlayOneShot(string eventPath)
    {
        EditorEventRef editorEvent = EventManager.EventFromPath(eventPath);

        SetteteFMODUtilityManager.LoadFMODPreviewBanks();

        var instance = FMODUnity.EditorUtils.PreviewEvent(editorEvent, null);
        instance.release(); // release immediately = one-shot behavior, FMOD cleans it up when done
    }

    public static EventInstance StartLoop(string eventPath)
    {
        EditorEventRef editorEvent = EventManager.EventFromPath(eventPath);

        SetteteFMODUtilityManager.LoadFMODPreviewBanks();

        var instance = FMODUnity.EditorUtils.PreviewEvent(editorEvent, null);

        return instance;
    }

    public static void StopLoop(EventInstance instance)
    {
        FMODUnity.EditorUtils.PreviewStop(instance);
    }
}
#endif