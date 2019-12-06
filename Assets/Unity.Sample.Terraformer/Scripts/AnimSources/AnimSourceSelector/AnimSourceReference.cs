using Unity.Entities;


public class AnimSourceReference
{
    public struct State : IComponentData
    {
        public static State Default => new State();

        public WeakAssetReference animSource;
    }
}
