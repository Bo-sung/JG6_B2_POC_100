using UnityEngine;

public sealed class A19Runtime : AbilityRuntime
{
    public A19Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (session.motor.Heavy && _request.kind == PlayerActionKind.Dash)
        {
            _request.cancelled = true;
        }
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        session.motor.StartHeavy(Data.duration, Data.gravityMultiplier, Data.speed);
    }

    public override void Landed(float _speed)
    {
        if (!session.motor.Heavy)
        {
            return;
        }
        // AirPeak predates activation, so activating just before landing cannot erase the fall.
        bool fatal = session.motor.FallDistance > Data.extra || _speed > Data.fatalImpactSpeed;
        Vector2 wave = new Vector2(session.motor.Position.x, session.motor.hurtbox.bounds.min.y + Data.height * .5f);
        var effect = SpawnEffect(wave, new Vector2(Data.range, Data.height), 0, Data.impactDuration,
            MakeHit(DamageKind.Physical, Data.damage), true, false, _suicide: fatal);
        if (effect)
        {
            effect.SuicideKind = DamageKind.Fall;
        }
        else if (fatal)
        {
            ApplySelfHit(DamageKind.Fall, true);
        }
        session.motor.EndHeavy();
    }

    public override void Cleanup()
    {
        session.motor.EndHeavy();
    }
}
