//#define INCLUDEMATHCHECKS

using System;
using Unity.Mathematics;
using Unity.Sample.Core;
using UnityEngine;


// Collection of converted classic Unity (Mathf, Vector3 etc.) + some homegrown math functions using Unity.Mathematics
// These are made/converted for production and unlike a proper library they are lacking any tests, so use at your own peril!
public static class MathHelper
{
    const float kEpsilonNormalSqrt = 1e-15F;
    // TODO: Should likely be platform dependent, maybe a value in unity.math?
    const float kEpisilon = 1.17549435E-38f;

    [ConfigVar(Name = "math.show.comparison", DefaultValue = "1", Description = "Show old vs new math comparison")]
    public static ConfigVar CompareMath;


    public static uint hash(uint i)
    {
        return i * 0x83B58237u + 0xA9D919BFu;
    }

    public static uint hash(int i)
    {
        return (uint)i * 0x83B58237u + 0xA9D919BFu;
    }


    // Sign function that has the old Mathf behaviour of returning 1 if f == 0
    static float MathfStyleZeroIsOneSign(float f)
    {
        return f >= 0F ? 1F : -1F;
    }

    //
    public static float SignedAngle(float a, float b)
    {
        var difference = b - a;
        var sign = math.sign(difference);
        var offset = sign * 180.0f;

        return ((difference + offset) % 360.0f) - offset;
    }

