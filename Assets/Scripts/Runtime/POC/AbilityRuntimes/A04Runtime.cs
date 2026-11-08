using UnityEngine;

// A04 강철의 숲 — blades erupt from the floor around the caster, including their own
// column; leave the blast columns fast or be impaled with the boss.
// delay + extra·i staggers emissionCount blades spaced by spacing, duration: blade life.
public sealed class A04Runtime : AbilityRuntime
{
    public A04Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Activate()
    {
        if (!Ready || Data.emissionCount <= 0)
        {
            return;
        }
        SpendCooldown();
        var originX = session.motor.Position.x;
        var floorY = session.Data.rules.floorY;
        for (int i = 0; i < Data.emissionCount; i++)
        {
            float offset = (i - (Data.emissionCount - 1) * 0.5f) * Data.spacing;
            var position = new Vector2(originX + offset, floorY + Data.height * 0.5f);
            SpawnEffect(position, new Vector2(Data.range, Data.height), Data.delay + i * Data.extra,
                Data.duration, MakeHit(DamageKind.Physical, Data.damage), true, true);
        }
    }
}
