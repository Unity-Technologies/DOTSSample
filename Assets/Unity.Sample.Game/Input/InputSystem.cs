using Unity.Sample.Core;
using UnityEngine;

public class InputSystem
{
    [ConfigVar(Name = "config.inverty", DefaultValue = "0", Description = "Invert y mouse axis", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar configInvertY;
    
    [ConfigVar(Name = "config.mousesensitivity", DefaultValue = "1.5", Description = "Mouse sensitivity", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar configMouseSensitivity;
    
    // TODO: these should be put in some global setting
    public static Vector2 s_JoystickLookSensitivity = new Vector2(90.0f, 60.0f);

    static float maxMoveYaw;
    static float maxMoveMagnitude;

    static int s_bMouseLockFrameNo;
    
    public static void RequestMousePointerLock()
    {
        s_bMouseLockFrameNo = Time.frameCount + 1;
    }

    public static void SetMousePointerLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        s_bMouseLockFrameNo = Time.frameCount; // prevent default handling in WindowFocusUpdate overriding requests
    }

    public static bool GetMousePointerLock()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    public static void WindowFocusUpdate(bool menusShowing)
    {
        bool lockWhenClicked = !menusShowing && !Console.IsOpen();

        if (s_bMouseLockFrameNo == Time.frameCount)
        {
            SetMousePointerLock(true);
            return;
        }

        if (lockWhenClicked)
        {
            // Default behaviour when no menus or anything. Catch mouse on click, release on escape.
            if (UnityEngine.Input.GetMouseButtonUp(0) && !GetMousePointerLock())
                SetMousePointerLock(true);

            if (UnityEngine.Input.GetKeyUp(KeyCode.Escape) && GetMousePointerLock())
                SetMousePointerLock(false);
        }
        else
        {
            // When menu or console open, release lock
            if (GetMousePointerLock())
            {
                SetMousePointerLock(false);
            }
        }
    }
    
    public static void AccumulateInput(ref UserCommand command, float deltaTime)
    {
        // To accumulate move we store the input with max magnitude and uses that
        Vector2 moveInput = new Vector2(GatedInput.GetAxisRaw("Horizontal"), GatedInput.GetAxisRaw("Vertical"));
        float angle = Vector2.Angle(Vector2.up, moveInput);
        if (moveInput.x < 0)
            angle = 360 - angle;
        float magnitude = Mathf.Clamp(moveInput.magnitude, 0, 1);
        if (magnitude > maxMoveMagnitude)
        {
            maxMoveYaw = angle;
            maxMoveMagnitude = magnitude;
        }
        command.moveYaw = maxMoveYaw;
        command.moveMagnitude = maxMoveMagnitude;

        float invertY = configInvertY.IntValue > 0 ? -1.0f : 1.0f;

        Vector2 deltaMousePos = new Vector2(0, 0);
        if (deltaTime > 0.0f)
            deltaMousePos += new Vector2(GatedInput.GetAxisRaw("Mouse X"), GatedInput.GetAxisRaw("Mouse Y") * invertY);
        deltaMousePos += deltaTime * (new Vector2(GatedInput.GetAxisRaw("RightStickX") * s_JoystickLookSensitivity.x, -invertY * GatedInput.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));
        deltaMousePos += deltaTime * (new Vector2(
            ((GatedInput.GetKey(KeyCode.Keypad4) ? -1.0f : 0.0f) + (GatedInput.GetKey(KeyCode.Keypad6) ? 1.0f : 0.0f)) * s_JoystickLookSensitivity.x,
            -invertY * GatedInput.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));

        command.lookYaw += deltaMousePos.x * configMouseSensitivity.FloatValue;
        command.lookYaw = command.lookYaw % 360;
        while (command.lookYaw < 0.0f) command.lookYaw += 360.0f;

        command.lookPitch += deltaMousePos.y * configMouseSensitivity.FloatValue;
        command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);

        command.buttons.Or(UserCommand.Button.Jump, GatedInput.GetKeyDown(KeyCode.Space) || GatedInput.GetKeyDown(KeyCode.Joystick1Button0));
        command.buttons.Or(UserCommand.Button.Boost, GatedInput.GetKey(KeyCode.B) || GatedInput.GetKey(KeyCode.Joystick1Button4));
        command.buttons.Or(UserCommand.Button.PrimaryFire, (GatedInput.GetMouseButton(0) && GetMousePointerLock()) || GatedInput.GetAxisRaw("Right Trigger") > 0.5f);
        command.buttons.Or(UserCommand.Button.SecondaryFire, GatedInput.GetMouseButton(1) || GatedInput.GetKey(KeyCode.Joystick1Button5));
        command.buttons.Or(UserCommand.Button.Ability1, GatedInput.GetKey(KeyCode.LeftShift) || GatedInput.GetKey(KeyCode.Joystick1Button8));
        command.buttons.Or(UserCommand.Button.Ability2, GatedInput.GetKey(KeyCode.F) || GatedInput.GetAxisRaw("Left Trigger") > 0.5f);
        command.buttons.Or(UserCommand.Button.Ability3, GatedInput.GetKey(KeyCode.Q));
        command.buttons.Or(UserCommand.Button.Reload, GatedInput.GetKey(KeyCode.R) || GatedInput.GetKey(KeyCode.Joystick1Button2));
        command.buttons.Or(UserCommand.Button.Melee, GatedInput.GetKey(KeyCode.V) || GatedInput.GetKey(KeyCode.Joystick1Button1));
        command.buttons.Or(UserCommand.Button.Use, GatedInput.GetKey(KeyCode.E));
        command.buttons.Or(UserCommand.Button.Crouch, GatedInput.GetKey(KeyCode.LeftControl) || GatedInput.GetKey(KeyCode.Joystick1Button9));

        // TODO: if needed bring back after unite demo
        //command.buttons.Or(UserCommand.Button.CameraSideSwitch, GatedInput.GetKey(KeyCode.C));

        command.buttons.Or(UserCommand.Button.Item1,
            GatedInput.GetKeyDown(KeyCode.Alpha1) || Input.GetAxisRaw("DPadY") == 1);
        command.buttons.Or(UserCommand.Button.Item2,
            GatedInput.GetKeyDown(KeyCode.Alpha2) || Input.GetAxisRaw("DPadX") == 1);
        command.buttons.Or(UserCommand.Button.Item3,
            GatedInput.GetKeyDown(KeyCode.Alpha3) || Input.GetAxisRaw("DPadY") == -1);
        command.buttons.Or(UserCommand.Button.Item4,
            GatedInput.GetKeyDown(KeyCode.Alpha4) || Input.GetAxisRaw("DPadX") == -1);



    }

    public static void ClearInput(ref UserCommand command)
    {
        maxMoveMagnitude = 0;
        command.ClearCommand();
    }
}
