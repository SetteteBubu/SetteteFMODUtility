using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SetteteFMODUtilityManager))]
public class SetteteFMODUtilityManagerEditor : Editor
{
    // ── Foldout state ─────────────────────────────────────────────
    private bool _3dTextFoldout;
    private bool _3dAttenuationSettingsFoldout;
    private bool _2dSettingsFoldout;
    private bool _gameViewDisplayFoldout;
    private bool _animSettingsFoldout;

    // ── Serialized properties ──────────────────────────────────────

    // Animation Events function
    private SerializedProperty _animationEventsFunctionName;
    private SerializedProperty _enableAnimationEventsPreview;

    // 3D Events
    private SerializedProperty _visualize3DEvents;
    private SerializedProperty _visualize3DEventsTexts;
    private SerializedProperty _events3DColor;
    private SerializedProperty _events3DFontSize;
    private SerializedProperty _visualize3DEventsAttenuationCurve;
    private SerializedProperty _defaultAttenuationCurveMinColor;
    private SerializedProperty _defaultAttenuationCurveMaxColor;
    private SerializedProperty _events3DPathRootFilter;
    private SerializedProperty _events3DAttenuationCurvePathRootFilter;

    // 2D Events
    private SerializedProperty _enable2DEventsVisualization;
    private SerializedProperty _defaultDebugUICanvasSortingOrder;
    private SerializedProperty _defaultScrollViewImage;
    private SerializedProperty _defaultViewportImage;
    private SerializedProperty _defaultScrollViewWidth;
    private SerializedProperty _defaultScrollViewHeight;
    private SerializedProperty _default2DEventUIElementHeight;
    private SerializedProperty _events2DColor;
    private SerializedProperty _visualizeLoadedBanks;
    private SerializedProperty _banksColor;
    private SerializedProperty _parametersColor;
    private SerializedProperty _defaultDebugUIAnchoring;
    private SerializedProperty _defaultScrollSensitivity;

    // Miscellaneous
    private SerializedProperty _activeInstancesRefreshDelay;

    // Animation
    private SerializedProperty _visualizeSelectedAnimatorCurrentState;
    private SerializedProperty _defaultSelectedAnimatorCurrentStateAnchor;
    private SerializedProperty _defaultSelectedAnimatorCurrentStateWidth;
    private SerializedProperty _defaultSelectedAnimatorCurrentStateHeight;
    private SerializedProperty _defaultSelectedAnimatorCurrentStateColor;
    private SerializedProperty _defaultSelectedAnimatorCurrentStateImage;

    // Parameters
    private SerializedProperty _enableParameterVisualization;
    private SerializedProperty _globalParametersToLog;
    private SerializedProperty _localParameterDatas;
    private SerializedProperty _parameterNamesLUT;

    // ── Styles (lazily initialized) ────────────────────────────────
    private GUIStyle _sectionHeaderStyle;
    private GUIStyle _explanatoryLabelStyle;
    private GUIStyle _sectionBoxStyle;
    private GUIStyle _subHeaderFoldoutStyle;

    // ── Section tint colors (alpha 30/255) ─────────────────────────
    private static readonly Color Color3DEvents = new Color(1f, 0f, 0f, 30f / 255f);
    private static readonly Color Color2DEvents = new Color(0f, 0.4f, 1f, 30f / 255f);
    private static readonly Color ColorRefreshRate = new Color(0f, 1f, 0f, 30f / 255f);
    private static readonly Color ColorAnimationSneakTool = new Color(0.6f, 0f, 1f, 30f / 255f);
    private static readonly Color ColorAnimationEventsPreviewTool = new Color(0f, 1f, 1f, 30f / 255f);
    private static readonly Color ColorParameters = new Color(0f, 0.4f, 1f, 30f / 255f);
    private static readonly Color ColorVideoExporter = new Color(1f, 0.5f, 0f, 30f / 255f);
    private static readonly Color ColorLoadedBanks = new Color(0f, 0.4f, 1f, 30f / 255f);
    private static readonly Color ColorGameViewDisplay = new Color(0f, 0.4f, 1f, 30f / 255f);

    private GUIStyle _sectionBoxStyle3D;
    private GUIStyle _sectionBoxStyle2D;
    private GUIStyle _sectionBoxStyleRefreshRate;
    private GUIStyle _sectionBoxStyleAnimation;
    private GUIStyle _sectionBoxStyleAnimationEventsPreview;
    private GUIStyle _sectionBoxStyleParameters;
    private GUIStyle _sectionBoxStyleVideoExporter;
    private GUIStyle _sectionBoxStyleLoadedBanks;
    private GUIStyle _sectionBoxStyleGameViewDisplay;

