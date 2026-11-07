using UnityEngine;

// A18 차원 충격파 — charging (delay) pins the owner in place, then a pierce beam crosses
// the whole arena; commit to the boss long recovery or be a sitting target.
// delay: charge, range/height: beam box, duration: beam life, damage: beam, cooldown: cast.
public sealed class A18Runtime : AbilityRuntime
{
    public A18Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        session.motor.Lock(Data.delay + Data.duration);
        var center = session.motor.Position + new Vector2(session.motor.Facing * Data.range * 0.5f, 0);
        SpawnEffect(center, new Vector2(Data.range, Data.height), Data.delay, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false);
    }
}
