using UnityEngine;

// A07 대지의 별 — the star always launches forward and then homes to the boss; when the
// boss stands behind you the return orbit sweeps back through your own body, so dodging
// the orbit is a real choice. speed: homing speed, duration: lifetime, spawnOffset: launch
// distance, range/height: star box, cooldown: cast.
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
        var direction = new Vector2(session.motor.Facing, 0);
        star = SpawnEffect(session.motor.Position + direction * Data.spawnOffset,
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
