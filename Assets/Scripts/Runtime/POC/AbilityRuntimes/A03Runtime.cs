using UnityEngine;

// A03 백염의 병 — a narrow lethal beam fires forward, but the launch position turns into
// a lingering danger zone; step out immediately or melt in your own flame.
// range: beam length, height: beam width, duration: beam life, cooldown: per cast.
public sealed class A03Runtime : AbilityRuntime
{
    const float BACKBLAST_DELAY = 0.2f;
    const float BACKBLAST_EXTRA = 0.6f;

    public A03Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        var direction = new Vector2(session.motor.Facing, 0);
        var center = session.motor.Position + direction * (Data.range * 0.5f);
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, false);
        SpawnEffect(session.motor.Position, new Vector2(Data.height * 1.4f, Data.height * 1.4f),
            BACKBLAST_DELAY, Data.duration + BACKBLAST_EXTRA,
            MakeHit(DamageKind.Magic, 1f), false, true);
    }
}