    private Texture2D _bannerTexture;

    private static Texture2D MakeSolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private static Texture2D LoadTextureRelativeToScript(string relativePath)
    {
        // Try relative to this script first
        string[] scriptGuids = AssetDatabase.FindAssets("SetteteFMODUtilityManagerEditor t:MonoScript");
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
        _bannerTexture = LoadTextureRelativeToScript("Resources/settete_banner.png");

        // Animation events functions name
        _animationEventsFunctionName = serializedObject.FindProperty("AnimationEventsFunctionName");
        _enableAnimationEventsPreview = serializedObject.FindProperty("enableAnimationEventsPreview");

        // 3D Events
        _visualize3DEvents = serializedObject.FindProperty("visualize3DEvents");
        _visualize3DEventsTexts = serializedObject.FindProperty("visualize3DEventsTexts");
        _events3DColor = serializedObject.FindProperty("events3DColor");
        _events3DFontSize = serializedObject.FindProperty("events3DFontSize");
        _visualize3DEventsAttenuationCurve = serializedObject.FindProperty("visualize3DEventsAttenuationCurve");
        _defaultAttenuationCurveMinColor = serializedObject.FindProperty("defaultAttenuationCurveMinColor");
        _defaultAttenuationCurveMaxColor = serializedObject.FindProperty("defaultAttenuationCurveMaxColor");
        _events3DPathRootFilter = serializedObject.FindProperty("events3DPathRootFilter");
        _events3DAttenuationCurvePathRootFilter = serializedObject.FindProperty("events3DAttenuationCurvePathRootFilter");
        _visualizeLoadedBanks = serializedObject.FindProperty("visualizeLoadedBanks");
        _banksColor = serializedObject.FindProperty("banksColor");
        _parametersColor = serializedObject.FindProperty("parametersColor");

        // 2D Events
        _enable2DEventsVisualization = serializedObject.FindProperty("enable2DEventsVisualization");
        _defaultDebugUICanvasSortingOrder = serializedObject.FindProperty("defaultDebugUICanvasSortingOrder");
        _defaultScrollViewImage = serializedObject.FindProperty("defaultScrollViewImage");
        _defaultViewportImage = serializedObject.FindProperty("defaultViewportImage");
        _defaultScrollViewWidth = serializedObject.FindProperty("defaultScrollViewWidth");
        _defaultScrollViewHeight = serializedObject.FindProperty("defaultScrollViewHeight");
        _default2DEventUIElementHeight = serializedObject.FindProperty("default2DEventUIElementHeight");
        _events2DColor = serializedObject.FindProperty("events2DColor");
        _defaultDebugUIAnchoring = serializedObject.FindProperty("defaultDebugUIAnchoring");
        _defaultScrollSensitivity = serializedObject.FindProperty("defaultScrollSensitivity");

        // Miscellaneous
        _activeInstancesRefreshDelay = serializedObject.FindProperty("activeInstancesRefreshDelay");

        // Animation
        _visualizeSelectedAnimatorCurrentState = serializedObject.FindProperty("visualizeSelectedAnimatorCurrentState");
        _defaultSelectedAnimatorCurrentStateAnchor = serializedObject.FindProperty("defaultSelectedAnimatorCurrentStateAnchor");
        _defaultSelectedAnimatorCurrentStateWidth = serializedObject.FindProperty("defaultSelectedAnimatorCurrentStateWidth");
        _defaultSelectedAnimatorCurrentStateHeight = serializedObject.FindProperty("defaultSelectedAnimatorCurrentStateHeight");
        _defaultSelectedAnimatorCurrentStateColor = serializedObject.FindProperty("defaultSelectedAnimatorCurrentStateColor");
        _defaultSelectedAnimatorCurrentStateImage = serializedObject.FindProperty("defaultSelectedAnimatorCurrentStateImage");

        // Parameters
        _enableParameterVisualization = serializedObject.FindProperty("enableParameterVisualization");
        _globalParametersToLog = serializedObject.FindProperty("globalParametersToLog");
        _localParameterDatas = serializedObject.FindProperty("localParameterDatas");
        _parameterNamesLUT = serializedObject.FindProperty("parameterNamesLUT");

        // Restore foldout states (default: closed)
        _3dTextFoldout = SessionState.GetBool("SFMOD_3dTextFoldout", false);
        _3dAttenuationSettingsFoldout = SessionState.GetBool("SFMOD_3dAttenFoldout", false);
        _2dSettingsFoldout = SessionState.GetBool("SFMOD_2dSettingsFoldout", false);
        _gameViewDisplayFoldout = SessionState.GetBool("SFMOD_gameViewDisplayFoldout", false);
        _animSettingsFoldout = SessionState.GetBool("SFMOD_animSettingsFoldout", false);
    }

