using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class SelectionToolbar : EditorWindow
{
    public SelectionToolbar()
    {
        m_HistoryStackEnd = -1;
        m_HistoryStackIdx = -1;
    }

    [MenuItem("A2/Windows/Selection Toolbar")]
    public static void ShowWindow()
    {
        var window = GetWindow<SelectionToolbar>(false, "Selection Toolbar", true);
        window.minSize = new Vector2(100, 10);
        m_HistoryStackEnd = -1;
        m_HistoryStackIdx = -1;
    }

    public static WeakReference[] m_HistoryStack = new WeakReference[128];
    public static int m_HistoryStackIdx = -1;
    public static int m_HistoryStackEnd = -1;

    [MenuItem("A2/Hotkeys/Selection Next &2")]
    public static void _Next()
    {
        if (m_HistoryStackIdx < m_HistoryStackEnd)
        {
            m_HistoryStackIdx++;
            var o = m_HistoryStack[m_HistoryStackIdx % m_HistoryStack.Length];
            if (o.IsAlive)
                Selection.activeObject = o.Target as UnityEngine.Object;
        }

        if (m_PopupWindow != null && m_PopupWindow.editorWindow != null)
            m_PopupWindow.editorWindow.Repaint();
    }

    [MenuItem("A2/Hotkeys/Selection Prev &1")]
    public static void _Prev()
    {
        if (m_HistoryStackIdx > 0)
        {
            m_HistoryStackIdx--;
            var o = m_HistoryStack[m_HistoryStackIdx % m_HistoryStack.Length];
            if (o.IsAlive)
                Selection.activeObject = o.Target as UnityEngine.Object;
        }

        if (m_PopupWindow != null && m_PopupWindow.editorWindow != null)
            m_PopupWindow.editorWindow.Repaint();
    }

    private void OnSelectionChange()
    {
        if (!Selection.activeObject)
            return;
        var obj = Selection.activeObject;

        var lastHistory = m_HistoryStackEnd > -1 ? m_HistoryStack[m_HistoryStackIdx % m_HistoryStack.Length] : null;
        if (lastHistory == null || !lastHistory.IsAlive || (lastHistory.Target as UnityEngine.Object) != obj)
        {
            m_HistoryStackIdx = ++m_HistoryStackEnd;
            m_HistoryStack[(m_HistoryStackIdx) % m_HistoryStack.Length] = new WeakReference(obj);
        }
    }


    Rect histRect;
    static HistoryWindow m_PopupWindow;

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("<<", GUILayout.Width(50.0f)))
            _Prev();
        if (GUILayout.Button("...", GUILayout.Width(50.0f)))
        {
            m_PopupWindow = new HistoryWindow(this);
            PopupWindow.Show(histRect, m_PopupWindow);
        }
        if (Event.current.type == EventType.Repaint) histRect = GUILayoutUtility.GetLastRect();
        if (GUILayout.Button(">>", GUILayout.Width(50.0f)))
            _Next();

        // Ping + drag done as label as drag seems to not work with buttons?
        GUILayout.Label("\u2299", GUILayout.Width(20.0f));
        var rect = GUILayoutUtility.GetLastRect();
        var cev = Event.current;
        var mousePos = cev.mousePosition;
        var o = Selection.activeObject;
        if (rect.Contains(mousePos) && o != null)
        {
            var mouseStartDrag = false;
            var mouseClick = false;
            mouseStartDrag = (cev.type == EventType.MouseDrag) && cev.button == 0;
            mouseClick = (cev.type == EventType.MouseUp) && cev.button == 0 && cev.clickCount == 1;
            if (mouseStartDrag)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.StartDrag(o.name);
                DragAndDrop.objectReferences = new UnityEngine.Object[] { o };
                Event.current.Use();
            }
            else if (mouseClick)
            {
                EditorGUIUtility.PingObject(o);
                Event.current.Use();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

}

public class HistoryWindow : PopupWindowContent
{
    private SelectionToolbar m_Window;

    GUIStyle historyItemStyle;
    GUIStyle historyItemSelectedStyle;
    private Vector2 scrollPosition;

    public HistoryWindow(SelectionToolbar m_Window)
    {
        this.m_Window = m_Window;

        historyItemStyle = new GUIStyle();
        historyItemStyle.contentOffset = new Vector2(4, 0);
        historyItemStyle.fixedHeight = 20.0f;
        historyItemStyle.stretchWidth = true;
    }


    public override void OnGUI(Rect rect)
    {
        if (editorWindow != null)
        {
            editorWindow.minSize = new Vector2(300.0f, 450.0f);
        }
        var l = SelectionToolbar.m_HistoryStack.Length;

        // Prune invalid refs by iterating from edn
        for (int src = SelectionToolbar.m_HistoryStackEnd, dst = src, count = 0; count < l; count++)
        {
            if (src < 0)
                break;
            var s = SelectionToolbar.m_HistoryStack[src % l];
            if (s.IsAlive && (s.Target as UnityEngine.Object) != null)
            {
                SelectionToolbar.m_HistoryStack[dst % l] = s;
                dst--;
            }
            src--;
        }

        var to = SelectionToolbar.m_HistoryStackEnd - 20;
        var from = SelectionToolbar.m_HistoryStackEnd;
        to = to < 0 ? 0 : to;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        for (var i = from; i >= to; --i)
        {
            var o = SelectionToolbar.m_HistoryStack[i % l];
            if (o.IsAlive)
            {
                var uo = o.Target as UnityEngine.Object;
                if (uo != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(i == SelectionToolbar.m_HistoryStackIdx ? "+" : "", GUILayout.Width(10.0f));
                    SelectionHistoryWindow.DrawObjectReference(uo, uo.name, historyItemStyle);
                    GUILayout.EndHorizontal();
                }
            }
        }
        GUILayout.EndScrollView();
    }
}
