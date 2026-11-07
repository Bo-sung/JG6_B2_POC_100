using UnityEngine;

// A19 초중량화 — the air dive becomes a rocket dive (speed, duration) and the landing
// shockwave is huge, but diving down from higher than extra metres crushes the owner
// too. range/height: wave box, damage: wave, extra: fatal dive height, cooldown: wave.
public sealed class A19Runtime : AbilityRuntime
{
    const float WAVE_DURATION = 0.25f;
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
        var position = session.motor.Position;
        if (diveStartY - position.y > Data.extra)
        {
            ApplySelfHit(DamageKind.Fall);
            return;
        }
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        var wave = new Vector2(position.x, session.Data.rules.floorY + Data.height * 0.5f);
        SpawnEffect(wave, new Vector2(Data.range, Data.height), 0f, WAVE_DURATION,
            MakeHit(DamageKind.Physical, Data.damage), true, false);
    }

    public override void Cleanup()
    {
        diving = false;
    }
}
