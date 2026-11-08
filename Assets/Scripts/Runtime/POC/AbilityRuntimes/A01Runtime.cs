using UnityEngine;

// A01 핏빛 거미 — each attack leaves a lethal slash trail behind it, and touching that
// trail cuts the owner too. The trail starts just outside the hurtbox, so the swing
// itself is safe and lingering near the blade afterwards is what kills.
// range/height: trail box, duration: trail lifetime, damage: trail hit, cooldown: per attack.
public sealed class A01Runtime : AbilityRuntime
{
    public A01Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled || !Ready)
        {
            return;
        }
        SpendCooldown();
        var facing = session.motor.Facing;
        var extents = session.motor.hurtbox.bounds.extents.x;
        var start = session.motor.Position + new Vector2(facing * (extents + Data.spawnOffset + Data.range * 0.5f), 0);
        SpawnEffect(start, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, true);
    }
}
