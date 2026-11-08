using UnityEngine;

// A15 산성 눈물 — a short acid spray hits the front. Only when the spray actually lands
// on the boss at close range does acid splash back at the cast spot, and the pool holds
// off for selfDelay seconds so stepping aside survives it. range/height: spray box,
// duration: spray life, self*: backsplash pool, damage: spray, cooldown: per cast.
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
        var facing = session.motor.Facing;
        var center = session.motor.Position + new Vector2(facing * Data.range * 0.5f, 0);
        var spray = new Bounds(center, new Vector3(Data.range, Data.height, 1f));
        var bossBounds = session.boss.hurtbox.bounds;
        bool closeContact = spray.Intersects(bossBounds) && bossBounds.SqrDistance(session.motor.Position) <= Data.extra * Data.extra;
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false);
        if (closeContact)
        {
            SpawnEffect(session.motor.Position, new Vector2(Data.selfWidth, Data.selfHeight),
                Data.selfDelay, Data.selfDuration,
                MakeHit(DamageKind.Magic, 1f), false, true);
        }
    }
}
