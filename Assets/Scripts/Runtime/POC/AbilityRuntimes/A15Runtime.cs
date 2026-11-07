using UnityEngine;

// A15 산성 눈물 — a short acid spray hits the front, but acid also pools at the cast spot
// and melts the owner who fires and stays. range/height: spray box, duration: spray life,
// damage: spray, cooldown: per cast.
public sealed class A15Runtime : AbilityRuntime
{
    const float NEAR_POOL_SIZE = 2.2f;
    const float NEAR_POOL_DELAY = 0.05f;
    const float NEAR_POOL_EXTRA = 0.3f;

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
        var direction = new Vector2(session.motor.Facing, 0);
        var center = session.motor.Position + direction * (Data.range * 0.5f);
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false);
        SpawnEffect(session.motor.Position, new Vector2(NEAR_POOL_SIZE, NEAR_POOL_SIZE * 0.9f),
            NEAR_POOL_DELAY, Data.duration + NEAR_POOL_EXTRA,
            MakeHit(DamageKind.Magic, 1f), false, true);
    }
}
