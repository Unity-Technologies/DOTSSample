using Unity.Entities;

public static class AbilityCollection
{

    public struct State : IComponentData
    {
        public Entity abilityOwner;
    }

    public struct AbilityEntry : IBufferElementData
    {
        public Entity entity;

        // TODO (mogensh) find better way to define this list of buttons
        public UserCommand.Button ActivateButton0;
        public UserCommand.Button ActivateButton1;
        public UserCommand.Button ActivateButton2;
        public UserCommand.Button ActivateButton3;

        public short abilityType;
        public short canRunWith;
        public short canInterrupt;
    }
}
