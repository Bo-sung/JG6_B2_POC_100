using UnityEngine;

// A04 강철의 숲 — blades erupt from the floor around the caster, including their own
// column; leave the blast columns fast or be impaled with the boss.
// delay + extra: blade stagger, duration: blade life, cooldown: per cast.
public sealed class A04Runtime : AbilityRuntime
{
    static readonly float[] BLADE_OFFSETS = { -3.3f, -1.1f, 0f, 1.1f, 3.3f };

    public A04Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready)
        {
            return;
        }
        SpendCooldown();
        var originX = session.motor.Position.x;
        var floorY = session.Data.rules.floorY;
        for (int i = 0; i < BLADE_OFFSETS.Length; i++)
        {
            var position = new Vector2(originX + BLADE_OFFSETS[i], floorY + Data.height * 0.5f);
            SpawnEffect(position, new Vector2(Data.range, Data.height), Data.delay + i * Data.extra,
                Data.duration, MakeHit(DamageKind.Physical, Data.damage), true, true);
        }
    }
}
