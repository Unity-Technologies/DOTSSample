using Unity.Entities;
using Unity.Jobs;

public class PlayerCharacterControl
{
    public struct State : IComponentData
    {
        public int characterType;
        public int requestedCharacterType;
    }

    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class PlayerCharacterControlSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var characterStateFromEntity = GetComponentDataFromEntity<Character.State>(false);
            var characterSettingsFromEntity = GetComponentDataFromEntity<Character.Settings>(false);
            var hitColliderOwnerStateFromEntity = GetComponentDataFromEntity<HitColliderOwner.State>(false);

            Entities
                .WithAll<State>()
                .ForEach((ref Player.State playerState) =>
            {
                var controlledEntity = playerState.controlledEntity;

                if (controlledEntity == Entity.Null || !characterStateFromEntity.HasComponent(controlledEntity))
                    return;

                var charState = characterStateFromEntity[controlledEntity];
                var charSettings = characterSettingsFromEntity[controlledEntity];

                // Update character team
                charState.teamId = playerState.teamIndex;

                // Update hit collision
                if (hitColliderOwnerStateFromEntity.HasComponent(controlledEntity))
                {
                    var hitCollisionOwner = hitColliderOwnerStateFromEntity[controlledEntity];
                    hitCollisionOwner.colliderFlags = 1U << charState.teamId;
                    hitColliderOwnerStateFromEntity[controlledEntity] = hitCollisionOwner;
                }

                charSettings.characterName = playerState.playerName;
                characterSettingsFromEntity[controlledEntity] = charSettings;
                characterStateFromEntity[controlledEntity] = charState;
            }).Run();

            return default;
        }
    }
}

