using UnityEngine;

/// <summary>
/// Implement this on any MonoBehaviour attached to the export target to
/// inject custom per-frame logic after AnimationMode.SampleAnimationClip runs.
/// Useful for pipelines where a secondary system (IK solvers, Puppet2D, etc.)
/// must propagate sampled pose data before the frame is rendered.
/// </summary>
public interface IAnimationExportFrameHook
{
    void OnFrameSampled(GameObject target, float time);
}