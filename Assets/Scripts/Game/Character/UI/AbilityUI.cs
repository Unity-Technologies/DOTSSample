using Unity.Entities;


//public partial class AbilityUI : MonoBehaviour, IConvertGameObjectToEntity
//{
//    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
//    {
//        dstManager.AddComponentData(entity,new State());
//    }
//}


public partial class AbilityUI
{
    public struct State : IComponentData
    {
        public Entity presentationOwner;
    }
}
