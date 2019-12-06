using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GatedInput
{
    [Flags]
    public enum Blocker
    {
        None = 0,
        Console = 1,
        Chat = 2,
        Debug = 4,
    }
    static Blocker blocks;

    public static void SetBlock(Blocker b, bool value)
    {
        if (value)
            blocks |= b;
        else
            blocks &= ~b;
    }

    public static float GetAxisRaw(string axis)
    {
        return blocks != Blocker.None ? 0.0f : UnityEngine.Input.GetAxisRaw(axis);
    }

    public static bool GetKey(KeyCode key)
    {
        return blocks != Blocker.None ? false : UnityEngine.Input.GetKey(key);
    }

    public static bool GetKeyDown(KeyCode key)
    {
        return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyDown(key);
    }

    public static bool GetMouseButton(int button)
    {
        return blocks != Blocker.None ? false : UnityEngine.Input.GetMouseButton(button);
    }

    public static bool GetKeyUp(KeyCode key)
    {
        return blocks != Blocker.None ? false : UnityEngine.Input.GetKeyUp(key);
    }
}