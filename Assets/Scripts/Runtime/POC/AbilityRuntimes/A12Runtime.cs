using UnityEngine;

public sealed class A12Runtime : AbilityRuntime
{
    public A12Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
        session.life.ResetHealth(Data.maxHealth);
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind == PlayerActionKind.Attack)
        {
            _request.damage = Data.damage;
            _request.range = Data.range;
            _request.height = Data.height;
        }
    }

    public override void GuardStarted()
    {
        session.life.ArmGuardVulnerability();
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        session.motor.SetAim(castDirection);
        session.motor.Immobilize(Data.skillCharge, true);
        var center = session.motor.Position + castDirection * (Data.skillRange * .5f);
        SpawnEffect(center, new Vector2(Data.skillRange, Data.skillHeight), Data.skillCharge, Data.skillDuration,
            MakeHit(DamageKind.Physical, Data.skillDamage), true, false, _angle: EffectSystem.Angle(castDirection));
    }
}
