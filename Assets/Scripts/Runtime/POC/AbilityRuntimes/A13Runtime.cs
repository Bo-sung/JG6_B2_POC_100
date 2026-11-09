using UnityEngine;

public sealed class A13Runtime : AbilityRuntime
{
    bool tracking;
    float travel;
    Vector2 previous;

    public A13Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind == PlayerActionKind.Attack)
        {
            _request.damage = Data.damage;
        }
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack)
        {
            return;
        }
        if (!tracking)
        {
            travel = 0;
            previous = session.motor.Position;
            tracking = true;
        }
        session.motor.Impulse(-_request.direction * Data.force, Data.duration);
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (!tracking)
        {
            return;
        }
        travel += Vector2.Distance(previous, session.motor.Position);
        previous = session.motor.Position;
        if (travel > Data.travelLimit)
        {
            ApplySelfHit(DamageKind.Self);
        }
        if (session.motor.Grounded && !session.motor.Recoiling)
        {
            tracking = false;
        }
    }

    public override void Landed(float _speed)
    {
        if (tracking && (session.motor.FallDistance > Data.fatalFallHeight || _speed > Data.fatalImpactSpeed))
        {
            ApplySelfHit(DamageKind.Fall);
        }
    }

    public override void Cleanup()
    {
        tracking = false;
        travel = 0;
    }
}