    public override void OnInspectorGUI()
    {
        InitStyles();
        serializedObject.Update();

        DrawBrandingBanner();
        EditorGUILayout.Space(6);
        DrawSectionAnimationEventsPreview();
        EditorGUILayout.Space(6);
        DrawSection3DEvents();
        EditorGUILayout.Space(6);
        DrawSectionGameViewDisplaySettings();
        EditorGUILayout.Space(6);
        DrawSection2DEvents();
        EditorGUILayout.Space(6);
        DrawSectionLoadedBanks();
        EditorGUILayout.Space(6);
        DrawSectionParameters();
        EditorGUILayout.Space(6);
        DrawSectionMiscellaneous();
        EditorGUILayout.Space(6);
        DrawSectionAnimation();
        EditorGUILayout.Space(6);
        DrawSectionVideoExporter();

        serializedObject.ApplyModifiedProperties();
    }

    // ── Section: Animation To Video Exporter ────────────────────────

    private void DrawSectionVideoExporter()
    {
        DrawSectionHeader("Animation To Video Exporter");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleVideoExporter))
        {
            EditorGUILayout.LabelField(
                "Lets you batch export animations in .mp4",
                _explanatoryLabelStyle);

            if (GUILayout.Button("Open Animation To Video Exporter"))
                AnimationExporterWindow.Open();
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

                // Available width for the image: total inspector width minus the
                // space we must always reserve for the buttons column.
                float availableForImage = EditorGUIUtility.currentViewWidth
                    - buttonsColumnWidth - spacing - 40f; // 40f ~ helpBox/inspector padding

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

    // ── Section: 3D Events ─────────────────────────────────────────

    private void DrawSection3DEvents()
    {
        DrawSectionHeader("3D Events");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyle3D))
        {
            EditorGUILayout.LabelField(
                "Visualize in Scene View FMOD 3D Events",
                _explanatoryLabelStyle);

            EditorGUILayout.PropertyField(_visualize3DEvents,
                new GUIContent("Enable 3D Events Visualization"));

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!_visualize3DEvents.boolValue))
            {
                // ── Text sub-group ─────────────────────────────────────
                EditorGUILayout.PropertyField(_visualize3DEventsTexts,
                    new GUIContent("Show Text Labels"));

                using (new EditorGUI.DisabledScope(!_visualize3DEventsTexts.boolValue))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _3dTextFoldout = EditorGUILayout.Foldout(
                            _3dTextFoldout, "Text Labels Settings", true, _subHeaderFoldoutStyle);
                        SessionState.SetBool("SFMOD_3dTextFoldout", _3dTextFoldout);
                    }

                    if (_3dTextFoldout)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(_events3DColor,
                                new GUIContent("Text Color"));
                            EditorGUILayout.PropertyField(_events3DFontSize,
                                new GUIContent("Text Size"));
                            EditorGUILayout.PropertyField(_events3DPathRootFilter,
                                new GUIContent("Event Path Filter"));
                        }
                    }
                }

                EditorGUILayout.Space(2);

                // ── Attenuation sub-group ──────────────────────────────
                EditorGUILayout.PropertyField(_visualize3DEventsAttenuationCurve,
                    new GUIContent("Show Attenuation Curve"));

                using (new EditorGUI.DisabledScope(!_visualize3DEventsAttenuationCurve.boolValue))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        _3dAttenuationSettingsFoldout = EditorGUILayout.Foldout(
                            _3dAttenuationSettingsFoldout, "Attenuation Curve Settings", true);
                        SessionState.SetBool("SFMOD_3dAttenFoldout", _3dAttenuationSettingsFoldout);

                        if (_3dAttenuationSettingsFoldout)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                EditorGUILayout.PropertyField(_defaultAttenuationCurveMinColor,
                                    new GUIContent("Min Distance Color"));
                                EditorGUILayout.PropertyField(_defaultAttenuationCurveMaxColor,
                                    new GUIContent("Max Distance Color"));
                                EditorGUILayout.PropertyField(_events3DAttenuationCurvePathRootFilter,
                                    new GUIContent("Attenuation Path Filter"));
                            }
                        }
                    }
                }
            }
        }
    }

    // ── Section: Loaded Banks ───────────────────────────────────────

    private void DrawSectionLoadedBanks()
    {
        DrawSectionHeader("Loaded Banks");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleLoadedBanks))
        {
            EditorGUILayout.LabelField(
                "Visualize in Game View FMOD Banks currently loaded in memory",
                _explanatoryLabelStyle);

            EditorGUILayout.PropertyField(_visualizeLoadedBanks,
                new GUIContent("Enable Loaded Banks Visualization"));

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!_visualizeLoadedBanks.boolValue))
            {
                EditorGUILayout.PropertyField(_banksColor, new GUIContent("Text Color"));
            }
        }
    }

    // ── Section: 2D Events ─────────────────────────────────────────

    private void DrawSection2DEvents()
    {
        DrawSectionHeader("2D Events");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyle2D))
        {
            EditorGUILayout.LabelField(
                "Visualize in Game View FMOD 2D Events Names",
                _explanatoryLabelStyle);

            EditorGUILayout.PropertyField(_enable2DEventsVisualization,
                new GUIContent("Enable 2D Events Visualization"));
        }
    }

    // ── Section: Game View Display Settings ────────────────────────

    private void DrawSectionGameViewDisplaySettings()
    {
        bool anyEnabled = _enable2DEventsVisualization.boolValue ||
                          _visualizeLoadedBanks.boolValue ||
                          _enableParameterVisualization.boolValue;

        DrawSectionHeader("Game View Display Settings");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleGameViewDisplay))
        {
            EditorGUILayout.LabelField(
                "Shared display settings for all Game View visualizations. Enabled when at least one visualization is active.",
                _explanatoryLabelStyle);

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!anyEnabled))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _gameViewDisplayFoldout = EditorGUILayout.Foldout(
                        _gameViewDisplayFoldout, "Display Settings", true, _subHeaderFoldoutStyle);
                    SessionState.SetBool("SFMOD_gameViewDisplayFoldout", _gameViewDisplayFoldout);
                }

                if (_gameViewDisplayFoldout)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_defaultDebugUIAnchoring, new GUIContent("UI Anchoring"));
                        EditorGUILayout.PropertyField(_events2DColor, new GUIContent("Text Color"));
                        EditorGUILayout.PropertyField(_default2DEventUIElementHeight, new GUIContent("Element Height"));
                        EditorGUILayout.PropertyField(_defaultScrollViewWidth, new GUIContent("Scroll View Width"));
                        EditorGUILayout.PropertyField(_defaultScrollViewHeight, new GUIContent("Scroll View Height"));
                        EditorGUILayout.PropertyField(_defaultScrollSensitivity, new GUIContent("Scroll Sensitivity"));
                        EditorGUILayout.PropertyField(_defaultScrollViewImage, new GUIContent("Scroll View Sprite"));
                        EditorGUILayout.PropertyField(_defaultViewportImage, new GUIContent("Viewport Sprite"));
                        EditorGUILayout.PropertyField(_defaultDebugUICanvasSortingOrder, new GUIContent("Canvas Sorting Order"));
                    }
                }
            }
        }
    }

    // ── Section: Miscellaneous ─────────────────────────────────────

    private void DrawSectionMiscellaneous()
    {
        DrawSectionHeader("Refresh Rate");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleRefreshRate))
        {
            EditorGUILayout.PropertyField(_activeInstancesRefreshDelay,
                new GUIContent("Active Instance Refresh Rate"));
        }
    }

    // ── Section: Animation ─────────────────────────────────────────

    private void DrawSectionAnimation()
    {
        DrawSectionHeader("Animation Sneak Tool");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleAnimation))
        {
            EditorGUILayout.LabelField(
                "Lets you visualize  animation datas of an object (game object name, prefab name, state and clip name. (To visualize the data select an object with the animator on it during runtime)",
                _explanatoryLabelStyle);

            EditorGUILayout.PropertyField(_visualizeSelectedAnimatorCurrentState,
                new GUIContent("Visualize Animator State"));

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!_visualizeSelectedAnimatorCurrentState.boolValue))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _animSettingsFoldout = EditorGUILayout.Foldout(_animSettingsFoldout, "Display Settings", true, _subHeaderFoldoutStyle);
                    SessionState.SetBool("SFMOD_animSettingsFoldout", _animSettingsFoldout);
                }

                if (_animSettingsFoldout)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(_defaultSelectedAnimatorCurrentStateAnchor, new GUIContent("Panel Anchor"));
                        EditorGUILayout.PropertyField(_defaultSelectedAnimatorCurrentStateWidth, new GUIContent("Panel Width"));
                        EditorGUILayout.PropertyField(_defaultSelectedAnimatorCurrentStateHeight, new GUIContent("Panel Height"));
                        EditorGUILayout.PropertyField(_defaultSelectedAnimatorCurrentStateColor, new GUIContent("Text Color"));
                        EditorGUILayout.PropertyField(_defaultSelectedAnimatorCurrentStateImage, new GUIContent("Background Sprite"));
                    }
                }
            }
        }
    }

    private void DrawSectionAnimationEventsPreview()
    {
        DrawSectionHeader("Animation Events Preview");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleAnimationEventsPreview))
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_animationEventsFunctionName);
            }
            EditorGUILayout.PropertyField(_enableAnimationEventsPreview);
        }
    }

    // ── Section: Parameters ────────────────────────────────────────

    private void DrawSectionParameters()
    {
        DrawSectionHeader("Parameters");
        using (new EditorGUILayout.VerticalScope(_sectionBoxStyleParameters))
        {
            EditorGUILayout.LabelField(
                "Visualize in Game View FMOD Parameters Values",
                _explanatoryLabelStyle);

            EditorGUILayout.PropertyField(_enableParameterVisualization,
                new GUIContent("Enable Parameter Visualization"));

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!_enableParameterVisualization.boolValue))
            {
                EditorGUILayout.PropertyField(_parametersColor, new GUIContent("Text Color"));

                EditorGUILayout.Space(4);

                // Unity's built-in array/list foldout headers ignore GUI.enabled,
                // so we dim everything manually via GUI.color as well.
                Color previousColor = GUI.color;
                if (!_enableParameterVisualization.boolValue)
                    GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * 0.5f);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_globalParametersToLog,
                        new GUIContent("Global Parameters"), true);
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_localParameterDatas,
                        new GUIContent("Local Parameters"), true);

                    if (_localParameterDatas.isExpanded)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(_parameterNamesLUT,
                                new GUIContent("Parameter Names LUT"), true);
                        }
                    }
                }

                GUI.color = previousColor;
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────

    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(title, _sectionHeaderStyle);
        var rect = GUILayoutUtility.GetLastRect();
        rect.y += rect.height;
        rect.height = 1f;
        EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f, 1f));
        GUILayout.Space(3);
    }

    private void InitStyles()
    {
        if (_sectionHeaderStyle != null) return;

        _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(2, 0, 4, 2)
        };

        _explanatoryLabelStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            fontStyle = FontStyle.Italic,
            padding = new RectOffset(2, 0, 0, 4),
            fontSize = 13
        };

        _sectionBoxStyle = new GUIStyle("helpBox")
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        _sectionBoxStyle3D = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyle3D.normal.background = MakeSolidTexture(Color3DEvents);

        _sectionBoxStyle2D = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyle2D.normal.background = MakeSolidTexture(Color2DEvents);

        _sectionBoxStyleRefreshRate = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleRefreshRate.normal.background = MakeSolidTexture(ColorRefreshRate);

        _sectionBoxStyleAnimation = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleAnimation.normal.background = MakeSolidTexture(ColorAnimationSneakTool);

        _sectionBoxStyleAnimationEventsPreview = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleAnimationEventsPreview.normal.background = MakeSolidTexture(ColorAnimationEventsPreviewTool);

        _sectionBoxStyleParameters = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleParameters.normal.background = MakeSolidTexture(ColorParameters);

        _sectionBoxStyleVideoExporter = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleVideoExporter.normal.background = MakeSolidTexture(ColorVideoExporter);

        _sectionBoxStyleLoadedBanks = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleLoadedBanks.normal.background = MakeSolidTexture(ColorLoadedBanks);

        _sectionBoxStyleGameViewDisplay = new GUIStyle(_sectionBoxStyle);
        _sectionBoxStyleGameViewDisplay.normal.background = MakeSolidTexture(ColorGameViewDisplay);

        _subHeaderFoldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
    }
}