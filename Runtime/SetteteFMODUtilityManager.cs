using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SetteteFMODUtilityDebugManager))]
public class SetteteFMODUtilityManager : MonoBehaviour
{
    public static readonly string AnimationEventsFunctionNameConstant = "SetteteAudio";

    [Serializable]
    public struct LocalParameterData
    {
        public EventReference fmodEventToPoll;
        public string localParameterName;
        //Used to convert values into names (e.g. 1 = wood)
        public string lutID;
    }
    [Serializable]
    public struct ParameterNamesLUT
    {
        public string lutID;
        public string[] lutEntries;
    }

    [Tooltip("Turn on/off the whole 3D events visualization")]
    public bool visualize3DEvents = false;
    [Tooltip("Turn on/off the whole 3D events text visualization")]
    public bool visualize3DEventsTexts = false;
    [Tooltip("Color used to draw text gizmos related to 3D events")]
    public Color events3DColor = Color.red;
    [Tooltip("Font size used to draw text gizmos related to 3D events")]
    public int events3DFontSize = 20;
    [Tooltip("Visualize 3D events attenuation curve")]
    public bool visualize3DEventsAttenuationCurve = false;
    [Tooltip("Default color used to draw the attenuation curve min sphere for 3D events")]
    public Color defaultAttenuationCurveMinColor = Color.yellow;
    [Tooltip("Default color used to draw the attenuation curve max sphere for 3D events")]
    public Color defaultAttenuationCurveMaxColor = Color.yellow;

    [Tooltip("Only visualize 3D events sharing the set path root. Clear field to remove filter")]
    public string events3DPathRootFilter;
    [Tooltip("Only visualize 3D events attenuation curve sharing the set path root. Clear field to remove filter")]
    public string events3DAttenuationCurvePathRootFilter;

    [Tooltip("Turn on/off the whole loaded FMOD banks visualization")]
    public bool visualizeLoadedBanks = false;
    [Tooltip("Color used to draw text in UI related to loaded FMOD banks")]
    public Color banksColor = Color.cyan;

    [Header("Refresh Rate Settings")]
    [Tooltip("Delay in seconds between each active instances refresh")]
    public float activeInstancesRefreshDelay = 1f;

    [Tooltip("Visualize information regarding current animation and state of Animator in selected GameObject")]
    public bool visualizeSelectedAnimatorCurrentState = false;
    [Tooltip("Anchoring of panel displaying info regarding current animation and state of Animator in selected GameObject")]
    public TextAnchor defaultSelectedAnimatorCurrentStateAnchor = TextAnchor.UpperRight;
    [Tooltip("Width of panel displaying info regarding current animation and state of Animator in selected GameObject")]
    public float defaultSelectedAnimatorCurrentStateWidth = 200f;
    [Tooltip("Height of panel displaying info regarding current animation and state of Animator in selected GameObject")]
    public float defaultSelectedAnimatorCurrentStateHeight = 200f;
    [Tooltip("Color used to draw text in UI related to current animation and state of Animator in selected GameObject")]
    public Color defaultSelectedAnimatorCurrentStateColor = Color.black;
    [Tooltip("Referenced just to grab Unity's built-in sprite, but feel free to customize")]
    public Sprite defaultSelectedAnimatorCurrentStateImage;

    [Header("Preview Animation Events")]
    [ReadOnly]
    //Function to call on SetteteAnimationEventReceiver to trigger audio. Will also be used to preview events in editor
    public readonly string AnimationEventsFunctionName = AnimationEventsFunctionNameConstant;

    [Tooltip("Turn on/off the whole 2D events visualization")]
    public bool enable2DEventsVisualization = false;
    [Tooltip("Should be on top of everything but feel free to change if needed")]
    public int defaultDebugUICanvasSortingOrder = 999;
    [Tooltip("Referenced just to grab Unity's built-in sprite, but feel free to customize")]
    public Sprite defaultScrollViewImage;
    [Tooltip("Referenced just to grab Unity's built-in sprite, but feel free to customize")]
    public Sprite defaultViewportImage;
    [Tooltip("Default width of overall debug UI scroll rect")]
    public float defaultScrollViewWidth = 200f;
    [Tooltip("Default height of overall debug UI scroll rect")]
    public float defaultScrollViewHeight = 200f;
    [Tooltip("2D UI Element default height which effectively dictates font size for the text")]
    public float default2DEventUIElementHeight = 30f;
    [Tooltip("Color used to draw text in UI related to 2D events")]
    public Color events2DColor = Color.orange;
    public TextAnchor defaultDebugUIAnchoring = TextAnchor.LowerLeft;
    [Tooltip("Default debug UI scroll sensitivity")]
    public float defaultScrollSensitivity = 10f;

