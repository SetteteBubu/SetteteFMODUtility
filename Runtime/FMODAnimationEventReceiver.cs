using FMOD.Studio;
using FMODUnity;
using System;
using UnityEngine;

[AddComponentMenu("FMOD/FMOD Animation Event Receiver")]
[Obsolete("Was used to trigger preview events but now they are tied directly to data configured in SetteteFMODUtilityManager")]
public class FMODAnimationEventReceiver : MonoBehaviour
{
    public void PlayOneShot2D(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

/*#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            FMODEditorPreview.PlayOneShot(eventPath);
            return;
        }
#endif*/
        RuntimeManager.PlayOneShot(eventPath);
    }

    public void PlayOneShot3D(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

/*#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // EditorUtils.System has no 3D listener so position is irrelevant,
            // but you can still set 3D attributes if the event uses spatialization
            FMODUnity.EditorUtils.System.getEvent(eventPath, out EventDescription desc);
            desc.createInstance(out EventInstance instance);

            var attrs = FMODUnity.RuntimeUtils.To3DAttributes(transform.position);
            instance.set3DAttributes(attrs);

            instance.start();
            instance.release();
            return;
        }
#endif*/
        RuntimeManager.PlayOneShot(eventPath, transform.position);
    }

    public void StartLoop(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

/*#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var instance = FMODEditorPreview.StartLoop(eventPath);
            FMODLoopRegistry.RegisterEditorInstance(eventPath, gameObject, instance);
            return;
        }
#endif*/
        FMODLoopRegistry.Start(eventPath, gameObject);
    }

    public void StopLoop(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

/*#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            FMODLoopRegistry.StopEditorInstance(eventPath, gameObject);
            return;
        }
#endif*/
        FMODLoopRegistry.Stop(eventPath, gameObject);
    }

    private void OnDestroy()
    {
        FMODLoopRegistry.StopAll(gameObject);
    }
}