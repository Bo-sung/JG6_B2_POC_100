using UnityEngine;

// A17 살인 고함 — a wide sound wave sweeps everything in front, but the shout stuns the
// shouter for extra seconds right after; shout during the boss recovery only.
// range/height: wave box, duration: wave life, damage: wave, extra: self stun.
public sealed class A17Runtime : AbilityRuntime
{
    public A17Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        var center = session.motor.Position + new Vector2(session.motor.Facing * Data.range * 0.45f, 0);
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false);
        session.motor.Lock(Data.extra);
    }
}