    public static float SignedAngle(float3 from, float3 to, float3 axis)
    {
        float unsignedAngle = Angle(from, to);
        float sign = MathfStyleZeroIsOneSign(math.dot(axis, math.cross(from, to)));
        var result = unsignedAngle * sign;

#if INCLUDEMATHCHECKS
        var oldMath = Vector3.SignedAngle(from, to, axis);
        if (math.abs(oldMath - result) > 0.1f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SignedAngle: Result not within tolerance! {0} : {1}", oldMath, result);
#endif
        return result;
    }


    public static float Angle(float3 from, float3 to)
    {
        float result;

        // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
        float denominator = math.sqrt(math.lengthsq(from) * math.lengthsq(to));

        if (denominator < kEpsilonNormalSqrt)
        {
            result = 0F;
        }
        else
        {
            float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
            result = math.degrees(math.acos(dot));
        }

#if INCLUDEMATHCHECKS
        var oldMath = Vector3.Angle(from, to);
        if (math.abs(oldMath - result) > 0.1f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.Angle: Result not within tolerance! {0} : {1}", oldMath, result);
#endif
        return result;
    }


    public static float MoveTowards(float current, float target, float maxDelta)
    {
        float result;

        if (math.abs(target - current) <= maxDelta)
            result =  target;
        else
            result = current + MathfStyleZeroIsOneSign(target - current) * maxDelta;

#if INCLUDEMATHCHECKS
        var oldMath = Mathf.MoveTowards(current, target, maxDelta);
        if (math.abs(oldMath - result) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.MoveTowards: Result not within tolerance! {0} : {1}", oldMath, result);
#endif
        return result;
    }


    public static float LerpAngle(float a, float b, float t)
    {
        float delta = Repeat((b - a), 360);
        if (delta > 180)
            delta -= 360;
        var result = a + delta * math.clamp(t,0f, 1f);

#if INCLUDEMATHCHECKS
        var oldMath = Mathf.LerpAngle(a, b, t);
        if (math.abs(oldMath - result) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.LerpAngle: Result not within tolerance! {0} : {1}", oldMath, result);
#endif
        return result;
    }


    public static float Repeat(float t, float length)
    {
        return math.clamp(t - math.floor(t / length) * length, 0.0f, length);
    }


    public static float DeltaAngle(float current, float target)
    {
        float delta = Repeat((target - current), 360.0F);
        if (delta > 180.0F)
            delta -= 360.0F;
        var result = delta;

#if INCLUDEMATHCHECKS
        var oldMath = Mathf.DeltaAngle(current, target);
        if (math.abs(oldMath - result) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.DeltaAngle: Result not within tolerance! {0} : {1}", oldMath, result);
#endif
        return result;
    }

    public static float SmoothDampAngle(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime)
    {

#if INCLUDEMATHCHECKS
        var currentVelocityDebug = currentVelocity;
#endif

        target = current + DeltaAngle(current, target);
        var result = SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);

#if INCLUDEMATHCHECKS
        // TODO: (sunek) Double check that this is atually a copy!?
        var oldMath = Mathf.SmoothDampAngle(current, target, ref currentVelocityDebug, smoothTime, maxSpeed, deltaTime);

        if (math.abs(oldMath - result) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDampAngle: Result not within tolerance! {0} : {1}", oldMath, result);

        if (math.abs(currentVelocity - currentVelocityDebug) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDampAngle: Current Velocity not within tolerance! {0} : {1}", currentVelocityDebug, currentVelocity);
#endif

        return result;
    }


    public static float SmoothDamp
    (
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime)
    {

#if INCLUDEMATHCHECKS
        var currentVelocityDebug = currentVelocity;
#endif

        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = math.max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
        float change = current - target;
        float originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = math.clamp(change, -maxChange, maxChange);
        target = current - change;

        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float result = target + (change + temp) * exp;

        // Prevent overshooting
        if (originalTo - current > 0.0F == result > originalTo)
        {
            result = originalTo;
            currentVelocity = (result - originalTo) / deltaTime;
        }

#if INCLUDEMATHCHECKS
        var oldMath = Mathf.SmoothDamp(current, target, ref currentVelocityDebug, smoothTime, maxSpeed, deltaTime);

        if (math.abs(oldMath - result) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDamp: Result not within tolerance! {0} : {1}", oldMath, result);

        if (math.abs(currentVelocity - currentVelocityDebug) > 0.00001f)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDamp: Current Velocity not within tolerance! {0} : {1}", currentVelocityDebug, currentVelocity);
#endif
        return result;
    }


    // Projects a vector onto another vector.
    public static float3 Project(float3 vector, float3 onNormal)
    {
        float3 result;

        float sqrMag = math.dot(onNormal, onNormal);
        if (sqrMag < kEpisilon)
            result = float3.zero;
        else
            result = onNormal * math.dot(vector, onNormal) / sqrMag;

#if INCLUDEMATHCHECKS
        var oldMath = Vector3.Project(vector, onNormal);
        if (oldMath != (Vector3)result)
        {
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.Project: Result not within tolerance! {0} : {1}", oldMath, result);
        }
#endif
        return result;
    }


    public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {
        var result = vector - Project(vector, planeNormal);

#if INCLUDEMATHCHECKS
        var oldMath = Vector3.ProjectOnPlane(vector, planeNormal);
        if (oldMath != (Vector3)result)
        {
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.ProjectOnPlane: Result not within tolerance! {0} : {1}", oldMath, result);
        }
#endif
        return result;
    }

    public static float2 ClampMagnitude(float2 vector, float maxLength)
    {
        if (math.lengthsq(vector) > maxLength * maxLength)
            return math.normalizesafe(vector) * maxLength;
        return vector;
    }

    public static float2 SmoothDamp(
        float2 current,
        float2 target,
        ref float2 currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime)
    {

#if INCLUDEMATHCHECKS
        Vector2 currentVelocityDebug = currentVelocity;
#endif
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = math.max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
        float2 change = current - target;
        float2 originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = ClampMagnitude(change, maxChange);
        target = current - change;

        float2 temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float2 output = target + (change + temp) * exp;

        // Prevent overshooting
        if (math.dot(originalTo - current, output - originalTo) > 0)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

#if INCLUDEMATHCHECKS
        // TODO: (sunek) Double check that this is atually a copy!?
        var oldMath = Vector2.SmoothDamp(current, target, ref currentVelocityDebug, smoothTime, maxSpeed, deltaTime);

        if (oldMath != (Vector2)output)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDampVector2: Result not within tolerance! {0} : {1}", oldMath, output);

        if ((Vector2)currentVelocity != currentVelocityDebug)
            GameDebug.Log(WorldId.Undefined, CompareMath, "MathHelper.SmoothDampVector2: Current Velocity not within tolerance! {0} : {1}", currentVelocityDebug, currentVelocity);
#endif
        return output;
    }
}
