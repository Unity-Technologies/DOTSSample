using Unity.Entities;
using Unity.Sample.Core;



public static class CharacterModule
{
    [ConfigVar(Name = "character.predictioncheck", Description = "Check Prediction", DefaultValue = "0")]
    public static ConfigVar PredictionCheck;

    public static ComponentSystemGroup CreateClientUpdateSystemGroup(World world)
    {
        var group = world.CreateSystem<CharacterUpdateSystemGroup>();
        return group;
    }

    public static ComponentSystemGroup CreateServerUpdateSystemGroup(World world)
    {
        var group = world.CreateSystem<CharacterUpdateSystemGroup>();
        group.AddSystemToUpdateList(world.CreateSystem<HandleCharacterSpawnRequests>());
        group.AddSystemToUpdateList(world.CreateSystem<HandleCharacterDespawnRequests>());
        group.AddSystemToUpdateList(world.CreateSystem<UpdateTeleportation>());
        return group;
    }

    public static ComponentSystemGroup CreateClientPresentationSystemGroup(World world)
    {
        var group = world.CreateSystem<CharacterPresentationSystemGroup>();
        group.AddSystemToUpdateList(world.CreateSystem<PrepareCharacterPresentationState>());
        group.AddSystemToUpdateList(world.CreateSystem<ApplyRootTransform>());
        group.AddSystemToUpdateList(world.CreateSystem<AnimSourceRootSystemGroup>());
        return group;
    }

    public static ComponentSystemGroup CreateServerPresentationSystemGroup(World world)
    {
        var group = world.CreateSystem<CharacterPresentationSystemGroup>();
        group.AddSystemToUpdateList(world.CreateSystem<PrepareCharacterPresentationState>());
        group.AddSystemToUpdateList(world.CreateSystem<ApplyRootTransform>());
        group.AddSystemToUpdateList(world.CreateSystem<AnimSourceRootSystemGroup>());
        return group;
    }

    public static void ServerCleanupPlayer(World world, EntityCommandBuffer ecb, Entity player)
    {
        var playerState = world.EntityManager.GetComponentData<Player.State>(player);
        if (playerState.controlledEntity != Entity.Null)
        {
            CharacterDespawnRequest.Create(ecb, playerState.controlledEntity);
        }
    }

}