    [Tooltip("Turn on/off the whole parameters visualization")]
    public bool enableParameterVisualization = false;
    [ParamRef]
    public string[] globalParametersToLog;
    public LocalParameterData[] localParameterDatas;
    public List<ParameterNamesLUT> parameterNamesLUT;

    private List<EventInstance> cachedActiveInstances;
    private float activeInstancesRefreshDelayCounter = 0;
    private SetteteFMODUtilityDebugManager debugManager;
    private Canvas debugUICanvas;
    private GameObject debugUIScrollContent;
    private ScrollRect debugUIScrollRect;
    private Dictionary<EventInstance, GameObject> cached2DEventsUIElements;
    private Dictionary<string, GameObject> cachedGlobalParametersUIElements;
    private Dictionary<LocalParameterData, GameObject> cachedLocalParametersUIElements;
    private List<string> active3DEventsAttenuationCurveTokens = new();
    private List<string> active3DEventsTokens = new();
    private GameObject animatorInfoPanel;
    private TextMeshProUGUI animatorInfoText;
    private GameObject globalParametersCategoryGO;
#if UNITY_EDITOR
    private GameObject localParametersCategoryGO;
#endif
    private GameObject _2DEventsCategoryGO;
    private GameObject banksCategoryGO;
    private Dictionary<string, GameObject> cachedBankUIElements;

    private Dictionary<string, string> cachedLocalParamValues;

    private void Awake()
    {
        debugManager = GetComponent<SetteteFMODUtilityDebugManager>();
        cachedActiveInstances = new();
        cached2DEventsUIElements = new();
        cachedGlobalParametersUIElements = new();
        cachedLocalParametersUIElements = new();
        cachedLocalParamValues = new();
        cachedBankUIElements = new();
    }

    private void Start()
    {
        CreateDebugUI();
    }

    // Update is called once per frame
    void Update()
    {
        //Handle cached active instances refresh
        activeInstancesRefreshDelayCounter += Time.deltaTime;
        if (activeInstancesRefreshDelayCounter >= activeInstancesRefreshDelay)
        {
            RefreshActiveInstances();
            activeInstancesRefreshDelayCounter = 0;
        }

        //Handle cached active instances update
        UpdateCachedActiveInstances();

        //Update global parameters
        UpdateParameters();

        //Update loaded banks visualization
        UpdateLoadedBanksUI();

#if UNITY_EDITOR
        HandleAnimatorInfoPanel();
#endif
    }

    void UpdateLoadedBanksUI()
    {
        if (banksCategoryGO == null) return;

        banksCategoryGO.SetActive(visualizeLoadedBanks);

        if (!visualizeLoadedBanks)
        {
            foreach (var go in cachedBankUIElements.Values)
                go.SetActive(false);
            return;
        }

        // Keep the whole "loaded banks" block queued after every other 2D UI entry
        banksCategoryGO.transform.SetAsLastSibling();

        RuntimeManager.StudioSystem.getBankList(out Bank[] banks);
        var currentBankPaths = new HashSet<string>();

        foreach (var bank in banks)
        {
            bank.getPath(out string bankPath);
            currentBankPaths.Add(bankPath);

            if (!cachedBankUIElements.TryGetValue(bankPath, out GameObject bankGO))
            {
                bankGO = CreateUIElement();
                cachedBankUIElements.Add(bankPath, bankGO);
            }

            TextMeshProUGUI bankText = bankGO.GetComponent<TextMeshProUGUI>();
            bankText.color = banksColor;
            bankText.text = "• " + System.IO.Path.GetFileName(bankPath);
            bankGO.SetActive(true);
            bankGO.transform.SetAsLastSibling();
        }

        // Clean up UI elements for banks that got unloaded
        List<string> toRemove = null;
        foreach (var kvp in cachedBankUIElements)
        {
            if (!currentBankPaths.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                (toRemove ??= new List<string>()).Add(kvp.Key);
            }
        }
        if (toRemove != null)
            foreach (var key in toRemove)
                cachedBankUIElements.Remove(key);
    }

