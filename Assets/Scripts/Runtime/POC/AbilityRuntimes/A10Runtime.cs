using UnityEngine;

// A10 나마흐의 눈물 — attack impacts splash corrosive fluid, but at point-blank range
// (boss within extra metres of the caster) the backsplash lands on the owner too.
// range/height: splash box, duration: splash life, damage: splash, extra: backsplash range.
public sealed class A10Runtime : AbilityRuntime
{
    public A10Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled || !Ready)
        {
            return;
        }
        SpendCooldown();
        var center = session.motor.Position + new Vector2(session.motor.Facing * Data.range * 0.6f, 0);
        var bounds = session.boss.hurtbox.bounds;
        bool pointBlank = bounds.SqrDistance(session.motor.Position) <= Data.extra * Data.extra;
        SpawnEffect(center, new Vector2(Data.range, Data.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, pointBlank);
    }
}
