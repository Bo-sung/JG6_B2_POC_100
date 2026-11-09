using UnityEngine;

public sealed class A07Runtime : AbilityRuntime
{
    EffectSlot star;

    public A07Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready || (star != null && star.live))
        {
            return;
        }
        SpendCooldown();
        var half = session.motor.hurtbox.bounds.extents;
        // Separating plane along the launch axis includes the rotated projectile half-length.
        // The old fixed 1.4 offset could overlap the combined extents at diagonal launch angles.
        float support = Mathf.Abs(castDirection.x) * half.x + Mathf.Abs(castDirection.y) * half.y;
        Vector2 muzzle = (Vector2)session.motor.hurtbox.bounds.center + castDirection * (support + Data.range * .5f + Data.spawnOffset);
        star = SpawnEffect(muzzle, new Vector2(Data.range, Data.height), 0, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, true, _angle: EffectSystem.Angle(castDirection));
        if (star)
        {
            // The slot owns movement and its swept previousPosition: no external transform race.
            star.HomingTarget = session.boss.transform;
            star.HomingSpeed = Data.speed;
        }
    }

    public override void Cleanup()
    {
        if (star != null)
        {
            star.Clear();
        }
        star = null;
    }
}
