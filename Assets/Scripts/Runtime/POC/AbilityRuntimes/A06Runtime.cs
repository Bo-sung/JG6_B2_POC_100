using UnityEngine;

// A06 뿌리의 지팡이 — plants a root strike at the cast spot that detonates after a long
// delay; the cast is very slow, so the boss must be lured onto the planted spot.
// delay: fuse, range/height: strike box, duration: strike life, cooldown: per cast.
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
        var position = session.motor.Position;
        var strike = new Vector2(position.x, session.Data.rules.floorY + Data.height * 0.5f);
        SpawnEffect(strike, new Vector2(Data.range, Data.height), Data.delay, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, false);
    }
}
