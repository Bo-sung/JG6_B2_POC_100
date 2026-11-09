using UnityEngine;

// Counter on a successful ordinary held guard. There is no timing/parry window.
public sealed class A05Runtime : AbilityRuntime
{
    public A05Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void GuardBlocked(HitContext _hit, Vector2 _direction)
    {
        if (!Ready || _hit.kind != DamageKind.Physical)
        {
            return;
        }
        SpendCooldown();
        var center = session.motor.Position + _direction * (Data.range * .5f);
        SpawnEffect(center, new Vector2(Data.range * 2, Data.height * 2), 0, Data.impactDuration,
            MakeHit(DamageKind.Explosion, Data.damage), true, false, _angle: EffectSystem.Angle(_direction));
        session.motor.Impulse(-_direction * Data.force + Vector2.up * Data.recoilLift, Data.recoilDuration);
    }
}