    void UpdateParameters()
    {
        foreach (var globalParam in globalParametersToLog)
        {
            Update2DGlobalParameterUIElement(globalParam);
        }
        globalParametersCategoryGO.SetActive(enableParameterVisualization);
#if UNITY_EDITOR
        foreach (var localParam in localParameterDatas)
        {
            UpdateLocalParameterUIElement(localParam);
        }
        localParametersCategoryGO.SetActive(enableParameterVisualization);
#endif
    }

    private void UpdateCachedActiveInstances()
    {
        for (int i = cachedActiveInstances.Count - 1; i >= 0; i--)
        {
            EventInstance inst = cachedActiveInstances[i];
            inst.getDescription(out EventDescription eventDescription);
            eventDescription.is3D(out bool is3D);
            eventDescription.getPath(out string eventPath);
            if (is3D)
            {
                inst.get3DAttributes(out FMOD.ATTRIBUTES_3D attributes);
                Vector3 instPos = new(
                    attributes.position.x,
                    attributes.position.y,
                    attributes.position.z
                );

                if (!visualize3DEvents)
                {
                    Stop3DEventVisualization(inst);
                    continue;
                }
                else
                {
                    Start3DEventVisualization(inst);
                }

                if (!visualize3DEventsTexts)
                {
                    Stop3DEventVisualization(inst, true, false);
                }

                //Handle attenuation curve filter
                if ((!string.IsNullOrEmpty(events3DAttenuationCurvePathRootFilter) && !eventPath.Contains(events3DAttenuationCurvePathRootFilter)) || !visualize3DEventsAttenuationCurve)
                {
                    Stop3DEventAttenuationCurveVisualization(inst);
                }
                else
                {
                    Start3DEventAttenuationCurveVisualization(inst);
                }

                //Handle global filter
                if (!string.IsNullOrEmpty(events3DPathRootFilter) && !eventPath.Contains(events3DPathRootFilter))
                {
                    debugManager.StopDrawing(inst.handle.ToString());
                    cachedActiveInstances.RemoveAt(i);
                }
                else
                {
                    debugManager.UpdateDrawingPosition(inst.handle.ToString(), instPos);
                    if (active3DEventsAttenuationCurveTokens.Contains(inst.handle.ToString() + "attenMin"))
                    {
                        debugManager.UpdateDrawingPosition(inst.handle.ToString() + "attenMin", instPos);
                        debugManager.UpdateDrawingPosition(inst.handle.ToString() + "attenMax", instPos);
                    }
                }
            }
            else
            {
                if (enable2DEventsVisualization)
                {
                    Create2DEventUIElement(inst);
                }
                else
                {
                    Delete2DEventUIElement(inst);
                }
            }
        }

        _2DEventsCategoryGO.SetActive(enable2DEventsVisualization);
    }

    private void RefreshActiveInstances()
    {
        List<EventInstance> currentActiveInstances = GetAllActiveInstances();

        // Find new instances (in current but not in cache)
        foreach (EventInstance inst in currentActiveInstances)
        {
            inst.getDescription(out EventDescription eventDescription);
            inst.getPlaybackState(out PLAYBACK_STATE state);
            eventDescription.is3D(out bool is3D);

            //Iterate new active instances to visualize them if needed
            if (!cachedActiveInstances.Any(c => c.handle == inst.handle) && state != PLAYBACK_STATE.STOPPED)
            {
                if (is3D)
                {
                    if (visualize3DEvents)
                    {
                        //Add to 3D events drawn as gizmos
                        Start3DEventVisualization(inst);
                    }
                    else
                    {
                        //Stop visualizing if entire feature is disabled
                        Stop3DEventVisualization(inst);
                    }
                }
                else
                {
                    if (enable2DEventsVisualization)
                    {
                        //Add to 2D events drawn as UI
                        Create2DEventUIElement(inst);
                    }
                    else
                    {
                        Delete2DEventUIElement(inst);
                    }
                }
            }

            //Record active but stopped instances to delete after foreach
            if (state == PLAYBACK_STATE.STOPPED)
            {
                if (is3D)
                {
                    Stop3DEventVisualization(inst);
                }
                else
                {
                    Delete2DEventUIElement(inst);
                }
            }
        }

        // Find non active instances (in cache but not in current)
        foreach (EventInstance inst in cachedActiveInstances)
        {
            inst.getDescription(out EventDescription eventDescription);
            eventDescription.is3D(out bool is3D);

            if (!currentActiveInstances.Any(c => c.handle == inst.handle))
            {
                if (is3D)
                {
                    Stop3DEventVisualization(inst);
                }
                else
                {
                    Delete2DEventUIElement(inst);
                }

                debugManager.InvalidateToken(inst.handle.ToString());
            }
        }

        //Remove stopped instances
        currentActiveInstances.RemoveAll((x) =>
        {
            x.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.STOPPED;
        });

        cachedActiveInstances = currentActiveInstances;
    }

