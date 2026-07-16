using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Add this component to the root of a Puppet2D character before exporting.
/// After each animation frame is sampled, it manually calls Run() on all
/// Puppet2D_GlobalControl instances in the hierarchy, propagating the animated
/// control positions through IK/splines/parent controls to the skinned mesh bones.
///
/// Uses reflection so the plugin has no hard compile-time dependency on Puppet2D.
/// </summary>
public class AnimationExportPuppet2DBridge : MonoBehaviour, IAnimationExportFrameHook
{
    private MonoBehaviour[] _globalControls;
    private MethodInfo _initializeArraysMethod;
    private MethodInfo _runMethod;
    private bool _initialized = false;

    public void OnFrameSampled(GameObject target, float time)
    {
        if (!_initialized)
            Initialize(target);

        if (_globalControls == null || _runMethod == null) return;

        foreach (var ctrl in _globalControls)
        {
            if (ctrl == null) continue;

            // Ensure cached arrays are up to date before running
            _initializeArraysMethod?.Invoke(ctrl, null);
            _runMethod.Invoke(ctrl, null);
        }
    }

    private void Initialize(GameObject target)
    {
        // Find Puppet2D_GlobalControl type without a hard reference
        Type puppet2DType = null;
        foreach (var b in target.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (b.GetType().Name == "Puppet2D_GlobalControl")
            {
                puppet2DType = b.GetType();
                break;
            }
        }

        if (puppet2DType == null)
        {
            Debug.LogWarning("[AnimationExportPuppet2DBridge] Could not find " +
                "Puppet2D_GlobalControl in hierarchy. Is Puppet2D installed?");
            return;
        }

        _runMethod = puppet2DType.GetMethod(
            "Run", BindingFlags.Public | BindingFlags.Instance);

        _initializeArraysMethod = puppet2DType.GetMethod(
            "InitializeArrays", BindingFlags.Public | BindingFlags.Instance);

        var found = new List<MonoBehaviour>();
        foreach (var b in target.GetComponentsInChildren<MonoBehaviour>(true))
            if (b != null && b.GetType() == puppet2DType)
                found.Add(b);

        _globalControls = found.ToArray();

        Debug.Log($"[AnimationExportPuppet2DBridge] Hooked into " +
            $"{_globalControls.Length} Puppet2D_GlobalControl(s).");

        _initialized = true;
    }
}