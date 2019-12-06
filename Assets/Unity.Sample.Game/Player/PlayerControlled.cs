using Unity.Entities;
using Unity.NetCode;


public class PlayerControlled
{
    public struct State : IComponentData
    {
        public UserCommand command;
        public UserCommand prevCommand;

        [GhostDefaultField]
        public int resetCommandTick;
        [GhostDefaultField(10)]
        public float resetCommandLookYaw;
        [GhostDefaultField(10)]
        public float resetCommandLookPitch; // = 90;
        public int lastResetCommandTick;

        public bool IsButtonPressed(UserCommand.Button button)
        {
            return command.buttons.IsSet(button) && !prevCommand.buttons.IsSet(button);
        }

        public void ResetCommand(int tick, float lookYaw, float lookPitch)
        {
            resetCommandTick = tick;
            resetCommandLookYaw = lookYaw;
            resetCommandLookPitch = lookPitch;
        }
    }

}
