using Unity.Entities;

public class DecisionTreeNode
{
    public struct State : IComponentData
    {
        public static State Default => new State { isTrue = true };
        public Entity parent;
        public Entity owner;
        public bool isTrue;
    }

    public struct SubtreeElement : IBufferElementData
    {
        public Entity root;
        public int count;
    }
}
