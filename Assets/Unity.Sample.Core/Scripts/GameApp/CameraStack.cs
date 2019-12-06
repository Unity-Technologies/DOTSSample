using System.Collections.Generic;
using Unity.Sample.Core;
using UnityEngine;

public class CameraStack
{
    public delegate void CameraEnabledChanged(Camera camera, bool enabled);

    public CameraEnabledChanged OnCameraEnabledChanged;

    List<Camera> m_CameraStack = new List<Camera>();

    public Camera TopCamera()
    {
        var c = m_CameraStack.Count;
        return c == 0 ? null : m_CameraStack[c - 1];
    }

    public void PushCamera(Camera cam)
    {
        if (m_CameraStack.Count > 0)
            SetCameraEnabled(m_CameraStack[m_CameraStack.Count - 1], false);
        m_CameraStack.Add(cam);
        SetCameraEnabled(cam, true);
    }

    public void PopCamera(Camera cam)
    {
        GameDebug.Assert(m_CameraStack.Count > 1, "Trying to pop last camera off stack!");
        GameDebug.Assert(cam == m_CameraStack[m_CameraStack.Count - 1]);
        if (cam != null)
            SetCameraEnabled(cam, false);
        m_CameraStack.RemoveAt(m_CameraStack.Count - 1);
        SetCameraEnabled(m_CameraStack[m_CameraStack.Count - 1], true);
    }

    public void Update()
    {
        // Make sure all cameras are in stack
        foreach (var camera in Camera.allCameras)
        {
            if (m_CameraStack.Contains(camera))
                continue;

            SetCameraEnabled(camera, false);
            m_CameraStack.Insert(0,camera);
        }

        // Verify if camera was somehow destroyed and pop it
        while (m_CameraStack.Count > 1 && m_CameraStack[m_CameraStack.Count - 1] == null)
        {
            PopCamera(null);
        }
    }

    void SetCameraEnabled(Camera cam, bool enabled)
    {
        cam.enabled = enabled;

        if (OnCameraEnabledChanged != null)
            OnCameraEnabledChanged(cam, enabled);
    }


}