    void Start3DEventAttenuationCurveVisualization(EventInstance eventInstance)
    {
        if (active3DEventsAttenuationCurveTokens.Contains(eventInstance.handle.ToString() + "attenMin")) return;
        if (!visualize3DEvents) return;

        eventInstance.getDescription(out EventDescription eventDescription);
        eventInstance.get3DAttributes(out FMOD.ATTRIBUTES_3D attributes);
        Vector3 instPos = new(
            attributes.position.x,
            attributes.position.y,
            attributes.position.z
        );
        eventDescription.getMinMaxDistance(out float min, out float max);
        debugManager.DrawSphere(instPos, min, defaultAttenuationCurveMinColor, float.MaxValue, eventInstance.handle.ToString() + "attenMin");
        debugManager.DrawSphere(instPos, max, defaultAttenuationCurveMaxColor, float.MaxValue, eventInstance.handle.ToString() + "attenMax");
        active3DEventsAttenuationCurveTokens.Add(eventInstance.handle.ToString() + "attenMin");
        active3DEventsAttenuationCurveTokens.Add(eventInstance.handle.ToString() + "attenMax");
    }

    void Stop3DEventAttenuationCurveVisualization(EventInstance eventInstance)
    {
        if (active3DEventsAttenuationCurveTokens.Contains(eventInstance.handle.ToString() + "attenMin"))
        {
            debugManager.StopDrawing(eventInstance.handle.ToString() + "attenMin");
            debugManager.StopDrawing(eventInstance.handle.ToString() + "attenMax");
            active3DEventsAttenuationCurveTokens.Remove(eventInstance.handle.ToString() + "attenMin");
            active3DEventsAttenuationCurveTokens.Remove(eventInstance.handle.ToString() + "attenMax");
        }
    }

    void Start3DEventVisualization(EventInstance eventInstance)
    {
        if (!visualize3DEvents) return;

        eventInstance.getDescription(out EventDescription eventDescription);

        eventDescription.getPath(out string path);

        //Handle filter
        if (!string.IsNullOrEmpty(events3DPathRootFilter) && !path.Contains(events3DPathRootFilter)) return;

        eventInstance.get3DAttributes(out FMOD.ATTRIBUTES_3D attributes);
        Vector3 instPos = new(
            attributes.position.x,
            attributes.position.y,
            attributes.position.z
        );

        if (visualize3DEventsTexts)
            debugManager.DrawText("• " + System.IO.Path.GetFileName(path), instPos, events3DColor, events3DFontSize, float.MaxValue, TextAnchor.MiddleLeft, eventInstance.handle.ToString());

        //Handle attenuation curve visualization
        if (visualize3DEventsAttenuationCurve && (string.IsNullOrEmpty(events3DAttenuationCurvePathRootFilter) || path.Contains(events3DAttenuationCurvePathRootFilter)))
        {
            Start3DEventAttenuationCurveVisualization(eventInstance);
        }

        if (!active3DEventsTokens.Contains(eventInstance.handle.ToString()))
        {
            active3DEventsTokens.Add(eventInstance.handle.ToString());
        }
    }

    void Stop3DEventVisualization(EventInstance eventInstance, bool disableText = true, bool disableAttenCurve = true)
    {
        if (disableText)
        {
            debugManager.StopDrawing(eventInstance.handle.ToString());
            active3DEventsTokens.Remove(eventInstance.handle.ToString());
        }
        if (disableAttenCurve)
        {
            Stop3DEventAttenuationCurveVisualization(eventInstance);
        }
    }

