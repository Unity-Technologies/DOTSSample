using System;

[Serializable]
public struct SplashDamageSettings
{
    public float radius;
    public float falloffStartRadius;
    public float damage;
    public float minDamage;
    public float impulse;
    public float minImpulse;
    public float ownerDamageFraction;

    public void Calculate(float distance, bool selfDamage, out float damageResult, out float impulseResult)
    {
        if (distance > radius)
        {
            damageResult = 0;
            impulseResult = 0;
            return;
        }

        damageResult = damage;
        impulseResult = impulse;

        if (distance > falloffStartRadius)
        {
            var falloffFraction = (distance - falloffStartRadius) / (radius - falloffStartRadius);
            damageResult -= (damage - minDamage) * falloffFraction;
            impulseResult -= (impulse - minImpulse) * falloffFraction;
        }

        if (selfDamage)
            damageResult = damageResult * ownerDamageFraction;
    }
}
