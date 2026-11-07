using UnityEngine;

// A16 인간 폭탄 — locks the owner for the charge (delay) then detonates a huge blast;
// the suicide flag resolves the boss damage first and then kills the owner for certain.
// damage: blast (the one 150 hit), range/height: blast box, cooldown: per detonation.
public sealed class A16Runtime : AbilityRuntime
{
    public A16Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        session.motor.Lock(Data.delay);
        SpawnEffect(session.motor.Position, new Vector2(Data.range * 2f, Data.height * 2f),
            Data.delay, Data.duration, MakeHit(DamageKind.Explosion, Data.damage),
            true, false, default, true);
    }
}