    void Create2DEventUIElement(EventInstance eventInstance)
    {
        if (!cached2DEventsUIElements.ContainsKey(eventInstance))
        {
            GameObject event2DElement = CreateUIElement();

            eventInstance.getDescription(out EventDescription eventDescription);
            eventDescription.getPath(out string eventPath);

            TextMeshProUGUI event2DElementText = event2DElement.GetComponent<TextMeshProUGUI>();
            event2DElementText.text = System.IO.Path.GetFileName(eventPath);

            cached2DEventsUIElements.Add(eventInstance, event2DElement);
        }
    }

    void Create2DGlobalParameterUIElement(string globalParameter)
    {
        GameObject event2DElement = CreateUIElement();
        cachedGlobalParametersUIElements.Add(globalParameter, event2DElement);
        Update2DGlobalParameterUIElement(globalParameter);
    }

#if UNITY_EDITOR
    void CreateLocalParameterUIElement(LocalParameterData localParameterData)
    {
        GameObject event2DElement = CreateUIElement();
        cachedLocalParametersUIElements.Add(localParameterData, event2DElement);
        UpdateLocalParameterUIElement(localParameterData);
    }
#endif

    //Would global parameters UI elements be ever deleted?
    /*void Delete2DGlobalParameterUIElement(string globalParameter)
    {
        if (cachedGlobalParametersUIElements.ContainsKey(globalParameter))
        {
            Destroy(cachedGlobalParametersUIElements[globalParameter]);
            cachedGlobalParametersUIElements.Remove(globalParameter);
        }
    }*/

    void Update2DGlobalParameterUIElement(string globalParameter)
    {
        if (cachedGlobalParametersUIElements.ContainsKey(globalParameter))
        {
            TextMeshProUGUI event2DElementText = cachedGlobalParametersUIElements[globalParameter].GetComponent<TextMeshProUGUI>();
            if (!enableParameterVisualization)
            {
                event2DElementText.enabled = false;
                return;
            }
            event2DElementText.enabled = true;
            RuntimeManager.StudioSystem.getParameterByName(globalParameter, out float paramVal);
            string globalParamString = globalParameter + ":" + paramVal;
            event2DElementText.text = globalParamString;
        }
    }

#if UNITY_EDITOR
    void UpdateLocalParameterUIElement(LocalParameterData localParameterData)
    {
        if (cachedLocalParametersUIElements.ContainsKey(localParameterData))
        {
            TextMeshProUGUI event2DElementText = cachedLocalParametersUIElements[localParameterData].GetComponent<TextMeshProUGUI>();

            if (!enableParameterVisualization)
            {
                event2DElementText.enabled = false;
                return;
            }

            event2DElementText.enabled = true;
            // Try to find the first active instance for this event
            for (int j = 0; j < cachedActiveInstances.Count; j++)
            {
                cachedActiveInstances[j].getDescription(out EventDescription desc);
                desc.getPath(out string path);
                if (path == localParameterData.fmodEventToPoll.Path)
                {
                    cachedActiveInstances[j].getParameterByName(localParameterData.localParameterName, out float paramValue);
                    string paramValueName = paramValue.ToString();

                    foreach (var lut in parameterNamesLUT)
                    {
                        if (lut.lutID == localParameterData.lutID)
                        {
                            for (int z = 0; z < lut.lutEntries.Length; z++)
                            {
                                if (z == (int)paramValue)
                                {
                                    paramValueName = lut.lutEntries[z];
                                    break;
                                }
                            }
                        }
                    }

                    if (cachedLocalParamValues.ContainsKey(localParameterData.localParameterName))
                        cachedLocalParamValues[localParameterData.localParameterName] = paramValueName;
                    else
                        cachedLocalParamValues.Add(localParameterData.localParameterName, paramValueName);

                    break; // First match only
                }
            }

            cachedLocalParamValues.TryGetValue(localParameterData.localParameterName, out string cachedValue);
            string localParamString = localParameterData.localParameterName + ":" + (cachedValue ?? "?");
            event2DElementText.text = localParamString;
        }
    }
#endif

    GameObject CreateCategoryUIElement(string categoryTitle)
    {
        GameObject element = CreateUIElement();
        TextMeshProUGUI elementText = element.GetComponent<TextMeshProUGUI>();
        elementText.text = categoryTitle;
        elementText.fontStyle = FontStyles.UpperCase;
        return element;
    }

    GameObject CreateUIElement()
    {
        GameObject event2DElement = new GameObject("FMODEventDebugItem", typeof(RectTransform));
        event2DElement.transform.SetParent(debugUIScrollContent.transform, false);
        event2DElement.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, default2DEventUIElementHeight);

