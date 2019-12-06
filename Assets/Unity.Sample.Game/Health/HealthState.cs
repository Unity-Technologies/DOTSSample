using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
public struct HealthStateData : IComponentData
{
    [GhostDefaultField(1)]
    [NonSerialized] public float health;
    [NonSerialized] public float maxHealth;
    [NonSerialized] public int deathTick;
    [NonSerialized] public Entity killedBy;

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        health = maxHealth;
    }

    public void ApplyDamage(DamageEvent damageEvent, int tick)
    {
        if (health <= 0)
            return;

        health -= damageEvent.Damage;
        if (health <= 0)
        {
            killedBy = damageEvent.Instigator;
            deathTick = tick;
            health = 0;
        }
    }
}

