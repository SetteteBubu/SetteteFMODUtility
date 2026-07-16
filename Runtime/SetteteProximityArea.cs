using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SetteteProximityArea : MonoBehaviour
{
    [Header("FMOD Event")]
    [SerializeField] private EventReference fmodEvent;
    [SerializeField] private bool startStopEventAutomatically = true;
    [Tooltip("Whether the proximity area range should resynch with the fmod event attenuation curve or just fetch it at start")]
    [SerializeField] private bool resynchFmodEventAttenuationCurve = false;
    [Tooltip("Resynch refresh rate in seconds between proximity area and event attenuation curve")]
    [SerializeField] private float resynchFmodEventAttenuationCurveRate = 0.5f;

    [Header("Gizmo")]
    [SerializeField] private bool drawGizmo = false;
    [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.25f);

    [Header("Events")]
    public UnityEvent<int> OnAnyListenerEnter;   // passes listener index
    public UnityEvent OnAllListenersExit;

    // Runtime state
    private float _radius = 0f;
    private EventInstance _eventInstance;
    private bool _eventPlaying = false;
    private HashSet<int> _listenersInside = new();
    private float resynchEventCurveCounter = 0f;

    // ─────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────

    private void Start()
    {
        _radius = GetAttenuationMaxDistance();
    }

    private void Update()
    {
        if (_radius <= 0f) return;

        HandleEventResynch();

        int listenerCount = RuntimeManager.StudioSystem.getNumListeners(out int count)
            == FMOD.RESULT.OK ? count : 0;

        for (int i = 0; i < listenerCount; i++)
        {
            Vector3 listenerPos = GetListenerPosition(i);
            bool isInside = IsInsideSphere(listenerPos);
            bool wasInside = _listenersInside.Contains(i);

            if (isInside && !wasInside)
            {
                _listenersInside.Add(i);

                OnAnyListenerEnter?.Invoke(i);

                if (startStopEventAutomatically)
                    EnsureEventPlaying();
            }
            else if (!isInside && wasInside)
            {
                _listenersInside.Remove(i);

                if (_listenersInside.Count == 0)
                {
                    OnAllListenersExit?.Invoke();

                    if (startStopEventAutomatically)
                        StopEvent();
                }
            }
        }
    }

    void HandleEventResynch()
    {
        if (resynchFmodEventAttenuationCurve)
        {
            resynchEventCurveCounter += Time.deltaTime;
            if (resynchEventCurveCounter >= resynchFmodEventAttenuationCurveRate)
            {
                resynchEventCurveCounter = 0f;
                ResynchEvent();
            }
        }
    }

    void ResynchEvent()
    {
        _radius = GetAttenuationMaxDistance();
    }

    private void OnDestroy()
    {
        StopEvent();
    }

    // ─────────────────────────────────────────
    //  Geometry
    // ─────────────────────────────────────────

    private bool IsInsideSphere(Vector3 point)
    {
        return (point - transform.position).sqrMagnitude <= _radius * _radius;
    }

    // ─────────────────────────────────────────
    //  FMOD helpers
    // ─────────────────────────────────────────

    private Vector3 GetListenerPosition(int listenerIndex)
    {
        RuntimeManager.StudioSystem.getListenerAttributes(
            listenerIndex,
            out FMOD.ATTRIBUTES_3D attributes
        );
        return new Vector3(attributes.position.x, attributes.position.y, attributes.position.z);
    }

    private float GetAttenuationMaxDistance()
    {
        if (fmodEvent.IsNull) return 0f;

        if (Application.isPlaying)
        {
            try
            {
                RuntimeManager.StudioSystem.getEventByID(fmodEvent.Guid, out EventDescription desc);
                desc.is3D(out bool is3D);
                if (!is3D) return 0f;

                desc.getMinMaxDistance(out float min, out float max);
                return max;
            }
            catch
            {
                return 0f;
            }
        }
#if UNITY_EDITOR
        else
        {
            SetteteFMODUtilityManager.LoadFMODPreviewBanks();

#if !FMOD_LEGACY_API
            EditorUtils.System.getEvent(fmodEvent.Path, out EventDescription desc);
#else
            FMODUnity.RuntimeManager.StudioSystem.getEvent(fmodEvent.Path, out EventDescription desc);
#endif

            desc.is3D(out bool is3D);
            if (!is3D) return 0f;

            desc.getMinMaxDistance(out float min, out float max);
            return max;
        }
#endif

        return 0f;
    }

    private void EnsureEventPlaying()
    {
        if (_eventPlaying) return;

        _eventInstance = RuntimeManager.CreateInstance(fmodEvent);

#if FMOD_LEGACY_API
        RuntimeManager.AttachInstanceToGameObject(_eventInstance, transform);
#else
        RuntimeManager.AttachInstanceToGameObject(_eventInstance, gameObject);
#endif

        _eventInstance.start();
        _eventPlaying = true;
    }

    private void StopEvent()
    {
        if (!_eventPlaying) return;

        _eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _eventInstance.release();
        _eventPlaying = false;
    }

    // ─────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        float r = Application.isPlaying ? _radius : GetAttenuationMaxDistance();
        if (r <= 0f) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, r);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, r);
    }
}