        TextMeshProUGUI event2DElementText = event2DElement.AddComponent<TextMeshProUGUI>();
        event2DElementText.enableAutoSizing = true;
        event2DElementText.fontSizeMax = 72f;
        event2DElementText.fontSizeMin = 5f;
        event2DElementText.textWrappingMode = TextWrappingModes.NoWrap;
        event2DElementText.color = events2DColor;

        return event2DElement;
    }

    void Delete2DEventUIElement(EventInstance eventInstance)
    {
        if (cached2DEventsUIElements.ContainsKey(eventInstance))
        {
            Destroy(cached2DEventsUIElements[eventInstance]);
            cached2DEventsUIElements.Remove(eventInstance);
        }
    }

    private List<EventInstance> GetAllActiveInstances()
    {
        var seen = new HashSet<IntPtr>();
        var allInstances = new List<EventInstance>();

        RuntimeManager.StudioSystem.getBankList(out Bank[] banks);

        for (int i = 0; i < banks.Length; i++)
        {
            Bank bank = banks[i];
            bank.getEventList(out EventDescription[] descriptions);

            for (int j = 0; j < descriptions.Length; j++)
            {
                EventDescription desc = descriptions[j];

                desc.getInstanceList(out EventInstance[] instances);

                for (int k = 0; k < instances.Length; k++)
                {
                    EventInstance inst = instances[k];

                    // EventInstance is a struct wrapping a native pointer
                    if (seen.Add(inst.handle))
                        allInstances.Add(inst);
                }
            }
        }

        return allInstances;
    }

    private void CreateDebugUI()
    {
        // --- Canvas ---
        GameObject canvasGO = new GameObject("SetteteFMODUtilityUI");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = defaultDebugUICanvasSortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Debug UI Scroll View ---
        GameObject scrollViewGO = new GameObject("DebugUIScrollView");
        scrollViewGO.layer = LayerMask.NameToLayer("UI");
        scrollViewGO.transform.SetParent(canvasGO.transform, false);

        Image scrollImage = scrollViewGO.AddComponent<Image>();
        scrollImage.color = new Color(1, 1, 1, 0.3921569f);
        scrollImage.sprite = defaultScrollViewImage;
        scrollImage.type = Image.Type.Sliced;

        ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();

        RectTransform scrollRT = scrollViewGO.GetComponent<RectTransform>();
        ApplyTextAnchorToRectTransform(scrollRT, defaultDebugUIAnchoring);
        scrollRT.anchoredPosition = Vector2.zero;
        scrollRT.sizeDelta = new Vector2(defaultScrollViewWidth, defaultScrollViewHeight);

        // --- Debug UI Viewport ---
        GameObject viewportGO = new GameObject("DebugUIViewport", typeof(RectTransform));
        viewportGO.layer = LayerMask.NameToLayer("UI");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);

        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = new Vector2(0, 0);
        viewportRT.pivot = new Vector2(0f, 1f);
        viewportRT.sizeDelta = new Vector2(0f, 0f);

        Image viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 1);
        viewportImage.sprite = defaultViewportImage;
        viewportImage.type = Image.Type.Sliced;

        Mask mask = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = viewportRT;

        // --- Debug UI Content ---
        GameObject contentGO = new GameObject("DebugUIContent", typeof(RectTransform));
        contentGO.layer = LayerMask.NameToLayer("UI");
        contentGO.transform.SetParent(viewportGO.transform, false);

        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 9999f); //so that elements keep piling up forever MMO-style chat
        VerticalLayoutGroup contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVLG.childAlignment = TextAnchor.LowerLeft;
        contentVLG.childControlHeight = false;
        contentVLG.childControlWidth = true;
        contentVLG.childForceExpandHeight = false;

        scrollRect.content = contentRT;

        // --- Scrollbars: disable and nullify ---
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;

        // disable default scrollbar gameobjects if they were created
        foreach (Transform child in scrollViewGO.transform)
        {
            string n = child.name;
            if (n == "Scrollbar Horizontal" || n == "Scrollbar Vertical")
                child.gameObject.SetActive(false);
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        //Scroll is scrolled upwards to the max by default
        scrollRect.normalizedPosition = Vector2.zero;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
        scrollRect.scrollSensitivity = defaultScrollSensitivity;

        // --- Cache content reference ---
        debugUIScrollContent = contentGO;
        debugUIScrollRect = scrollRect;
        debugUICanvas = canvas;

        //Animator info panel
        animatorInfoPanel = new GameObject("AnimatorInfoPanel", typeof(RectTransform));
        animatorInfoPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform animatorInfoPanelRT = animatorInfoPanel.GetComponent<RectTransform>();
        ApplyTextAnchorToRectTransform(animatorInfoPanelRT, defaultSelectedAnimatorCurrentStateAnchor);
        animatorInfoPanelRT.sizeDelta = new(defaultSelectedAnimatorCurrentStateWidth, defaultSelectedAnimatorCurrentStateHeight);

        Image animatorInfoPanelBG = animatorInfoPanel.AddComponent<Image>();
        animatorInfoPanelBG.sprite = defaultSelectedAnimatorCurrentStateImage;
        animatorInfoPanelBG.type = Image.Type.Sliced;

        GameObject animatorInfoPanelText = new GameObject("AnimatorInfoPanelText", typeof(RectTransform));
        animatorInfoPanelText.transform.SetParent(animatorInfoPanel.transform, false);
        animatorInfoText = animatorInfoPanelText.AddComponent<TextMeshProUGUI>();
        animatorInfoText.text = "";
        animatorInfoText.enableAutoSizing = true;
        animatorInfoText.fontSizeMax = 72f;
        animatorInfoText.fontSizeMin = 5f;
        animatorInfoText.textWrappingMode = TextWrappingModes.NoWrap;
        animatorInfoText.color = defaultSelectedAnimatorCurrentStateColor;
        animatorInfoText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        animatorInfoText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        animatorInfoText.GetComponent<RectTransform>().offsetMin = new Vector2(10f, 10f);
        animatorInfoText.GetComponent<RectTransform>().offsetMax = new Vector2(-10f, -10f);
        animatorInfoText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        animatorInfoText.verticalAlignment = VerticalAlignmentOptions.Middle;

        animatorInfoPanel.SetActive(false);

        globalParametersCategoryGO = CreateCategoryUIElement("global parameters");
        //Global Parameters
        foreach (var globalParameters in globalParametersToLog)
        {
            Create2DGlobalParameterUIElement(globalParameters);
        }
#if UNITY_EDITOR
        localParametersCategoryGO = CreateCategoryUIElement("local parameters");
        //Local Parameters
        foreach (var localParameterData in localParameterDatas)
        {
            CreateLocalParameterUIElement(localParameterData);
        }
#endif
        _2DEventsCategoryGO = CreateCategoryUIElement("2d events");
        banksCategoryGO = CreateCategoryUIElement("loaded banks");
    }

    void ApplyTextAnchorToRectTransform(RectTransform rt, TextAnchor anchor)
    {
        Vector2 anchorValue = anchor switch
        {
            TextAnchor.UpperLeft => new Vector2(0f, 1f),
            TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
            TextAnchor.UpperRight => new Vector2(1f, 1f),
            TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
            TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
            TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
            TextAnchor.LowerLeft => new Vector2(0f, 0f),
            TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
            TextAnchor.LowerRight => new Vector2(1f, 0f),
            _ => new Vector2(0.5f, 0.5f)
        };

        rt.anchorMin = anchorValue;
        rt.anchorMax = anchorValue;
        rt.pivot = anchorValue;
    }

