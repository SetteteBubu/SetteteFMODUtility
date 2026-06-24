using UnityEngine;
using System.Collections.Generic;

public class SetteteFMODUtilityDebugManager : MonoBehaviour
{
    List<DrawRequest> m_drawCalls = new List<DrawRequest>();
    List<GizmoGroup> m_gizmoGroups = new List<GizmoGroup>();

    public int ActiveGizmoGroupIndex { get; set; } = 0;
    public int GizmoGroupCount => m_gizmoGroups.Count;

    public GizmoGroup ActiveGizmoGroup => ActiveGizmoGroupIndex >= 0 && ActiveGizmoGroupIndex < m_gizmoGroups.Count ? m_gizmoGroups[ActiveGizmoGroupIndex] : null;

    public GameDebugConfig GetDebugConfig()
    {
#if UNITY_DEBUG
		return m_config?.Data;
#else
        return new GameDebugConfig();
#endif
    }

    static bool CanAddDraw()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPaused;
#else
            return false;
#endif

    }

    public GizmoGroup CreateGizmoGroup(string _name)
    {
        var newGroup = new GizmoGroup { Name = _name };
        m_gizmoGroups.Add(newGroup);
        return newGroup;
    }

    public void DrawBox(Vector3 _center, Vector3 _halfSize, Vector3 _pos, Quaternion _rot, Color _color, float _lifeTime = 0.0f)
    {
        if (!CanAddDraw()) return;
        m_drawCalls.Add(new BoxDrawRequest { Center = _center, Halfsize = _halfSize, Position = _pos, Rotation = _rot, Color = _color, LifeTime = _lifeTime });
    }

    public void DrawSphere(Vector3 _center, float _radius, Color _color, float _lifeTime = 0.0f, object _controlToken = null)
    {
        if (!CanAddDraw()) return;
        m_drawCalls.Add(new SphereDrawRequest { Position = _center, Radius = _radius, Color = _color, LifeTime = _lifeTime, ControlToken = _controlToken });
    }

    public void DrawArrow(float _arrowLength, Vector3 _pos, Quaternion _rot, Color _color, float _lifeTime = 0.0f)
    {
        if (!CanAddDraw()) return;
        m_drawCalls.Add(new ArrowDrawRequest { ArrowLength = _arrowLength, Position = _pos, Rotation = _rot, Color = _color, LifeTime = _lifeTime });
    }

    public void DrawText(string _text, Vector3 _pos, Color _color, int _fontSize = 20, float _lifeTime = 0.0f, TextAnchor _alligment = TextAnchor.UpperLeft, object _controlToken = null)
    {
        if (!CanAddDraw()) return;
        m_drawCalls.Add(new TextDrawRequest { Text = _text, Position = _pos, Color = _color, FontSize = _fontSize, LifeTime = _lifeTime, Allignment = _alligment, ControlToken = _controlToken });
    }

    public void UpdateDrawingPosition(object _controlToken, Vector3 _pos)
    {
        if (!CanAddDraw()) return;
        foreach (var r in m_drawCalls)
        {
            if (r.ControlToken == null) continue;
            if (!r.ControlToken.Equals(_controlToken)) continue;
            if (r is not DrawRequest_WithPosition rp) continue;
            rp.Position = _pos;
        }
    }

    public void UpdateSphereDrawingColor(object _controlToken, Color _color)
    {
        if (!CanAddDraw()) return;
        foreach (var r in m_drawCalls)
        {
            if (r.ControlToken == null) continue;
            if (!r.ControlToken.Equals(_controlToken)) continue;
            if (r is not SphereDrawRequest sdr) continue;
            sdr.Color = _color;
        }
    }

    public void UpdateTextDrawingColor(object _controlToken, Color _col)
    {
        if (!CanAddDraw()) return;
        foreach (var r in m_drawCalls)
        {
            if (r.ControlToken == null) continue;
            if (!r.ControlToken.Equals(_controlToken)) continue;
            if (r is not TextDrawRequest tdr) continue;
            tdr.Color = _col;
        }
    }

    public void UpdateTextDrawingFontSize(object _controlToken, int _fontSize)
    {
        if (!CanAddDraw()) return;
        foreach (var r in m_drawCalls)
        {
            if (r.ControlToken == null) continue;
            if (!r.ControlToken.Equals(_controlToken)) continue;
            if (r is not TextDrawRequest tdr) continue;
            tdr.FontSize = _fontSize;
        }
    }


    public void StopDrawing(object _controlToken)
    {
        if (!CanAddDraw()) return;
        if (_controlToken == null) return;
        m_drawCalls.RemoveAll(r => r.ControlToken.Equals(_controlToken));
    }


    public void DrawFlag(string _name, Vector3 _position, Color _color, float height = 2.0f)
    {
        DrawFlag(_name, _position, _color, Vector3.up, height);
    }

    public void DrawFlag(string _name, Vector3 _position, Color _color, Vector3 _axis, float height = 1.0f)
    {

        Debug.DrawLine(_position, _position + _axis, _color);
        DrawText(_name, _position + _axis, _color);
    }

    protected void Awake()
    {

#if UNITY_EDITOR
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
#else
        Application.SetStackTraceLogType( LogType.Log,StackTraceLogType.None );
        Application.SetStackTraceLogType( LogType.Warning,StackTraceLogType.None );
        Application.SetStackTraceLogType( LogType.Error,StackTraceLogType.Full );
#endif

        //UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }

    bool m_debugPaused = false;
    public float? DebugTimeScale;

    List<object> invalidatedTokens = new();

    private void Update()
    {
        m_drawCalls.RemoveAll(x => x.IsDead);
        foreach (var draw in m_drawCalls)
        {
            draw.Update();
        }
        //Handle invalidated tokens
        if (invalidatedTokens.Count > 0)
        {
            for (int i = 0; i < invalidatedTokens.Count; i++)
            {
                m_drawCalls.RemoveAll(x => x.ControlToken == null || x.ControlToken.Equals(invalidatedTokens[i]));
            }
            invalidatedTokens.Clear();
        }
    }

    public void InvalidateToken(object token)
    {
        invalidatedTokens.Add(token);
    }

    public void OnDrawGizmos()
    {
        foreach (var draw in m_drawCalls)
        {
            draw.Draw();
        }

        var activeGizmoGroup = ActiveGizmoGroup;
        if (activeGizmoGroup != null)
        {
            activeGizmoGroup.Draw();
        }
    }


    class TextDrawRequest : DrawRequest_WithPosition
    {
        public Color Color;
        public string Text;
        public TextAnchor Allignment;
        public int FontSize;

        public override void Draw()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color;
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = Allignment;
            style.fontSize = FontSize;
            style.normal = new GUIStyleState { textColor = Color };
            UnityEditor.Handles.Label(Position, Text, style);
            UnityEditor.Handles.color = Color.white;
#endif
        }
    }


    class BoxDrawRequest : DrawRequest_WithPosition
    {
        public Quaternion Rotation;
        public Color Color;
        public Vector3 Halfsize;
        public Vector3 Center;

        public override void Draw()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Position, Rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = Color;
            Gizmos.DrawWireCube(Center, Halfsize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
    }

    class SphereDrawRequest : DrawRequest_WithPosition
    {
        public Color Color;
        public float Radius;

        public override void Draw()
        {
            Gizmos.color = Color;
            Gizmos.DrawSphere(Position, Radius);
            Gizmos.color = Color.white;
        }
    }

    class ArrowDrawRequest : DrawRequest_WithPosition
    {
        public float ArrowLength;
        public Quaternion Rotation;
        public Color Color;

        public override void Draw()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color;
            UnityEditor.Handles.ArrowHandleCap(0, Position, Rotation, ArrowLength, EventType.Repaint);
            UnityEditor.Handles.color = Color.white;
#endif
        }
    }

    class CustomDrawRequest : DrawRequest
    {
        public System.Action DrawAction;

        public override void Draw()
        {
            DrawAction();
        }
    }

    abstract class DrawRequest_WithPosition : DrawRequest
    {
        public Vector3 Position;
    }


    abstract class DrawRequest
    {
        public float LifeTime = 0.0f;
        public object ControlToken = null;

        public abstract void Draw();
        public void Update()
        {
#if UNITY_EDITOR
            LifeTime -= UnityEditor.EditorApplication.isPaused ? 0.0f : Time.deltaTime;
#else
                LifeTime -= Time.deltaTime;
#endif
            if (IsDead) OnDestroy();
        }

        public bool IsDead
        {
            get
            {
                return LifeTime < 0.0f;
            }
        }

        public virtual void OnDestroy() { }
    }

    public class GizmoGroup
    {
        List<DrawRequest> DrawRequests = new List<DrawRequest>();
        public string Name;

        public void Draw()
        {
            foreach (var d in DrawRequests)
            {
                d.Draw();
            }
        }

        public void AddBox(Vector3 _center, Vector3 _halfSize, Vector3 _pos, Quaternion _rot, Color _color)
        {
            if (!CanAddDraw()) return;
            DrawRequests.Add(new BoxDrawRequest { Center = _center, Halfsize = _halfSize, Position = _pos, Rotation = _rot, Color = _color });
        }

        public void AddArrow(float _arrowLength, Vector3 _pos, Quaternion _rot, Color _color)
        {
            if (!CanAddDraw()) return;
            DrawRequests.Add(new ArrowDrawRequest { ArrowLength = _arrowLength, Position = _pos, Rotation = _rot, Color = _color });
        }

        public void AddSphere(float _radius, Vector3 _pos, Color _color)
        {
            if (!CanAddDraw()) return;
            DrawRequests.Add(new SphereDrawRequest { Radius = _radius, Position = _pos, Color = _color });
        }

        public void AddText(string _text, Vector3 _pos, Color _color)
        {
            if (!CanAddDraw()) return;
            DrawRequests.Add(new TextDrawRequest { Text = _text, Position = _pos, Color = _color });
        }

        public void AddCustomDraw(System.Action _draw)
        {
            if (!CanAddDraw()) return;
            DrawRequests.Add(new CustomDrawRequest { DrawAction = _draw });
        }
    }
}

public partial class GameDebugConfig
{
    [Header("Editor")]
    public bool DisabledAllEditorLocks = false;
}