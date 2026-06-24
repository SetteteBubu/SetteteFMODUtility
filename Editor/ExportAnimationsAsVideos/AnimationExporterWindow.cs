using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AnimationExporterWindow : EditorWindow
{
    private GameObject _rigWithMesh;      // the mesh+rig FBX instance
    [SerializeField] private List<GameObject> _animationSources = new(); //prefabs where animation clips are collected
    private string _outputFolder = "Assets/AnimExports";
    private string _singleClipName = "";
    private int _fps = 30;
    private Vector2Int _resolution = new Vector2Int(1280, 720);

    // Camera framing
    private Vector3 _cameraOffset = new Vector3(0.5f, 1.0f, -2.5f);
    private Vector3 _lookAtOffset = new Vector3(0f, 0.9f, 0f); // approx hip height

    private List<AnimationClip> _clips = new List<AnimationClip>();
    private Vector2 _scrollPos;
    private Texture2D _preview;

    private SerializedObject _serializedWindow;
    private SerializedProperty _animSourcesProp;

    private void OnDisable()
    {
        // Clean up the texture when the window closes
        if (_preview != null)
        {
            Object.DestroyImmediate(_preview);
            _preview = null;
        }
    }

    private void OnEnable()
    {
        _serializedWindow = new SerializedObject(this);
        _animSourcesProp = _serializedWindow.FindProperty("_animationSources");
    }

    [MenuItem("Tools/Animation Video Exporter")]
    public static void Open() => GetWindow<AnimationExporterWindow>("Anim Exporter");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Video Exporter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _rigWithMesh = (GameObject)EditorGUILayout.ObjectField(
            "Mesh + Rig", _rigWithMesh, typeof(GameObject), true);
        _serializedWindow.Update();
        EditorGUILayout.PropertyField(_animSourcesProp, new GUIContent("Animation FBXes"), true);
        _serializedWindow.ApplyModifiedProperties();

        if (GUILayout.Button("Refresh Clip List") && _animationSources != null)
            RefreshClips();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(150));

        foreach (var clip in _clips)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("• " + clip.name);

            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = clip.name;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        _fps = EditorGUILayout.IntField("FPS", _fps);
        _resolution = EditorGUILayout.Vector2IntField("Resolution", _resolution);
        _cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", _cameraOffset);
        _lookAtOffset = EditorGUILayout.Vector3Field("LookAt Offset", _lookAtOffset);
        if (GUILayout.Button("Update Preview") && _rigWithMesh != null)
        {
            UpdatePreview();
        }

        if (_preview != null)
        {
            // Scale the preview to fit the window width while keeping aspect ratio
            float aspect = (float)_preview.width / _preview.height;
            float drawWidth = EditorGUIUtility.currentViewWidth - 20f;
            float drawHeight = drawWidth / aspect;

            var rect = GUILayoutUtility.GetRect(drawWidth, drawHeight);
            GUI.DrawTexture(rect, _preview, ScaleMode.ScaleToFit);
        }

        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

        EditorGUILayout.Space();
        GUI.enabled = _rigWithMesh != null && _clips.Count > 0;
        if (GUILayout.Button("Export All Clips"))
            ExportClips(_clips);
        _singleClipName = EditorGUILayout.TextField("Single Clip Export", _singleClipName);
        if (GUILayout.Button("Export Single Clip"))
            ExportClips(new() { _clips.Find(x => x.name == _singleClipName) });
        GUI.enabled = true;
    }

    void UpdatePreview()
    {
        // Instantiate the mesh rig into a temp scene-like environment
        var instance = PrefabUtility.IsPartOfPrefabAsset(_rigWithMesh)
            ? (GameObject)PrefabUtility.InstantiatePrefab(_rigWithMesh)
            : _rigWithMesh; // already a scene instance

        bool weCreatedIt = instance != _rigWithMesh;

        if (_preview != null) Object.DestroyImmediate(_preview);
        _preview = AnimationExportJob.TakeScreenshot(
            _rigWithMesh, _resolution, _cameraOffset, _lookAtOffset);

        if (weCreatedIt) DestroyImmediate(instance);
    }

    private void RefreshClips()
    {
        _clips.Clear();
        foreach (var source in _animationSources)
        {
            if (source == null) continue;
            string path = AssetDatabase.GetAssetPath(source);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip c && !c.name.StartsWith("__preview__"))
                    _clips.Add(c);
            }
        }
    }

    private void ExportClips(List<AnimationClip> clips)
    {
        if (!Directory.Exists(_outputFolder))
            Directory.CreateDirectory(_outputFolder);

        // Instantiate the mesh rig into a temp scene-like environment
        var instance = PrefabUtility.IsPartOfPrefabAsset(_rigWithMesh)
            ? (GameObject)PrefabUtility.InstantiatePrefab(_rigWithMesh)
            : _rigWithMesh; // already a scene instance

        bool weCreatedIt = instance != _rigWithMesh;

        try
        {
            foreach (var clip in clips)
            {
                if (clip != null)
                {
                    string sanitizedClipName = clip.name.Replace("|", "_");
                    string clipFolder = Path.Combine(_outputFolder, sanitizedClipName);
                    Directory.CreateDirectory(clipFolder);
                    AnimationExportJob.Run(instance, clip, clipFolder, _fps,
                        _resolution, _cameraOffset, _lookAtOffset);
                    FfmpegUtils.StitchToMp4(clipFolder,
                        Path.Combine(_outputFolder, sanitizedClipName + ".mp4"), _fps);
                }
            }
        }
        finally
        {
            if (weCreatedIt) DestroyImmediate(instance);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done",
            $"Exported {clips.Count} clips to {_outputFolder}", "OK");
    }
}