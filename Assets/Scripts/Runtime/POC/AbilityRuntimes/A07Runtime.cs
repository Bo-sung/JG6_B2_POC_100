using UnityEngine;

// A07 대지의 별 — a slow homing star chases the boss through everything on its orbit,
// owner included; launching with the boss behind you means the star crosses your own
// body. speed: homing speed, duration: lifetime, range/height: star box, cooldown: cast.
public sealed class A07Runtime : AbilityRuntime
{
    const float LAUNCH_OFFSET = 1.4f;
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
        var direction = AimToBoss();
        star = SpawnEffect(session.motor.Position + direction * LAUNCH_OFFSET,
            new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, true);
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (star == null || !star.live)
        {
            return;
        }
        var target = (Vector2)session.boss.hurtbox.bounds.center;
        star.transform.position = Vector2.MoveTowards(star.transform.position, target, Data.speed * _dt);
    }

    public override void Cleanup()
    {
        if (star != null && star.live)
        {
            star.Clear();
        }
        star = null;
    }
}
