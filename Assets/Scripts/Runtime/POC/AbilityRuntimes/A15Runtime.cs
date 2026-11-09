using UnityEngine;

public sealed class A15Runtime : AbilityRuntime
{
    public A15Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
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
        session.motor.Immobilize(Data.duration, true);
        Vector2 origin = session.motor.Position;
        var spray = SpawnEffect(origin + castDirection * (Data.range * .5f), new Vector2(Data.range, Data.height),
            0, Data.duration, MakeHit(DamageKind.Magic, Data.damage), true, false, _angle: EffectSystem.Angle(castDirection));
        if (spray)
        {
            spray.FollowTarget = session.motor.transform;
            spray.FollowOffset = castDirection * (Data.range * .5f);
        }
        SpawnEffect(origin, new Vector2(Data.selfWidth, Data.selfHeight), Data.selfDelay, Data.selfDuration,
            MakeHit(DamageKind.Magic, Data.damage), true, true);
    }
}
