using UnityEngine;

// A14 거대화 — grow (height = scale multiplier) for duration seconds; the hurtbox grows
// with the body, ceiling contact becomes lethal, and attacks hit harder and wider
// (extra = range multiplier, damage = giant attack). Cooldown gates the transformation.
public sealed class A14Runtime : AbilityRuntime
{
    float giantRemaining;
    bool giant;

    public A14Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready || giant)
        {
            return;
        }
        SpendCooldown();
        giant = true;
        giantRemaining = Data.duration;
        session.motor.transform.localScale = Vector3.one * Data.height;
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (!giant || _request.kind != PlayerActionKind.Attack || _request.cancelled)
        {
            return;
        }
        _request.damage = Data.damage;
        _request.range *= Data.extra;
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (!giant)
        {
            return;
        }
        giantRemaining -= _dt;
        if (giantRemaining <= 0f)
        {
            EndGiant();
        }
    }

    public override void Cleanup()
    {
        EndGiant();
    }

    void EndGiant()
    {
        giant = false;
        if (session.motor != null)
        {
            session.motor.transform.localScale = Vector3.one;
        }
    }
}
