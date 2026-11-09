using UnityEngine;

public sealed class A06Runtime : AbilityRuntime
{
    public A06Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        session.motor.SetAim(castDirection);
        session.motor.Immobilize(Data.delay, true);
        var center = session.motor.Position + castDirection * (Data.range * .5f);
        SpawnEffect(center, new Vector2(Data.range, Data.height), Data.delay, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, false, _angle: EffectSystem.Angle(castDirection));
    }
}