#if UNITY_EDITOR
    void HandleAnimatorInfoPanel()
    {
        GameObject currentSelectedGO = Selection.activeGameObject;
        bool shouldDisplayAnimatorInfo = false;
        if (currentSelectedGO != null && visualizeSelectedAnimatorCurrentState)
        {
            if (currentSelectedGO.TryGetComponent(out Animator animator))
            {
                var sb = new System.Text.StringBuilder();

                // Get the prefab asset the object originates from
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentSelectedGO);
                string prefabName = prefabAsset != null ? prefabAsset.name : "None";

                sb.AppendLine($"GameObject Name: [{currentSelectedGO.name}],\nPrefab Name: [{prefabName}]");

                for (int i = 0; i < animator.layerCount; i++)
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                    AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(i);

                    string layerName = animator.GetLayerName(i);
                    string stateName = "Unknown State";
                    string clipName = clipInfos.Length > 0 ? clipInfos[0].clip.name : "No Clip";

                    var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller != null && i < controller.layers.Length)
                    {
                        foreach (var state in controller.layers[i].stateMachine.states)
                        {
                            if (stateInfo.IsName(state.state.name))
                            {
                                stateName = state.state.name;
                                break;
                            }
                        }
                    }

                    sb.AppendLine($"[Layer {i}: {layerName}] State: {stateName}\nClip: {clipName}");
                }

                shouldDisplayAnimatorInfo = true;
                animatorInfoText.text = sb.ToString();
            }
            else
            {
                shouldDisplayAnimatorInfo = false;
            }
        }
        else
        {
            shouldDisplayAnimatorInfo = false;
        }

        animatorInfoPanel.SetActive(shouldDisplayAnimatorInfo);
    }
    [ContextMenu("Print Active Instances")]
    void PrintActiveInstances()
    {
        string activeInstancesMsg = "";
        foreach (EventInstance eventInstance in cachedActiveInstances)
        {
            if (eventInstance.isValid())
            {
                eventInstance.getDescription(out EventDescription description);
                description.getPath(out string eventPath);
                activeInstancesMsg += eventPath + "\n";
            }
        }
        Debug.Log("{SetteteFMODUtility}[PrintActiveInstances] Actives instances: \n" + activeInstancesMsg);
    }

    private void OnValidate()
    {
        //Synch debug UI
        //Synchg debug UI canvas
        if (debugUICanvas != null)
        {
            debugUICanvas.sortingOrder = defaultDebugUICanvasSortingOrder;
        }
        //Synch debug UI scroll rect
        if (debugUIScrollRect != null)
        {
            RectTransform debugUIScrollRectTransform = debugUIScrollRect.GetComponent<RectTransform>();
            debugUIScrollRectTransform.sizeDelta = new Vector2(defaultScrollViewWidth, defaultScrollViewHeight);
            ApplyTextAnchorToRectTransform(debugUIScrollRectTransform, defaultDebugUIAnchoring);

            debugUIScrollRect.GetComponent<Image>().sprite = defaultScrollViewImage;
            if (debugUIScrollRect.transform.childCount > 0)
            {
                debugUIScrollRect.transform.GetChild(0).GetComponent<Image>().sprite = defaultViewportImage;
            }
            debugUIScrollRect.scrollSensitivity = defaultScrollSensitivity;
            debugUIScrollRect.normalizedPosition = Vector2.zero;
        }
        //Synch debug UI scroll content
        if (debugUIScrollContent != null)
        {
            for (int i = 0; i < debugUIScrollContent.transform.childCount; i++)
            {
                Transform debugUIElement = debugUIScrollContent.transform.GetChild(i);
                debugUIElement.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, default2DEventUIElementHeight);
                debugUIElement.GetComponent<TextMeshProUGUI>().color = events2DColor;
            }
        }

        //Synch 3D events variables
        foreach (string event3DToken in active3DEventsTokens)
        {
            //3D events color
            debugManager.UpdateTextDrawingColor(event3DToken, events3DColor);
            //3D events font size
            debugManager.UpdateTextDrawingFontSize(event3DToken, events3DFontSize);
        }

        //Synch 3D events attenuation curves variables
        foreach (string event3DAttenuationCurveToken in active3DEventsAttenuationCurveTokens)
        {
            //3D events min attenuation curve color
            bool isMin = event3DAttenuationCurveToken.EndsWith("Min");
            debugManager.UpdateSphereDrawingColor(event3DAttenuationCurveToken, isMin ? defaultAttenuationCurveMinColor : defaultAttenuationCurveMaxColor);
        }

        //Synch loaded banks color
        if (cachedBankUIElements != null)
        {
            foreach (var go in cachedBankUIElements.Values)
            {
                go.GetComponent<TextMeshProUGUI>().color = banksColor;
            }
        }

        //Synch animator info panel
        if (animatorInfoPanel != null)
        {
            RectTransform animatorInfoRT = animatorInfoPanel.GetComponent<RectTransform>();
            ApplyTextAnchorToRectTransform(animatorInfoRT, defaultSelectedAnimatorCurrentStateAnchor);
            animatorInfoRT.sizeDelta = new(defaultSelectedAnimatorCurrentStateWidth, defaultSelectedAnimatorCurrentStateHeight);
            animatorInfoText.color = defaultSelectedAnimatorCurrentStateColor;
            animatorInfoPanel.GetComponent<Image>().sprite = defaultSelectedAnimatorCurrentStateImage;
        }
    }
#endif
}
