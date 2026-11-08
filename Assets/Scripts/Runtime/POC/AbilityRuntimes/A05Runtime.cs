using UnityEngine;

// A05 공명의 천둥북 — a short guard stance blocks one physical hit and answers with a
// shockwave, but the resonance recoil hurls the owner backwards toward the hazards.
// duration: guard window, impactDuration: blast life, damage: blast, force/recoil*: recoil.
public sealed class A05Runtime : AbilityRuntime
{
    float guardRemaining;

    public A05Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
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

    public override void Tick(float _dt)
    {
        base.Tick(_dt);
        if (guardRemaining > 0f)
        {
            guardRemaining -= _dt;
        }
    }

    public override bool Defend(HitContext _hit)
    {
        if (guardRemaining <= 0f || _hit.kind != DamageKind.Physical)
        {
            return false;
        }
        guardRemaining = 0f;
        var facing = session.motor.Facing;
        var center = session.motor.Position + new Vector2(facing * Data.range * 0.5f, 0);
        SpawnEffect(center, new Vector2(Data.range * 2f, Data.height * 2f), 0f, Data.impactDuration,
            MakeHit(DamageKind.Explosion, Data.damage), true, false);
        session.motor.Impulse(new Vector2(-facing * Data.force, Data.recoilLift), Data.recoilDuration);
        return true;
    }

    public override void Cleanup()
    {
        guardRemaining = 0f;
    }
}
