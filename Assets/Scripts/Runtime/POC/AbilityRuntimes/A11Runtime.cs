using UnityEngine;

// A11 신뢰의 방벽 — duration seconds of perfect protection against magic hits only;
// physical blades and dashes go straight through the barrier untouched.
// duration: barrier window, cooldown: per cast.
public sealed class A11Runtime : AbilityRuntime
{
    float guardRemaining;

    public A11Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        guardRemaining = Data.duration;
    }

    public override bool Defend(HitContext _hit)
    {
        return guardRemaining > 0f && _hit.kind == DamageKind.Magic;
    }

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (guardRemaining > 0f)
        {
            guardRemaining -= _dt;
        }
    }

    public override void Cleanup()
    {
        guardRemaining = 0f;
    }
}
