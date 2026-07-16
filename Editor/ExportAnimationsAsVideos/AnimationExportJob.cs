using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class AnimationExportJob
{
    public static void Run(
    GameObject target,
    AnimationClip clip,
    string frameOutputFolder,
    int fps,
    Vector2Int resolution,
    Vector3 cameraOffset,
    Vector3 lookAtOffset)
    {
        var rt = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        rt.Create();

        var camGo = new GameObject("__ExportCam__");
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.fieldOfView = 50f;

        int totalFrames = Mathf.CeilToInt(clip.length * fps);
        float frameDuration = 1f / fps;

        // Cache frame hooks once — avoids GetComponentsInChildren every frame
        var frameHooks = target.GetComponentsInChildren<IAnimationExportFrameHook>(true);

        AnimationMode.StartAnimationMode();

        var animator = target.GetComponent<Animator>();
        var graph = PlayableGraph.Create("Export");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        var playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        playableOutput.SetSourcePlayable(clipPlayable);
        graph.Play();

        try
        {
            for (int i = 0; i < totalFrames; i++)
            {
                float time = i * frameDuration;

                // 1. Sample the raw animation curves onto transforms
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target, clip, time);
                AnimationMode.EndSampling();

                // 2. Advance the playable graph
                clipPlayable.SetTime(time);
                graph.Evaluate();

                // 3. Let secondary systems (Puppet2D IK, custom rigs, etc.)
                //    propagate the sampled pose to skinned mesh bones
                foreach (var hook in frameHooks)
                    hook.OnFrameSampled(target, time);

                // 4. Force skinned mesh deformation to evaluate
                foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.forceMatrixRecalculationPerRender = true;
                    smr.updateWhenOffscreen = true;
                }

                target.transform.hasChanged = true;
                EditorApplication.QueuePlayerLoopUpdate();

                // 5. Frame camera and render
                Vector3 pivot = target.transform.position + lookAtOffset;
                camGo.transform.position = target.transform.position + cameraOffset;
                camGo.transform.LookAt(pivot);

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                cam.Render();

                var tex = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                tex.Apply();

                File.WriteAllBytes(
                    Path.Combine(frameOutputFolder, $"frame_{i:D5}.png"),
                    tex.EncodeToPNG());

                Object.DestroyImmediate(tex);
                RenderTexture.active = prev;

                float progress = (float)i / totalFrames;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Exporting " + clip.name, $"Frame {i}/{totalFrames}", progress))
                    break;
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            EditorUtility.ClearProgressBar();
            graph.Destroy();
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }

    public static Texture2D TakeScreenshot(
    GameObject target,
    Vector2Int resolution,
    Vector3 cameraOffset,
    Vector3 lookAtOffset)
    {
        var rt = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        rt.Create();

        var camGo = new GameObject("__PreviewCam__");
        var cam = camGo.AddComponent<Camera>();
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.fieldOfView = 50f;

        Vector3 pivot = target.transform.position + lookAtOffset;
        camGo.transform.position = target.transform.position + cameraOffset;
        camGo.transform.LookAt(pivot);

        var animator = target.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        Texture2D result = null;

        try
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            cam.Render();

            result = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            result.Apply();

            RenderTexture.active = prev;
        }
        finally
        {
            if (animator != null) animator.enabled = true;
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        return result;
    }
}