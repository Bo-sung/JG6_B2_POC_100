using UnityEngine;

// A19 초중량화 — the air dive becomes a rocket dive (speed, duration) and every dive
// landing detonates a huge shockwave (impactDuration); the boss is resolved on the wave
// before the suicide flag claims the owner, so even fatal high dives still pay out.
// Diving down from higher than extra metres is lethal regardless of the payout.
// range/height: wave box, damage: wave, extra: fatal dive height, cooldown: wave.
public sealed class A19Runtime : AbilityRuntime
{
    float diveStartY;
    bool diving;

    public A19Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Dive || _request.cancelled)
        {
            return;
        }
        if (!diving)
        {
            diving = true;
            diveStartY = session.motor.Position.y;
        }
        session.motor.Impulse(new Vector2(0f, -Data.speed), Data.duration);
    }

    public override void Landed(float _speed)
    {
        if (!diving)
        {
            return;
        }
        diving = false;
        bool fatal = diveStartY - session.motor.Position.y > Data.extra;
        if (!fatal && !Ready)
        {
            return;
        }
        SpendCooldown();
        var position = session.motor.Position;
        var wave = new Vector2(position.x, session.Data.rules.floorY + Data.height * 0.5f);
        var effect = SpawnEffect(wave, new Vector2(Data.range, Data.height), 0f, Data.impactDuration,
            MakeHit(DamageKind.Physical, Data.damage), true, false, default, fatal);
        if (effect != null)
        {
            effect.SuicideKind = DamageKind.Fall;
        }
    }

    public override void Cleanup()
    {
        diving = false;
    }
}
