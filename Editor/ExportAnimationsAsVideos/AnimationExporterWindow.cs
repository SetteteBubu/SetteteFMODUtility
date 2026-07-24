using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimationExporterWindow : EditorWindow
{
    private GameObject _rigWithMesh;
    [SerializeField] private List<GameObject> _animationSources = new();
    private string _outputFolder = "Assets/AnimExports";
    private int _fps = 30;
    private Vector2Int _resolution = new Vector2Int(1280, 720);

    private Vector3 _cameraOffset = new Vector3(0.5f, 1.0f, -2.5f);
    private Vector3 _lookAtOffset = new Vector3(0f, 0.9f, 0f);

    private List<AnimationClip> _clips = new();
    private List<bool> _clipToggles = new();
    private Vector2 _scrollPos;
    private Texture2D _preview;

    public enum AnimationsSourceType { FBX, AnimatorAndClips }

    private AnimationsSourceType _sourceType = AnimationsSourceType.FBX;
    private Animator _sourceAnimator;
    [SerializeField] private AnimationClip[] _sourceAnimationClips = Array.Empty<AnimationClip>();

    private SerializedObject _serializedWindow;
    private SerializedProperty _animSourcesProp;
    private SerializedProperty _sourceAnimationClipsProp;
    private Texture2D _bannerTexture;

    // Track previous camera values to detect changes
    private Vector3 _prevCameraOffset;
    private Vector3 _prevLookAtOffset;
    private Vector2Int _prevResolution;




    [MenuItem("Tools/Settete Animation To Video Export")]
    public static AnimationExporterWindow Open()
    {
        var window = GetWindow<AnimationExporterWindow>("Settete Animation To Video Export");
        return window;
    }

    private static Texture2D LoadTextureRelativeToEditor(string relativePath)
    {
        // Try relative to this script first
        string[] scriptGuids = AssetDatabase.FindAssets("AnimationExporterWindow t:MonoScript");
        if (scriptGuids.Length > 0)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
            string folder = System.IO.Path.GetDirectoryName(scriptPath);
            string texturePath = System.IO.Path.Combine(folder, relativePath).Replace('\\', '/');
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null) return tex;
        }

        // Fallback: search for the texture by name anywhere in the project
        string fileName = System.IO.Path.GetFileNameWithoutExtension(relativePath);
        string[] texGuids = AssetDatabase.FindAssets(fileName + " t:Texture2D");
        foreach (var guid in texGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("settete_banner"))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        return null;
    }

    private void OnEnable()
    {
        _serializedWindow = new SerializedObject(this);
        _animSourcesProp = _serializedWindow.FindProperty("_animationSources");
        _sourceAnimationClipsProp = _serializedWindow.FindProperty("_sourceAnimationClips");

        _prevCameraOffset = _cameraOffset;
        _prevLookAtOffset = _lookAtOffset;
        _prevResolution = _resolution;

        _bannerTexture = LoadTextureRelativeToEditor("Resources/settete_banner.png");

        _outputFolder = EditorPrefs.GetString("SATVE_OutputFolder", _outputFolder);
    }

    private void OnDisable()
    {
        if (_preview != null)
        {
            UnityEngine.Object.DestroyImmediate(_preview);
            _preview = null;
        }
    }

    // ── Branding banner ──────────────────────────────────────────────

    private void DrawBrandingBanner()
    {
        const float bannerHeight = 80f;
        const float buttonsColumnWidth = 110f; // reserved space, buttons must never disappear
        const float spacing = 10f;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(bannerHeight)))
        {
            if (_bannerTexture != null)
            {
                float aspect = (float)_bannerTexture.width / _bannerTexture.height;

                float availableForImage = EditorGUIUtility.currentViewWidth
                    - buttonsColumnWidth - spacing - 40f;

                float imageHeight = bannerHeight;
                float imageWidth = imageHeight * aspect;

                if (imageWidth > availableForImage)
                {
                    imageWidth = Mathf.Max(20f, availableForImage);
                    imageHeight = imageWidth / aspect;
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Height(bannerHeight)))
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(_bannerTexture, GUILayout.Width(imageWidth), GUILayout.Height(imageHeight));
                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.VerticalScope(GUILayout.Height(bannerHeight), GUILayout.Width(buttonsColumnWidth)))
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Website"))
                    Application.OpenURL("https://settete.com/");

                GUILayout.Space(4);

                if (GUILayout.Button("Contact Us"))
                    Application.OpenURL("https://settete.com/contact");

                GUILayout.FlexibleSpace();
            }
        }
    }

    private void OnGUI()
    {
        DrawBrandingBanner();
        EditorGUILayout.Space(4);

        // ── Source type ───────────────────────────────────────────────
        _sourceType = (AnimationsSourceType)EditorGUILayout.EnumPopup("Source Type", _sourceType);
        EditorGUILayout.Space(2);

        if (_sourceType == AnimationsSourceType.FBX)
        {
            var prevRig = _rigWithMesh;
            _rigWithMesh = (GameObject)EditorGUILayout.ObjectField(
                "Mesh + Rig", _rigWithMesh, typeof(GameObject), true);
            if (_rigWithMesh != prevRig) TryRefreshPreview();

            _serializedWindow.Update();
            EditorGUILayout.PropertyField(_animSourcesProp, new GUIContent("Animation FBXes"), true);
            _serializedWindow.ApplyModifiedProperties();

            if (GUILayout.Button("Refresh Clip List") && _animationSources != null)
                RefreshClips();
        }
        else
        {
            var prevAnimator = _sourceAnimator;
            _sourceAnimator = (Animator)EditorGUILayout.ObjectField(
                "Source Animator", _sourceAnimator, typeof(Animator), true);
            if (_sourceAnimator != prevAnimator) TryRefreshPreview();

            _serializedWindow.Update();
            EditorGUILayout.PropertyField(
                _sourceAnimationClipsProp, new GUIContent("Animation Clips"), true);
            _serializedWindow.ApplyModifiedProperties();

            if (GUILayout.Button("Refresh Clip List"))
                RefreshClipsFromAnimatorAndClips();
        }

        if (_clips.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select All", GUILayout.Width(90)))
                for (int i = 0; i < _clipToggles.Count; i++) _clipToggles[i] = true;
            GUILayout.Space(8);
            if (GUILayout.Button("Select None", GUILayout.Width(90)))
                for (int i = 0; i < _clipToggles.Count; i++) _clipToggles[i] = false;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        if (_clips.Count > 0)
        {
            float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float maxScrollHeight = itemHeight * 8f;
            float scrollHeight = Mathf.Min(_clips.Count * itemHeight, maxScrollHeight);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(scrollHeight));

            for (int i = 0; i < _clips.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _clipToggles[i] = EditorGUILayout.Toggle(_clipToggles[i], GUILayout.Width(20));
                using (new EditorGUI.DisabledScope(!_clipToggles[i]))
                {
                    EditorGUILayout.LabelField("• " + _clips[i].name);
                    //if (GUILayout.Button("Copy", GUILayout.Width(50)))
                    //EditorGUIUtility.systemCopyBuffer = _clips[i].name;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Everything below — drawn for real now ──────────────────
        DrawBelowScrollView();
    }

    private void DrawBelowScrollView()
    {
        EditorGUILayout.Space(4);
        _fps = EditorGUILayout.IntField("FPS", _fps);
        _resolution = EditorGUILayout.Vector2IntField("Resolution", _resolution);
        _cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", _cameraOffset);
        _lookAtOffset = EditorGUILayout.Vector3Field("LookAt Offset", _lookAtOffset);

        if (_cameraOffset != _prevCameraOffset ||
            _lookAtOffset != _prevLookAtOffset ||
            _resolution != _prevResolution)
        {
            _prevCameraOffset = _cameraOffset;
            _prevLookAtOffset = _lookAtOffset;
            _prevResolution = _resolution;
            TryRefreshPreview();
        }

        if (_preview != null)
        {
            float aspect = (float)_preview.width / _preview.height;
            float drawWidth = EditorGUIUtility.currentViewWidth - 20f;
            float drawHeight = drawWidth / aspect;
            var rect = GUILayoutUtility.GetRect(drawWidth, drawHeight);
            GUI.DrawTexture(rect, _preview, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            var newFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (newFolder != _outputFolder)
            {
                _outputFolder = newFolder;
                EditorPrefs.SetString("SATVE_OutputFolder", _outputFolder);
            }
            if (GUILayout.Button("Browse…", GUILayout.Width(70)))
            {
                string chosen = EditorUtility.OpenFolderPanel(
                    "Select Export Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(chosen))
                {
                    _outputFolder = chosen;
                    EditorPrefs.SetString("SATVE_OutputFolder", _outputFolder);
                }
            }
        }

        EditorGUILayout.Space(4);

        int enabledCount = 0;
        foreach (var t in _clipToggles) if (t) enabledCount++;

        using (new EditorGUI.DisabledScope((_rigWithMesh == null && _sourceAnimator == null) || enabledCount == 0))
        {
            if (GUILayout.Button($"Export {enabledCount} Selected Clip(s)"))
                ExportSelectedClips();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void TryRefreshPreview()
    {
        GameObject target = ResolvePreviewTarget();
        if (target == null) return;

        bool weCreatedIt = target != _rigWithMesh &&
                           target != (_sourceAnimator != null ? _sourceAnimator.gameObject : null);

        if (_preview != null) UnityEngine.Object.DestroyImmediate(_preview);
        _preview = AnimationExportJob.TakeScreenshot(
            target, _resolution, _cameraOffset, _lookAtOffset);

        if (weCreatedIt) DestroyImmediate(target);
        Repaint();
    }

    private GameObject ResolvePreviewTarget()
    {
        if (_sourceType == AnimationsSourceType.FBX)
        {
            if (_rigWithMesh == null) return null;
            if (PrefabUtility.IsPartOfPrefabAsset(_rigWithMesh))
                return (GameObject)PrefabUtility.InstantiatePrefab(_rigWithMesh);
            return _rigWithMesh;
        }
        else
        {
            if (_sourceAnimator == null) return null;
            if (PrefabUtility.IsPartOfPrefabAsset(_sourceAnimator.gameObject))
                return (GameObject)PrefabUtility.InstantiatePrefab(_sourceAnimator.gameObject);
            return _sourceAnimator.gameObject;
        }
    }

    private void RefreshClips()
    {
        _clips.Clear();
        _clipToggles.Clear();

        foreach (var source in _animationSources)
        {
            if (source == null) continue;
            string path = AssetDatabase.GetAssetPath(source);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    _clips.Add(c);
                    _clipToggles.Add(true); // enabled by default
                }
            }
        }
    }

    private void RefreshClipsFromAnimatorAndClips()
    {
        _clips.Clear();
        _clipToggles.Clear();

        foreach (var clip in _sourceAnimationClips)
        {
            if (clip == null) continue;
            _clips.Add(clip);
            _clipToggles.Add(true);
        }
    }

    private void ExportSelectedClips()
    {
        var toExport = new List<AnimationClip>();
        for (int i = 0; i < _clips.Count; i++)
            if (_clipToggles[i]) toExport.Add(_clips[i]);

        if (toExport.Count == 0) return;

        if (!Directory.Exists(_outputFolder))
            Directory.CreateDirectory(_outputFolder);

        GameObject instance = ResolvePreviewTarget();
        bool weCreatedIt = (_sourceType == AnimationsSourceType.FBX &&
                                 PrefabUtility.IsPartOfPrefabAsset(_rigWithMesh)) ||
                                 (_sourceType == AnimationsSourceType.AnimatorAndClips &&
                                 PrefabUtility.IsPartOfPrefabAsset(_sourceAnimator.gameObject));

        if (instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No valid source target assigned.", "OK");
            return;
        }

        var autoBridge = AnimationExportJob.TryAutoAddPuppet2DBridge(instance);

        string lastMp4 = null;
        try
        {
            foreach (var clip in toExport)
            {
                string sanitized = clip.name.Replace("|", "_");
                string clipFolder = Path.Combine(_outputFolder, sanitized);
                Directory.CreateDirectory(clipFolder);
                AnimationExportJob.Run(instance, clip, clipFolder, _fps,
                    _resolution, _cameraOffset, _lookAtOffset);
                string mp4Path = Path.Combine(_outputFolder, sanitized + ".mp4");
                FfmpegUtils.StitchToMp4(clipFolder, mp4Path, _fps);
                lastMp4 = mp4Path;
            }
        }
        finally
        {
            if (weCreatedIt) DestroyImmediate(instance);
            else if (autoBridge != null)
                DestroyImmediate(autoBridge); // clean up only if we added it
        }

        AssetDatabase.Refresh();
        bool openFolder = EditorUtility.DisplayDialog("Done",
            $"Exported {toExport.Count} clip(s) to {_outputFolder}",
            "Open File Location", "Close");

        if (openFolder && lastMp4 != null)
            EditorUtility.RevealInFinder(lastMp4);
    }
}