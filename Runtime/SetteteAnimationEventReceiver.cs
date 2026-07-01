using FMODUnity;
using UnityEngine;

[AddComponentMenu("Settete/Settete Animation Event Receiver")]
public class SetteteAnimationEventReceiver : MonoBehaviour
{
    public GameObject overrideEventGameObject;
    public void SetteteAudio(string eventPath)
    {
        RuntimeManager.PlayOneShotAttached(eventPath, overrideEventGameObject != null ? overrideEventGameObject : gameObject);
    }
}