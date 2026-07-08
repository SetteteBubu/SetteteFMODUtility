using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public static class FMODLoopRegistry
{
    // Key: (gameObject, eventPath) → EventInstance
    private static readonly Dictionary<(int, string), EventInstance> _instances = new();

    public static void Start(string eventPath, GameObject owner)
    {
        var key = (owner.GetInstanceID(), eventPath);
        if (_instances.ContainsKey(key)) return; // already playing

        EventInstance instance = RuntimeManager.CreateInstance(eventPath);

#if UNITY_2022_3
        RuntimeManager.AttachInstanceToGameObject(instance, transform);
#else
        RuntimeManager.AttachInstanceToGameObject(instance, owner);
#endif

        instance.start();
        _instances[key] = instance;
    }

    public static void Stop(string eventPath, GameObject owner)
    {
        var key = (owner.GetInstanceID(), eventPath);
        if (!_instances.TryGetValue(key, out var instance)) return;

        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        _instances.Remove(key);
    }

    public static void StopAll(GameObject owner)
    {
        int id = owner.GetInstanceID();
        foreach (var key in new List<(int, string)>(_instances.Keys))
        {
            if (key.Item1 != id) continue;
            _instances[key].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _instances[key].release();
            _instances.Remove(key);
        }
    }

/*
#if UNITY_EDITOR
public static void StopAll()
{
    foreach (var instance in _editorInstances.Values)
    {
        EditorUtils.PreviewStop(instance);
    }
    _editorInstances.Clear();
}


private static readonly Dictionary<(int, string), FMOD.Studio.EventInstance> _editorInstances = new();

public static void RegisterEditorInstance(string eventPath, GameObject owner, FMOD.Studio.EventInstance instance)
{
    var key = (owner.GetInstanceID(), eventPath);
    if (_editorInstances.TryGetValue(key, out var existing))
    {
        EditorUtils.PreviewStop(existing);
    }
    _editorInstances[key] = instance;
}

public static void StopEditorInstance(string eventPath, GameObject owner)
{
    var key = (owner.GetInstanceID(), eventPath);
    if (!_editorInstances.TryGetValue(key, out var instance)) return;

    FMODEditorPreview.StopLoop(instance);
    _editorInstances.Remove(key);
}
#endif
*/
}