using UnityEngine;

// A01 핏빛 거미 — each attack leaves a lethal slash trail behind it, and touching that
// trail cuts the owner too. range/height: trail box, duration: trail lifetime,
// damage: trail hit, cooldown: trail per attack.
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
        var center = session.motor.Position + new Vector2(session.motor.Facing * Data.range * 0.5f, 0);
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Physical, Data.damage), true, true);
    }
}
