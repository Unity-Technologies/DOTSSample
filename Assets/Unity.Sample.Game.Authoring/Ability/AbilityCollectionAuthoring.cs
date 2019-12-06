using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR

public class AbilityCollectionAuthoring
{

    [Serializable]
    public class AbilitySetup
    {
        [Tooltip("Factory that creates ability")]
        public AbilityAuthoring ability;

        public List<UserCommand.Button> ActivateButtons = new List<UserCommand.Button>();

        [EnumBitField(typeof(Ability.AbilityType))]
        public short abilityTypeFlags;

        [EnumBitField(typeof(Ability.AbilityType))]
        public short canRunWithFlags;

        [EnumBitField(typeof(Ability.AbilityType))]
        public short canInterruptFlags;
    }

    public static void AddAbilityComponents(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem, AbilitySetup[] abilities)
    {
        dstManager.AddComponentData(entity, new AbilityCollection.State());

        // Create ability entities
        var abilityEntities = new List<Entity>(abilities.Length);
        for (int i = 0; i < abilities.Length; i++)
        {
            var e = conversionSystem.GetEntities(abilities[i].ability);
            e.MoveNext();
            var abilityEntity = e.Current;

            if (abilityEntities.Contains(abilityEntity))
            {
                GameDebug.LogError("Ability " + abilities[i].ability + " registered multiple times in abilities list");
            }

            abilityEntities.Add(abilityEntity);
        }

        // Add abilities to ability buffer
        dstManager.AddBuffer<AbilityCollection.AbilityEntry>(entity);
        var abilityBuffer = dstManager.GetBuffer<AbilityCollection.AbilityEntry>(entity);

        for (int i = 0; i < abilities.Length; i++)
        {
            var entry = new AbilityCollection.AbilityEntry
            {
                entity = abilityEntities[i],
                abilityType = abilities[i].abilityTypeFlags,
                canRunWith = abilities[i].canRunWithFlags,
                canInterrupt = abilities[i].canInterruptFlags,
            };

            if (abilities[i].ActivateButtons.Count > 4)
            {
                GameDebug.LogError("A maximum of 4 activate buttons are allowed. Currently specified:" + abilities[i].ActivateButtons.Count);
            }

            if(abilities[i].ActivateButtons.Count > 0)
                entry.ActivateButton0 = abilities[i].ActivateButtons[0];
            if(abilities[i].ActivateButtons.Count > 1)
                entry.ActivateButton1 = abilities[i].ActivateButtons[1];
            if(abilities[i].ActivateButtons.Count > 2)
                entry.ActivateButton2 = abilities[i].ActivateButtons[2];
            if(abilities[i].ActivateButtons.Count > 3)
                entry.ActivateButton3 = abilities[i].ActivateButtons[3];

            abilityBuffer.Add(entry);
        }
    }
}

#endif
