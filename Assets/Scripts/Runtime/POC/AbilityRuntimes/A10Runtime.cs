using UnityEngine;

// A10 나마흐의 눈물 — the splash only sheds off an actual impact: a whiffed swing sheds
// no fluid. When the boss is point-blank (within extra metres) the backsplash lands on
// the owner too. range/height: splash box, duration: splash life, damage: splash,
// extra: backsplash range.
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
        var center = session.motor.Position + new Vector2(session.motor.Facing * _request.range * 0.5f, 0);
        var hitbox = new Bounds(center, new Vector3(_request.range, _request.height, 1f));
        var bossBounds = session.boss.hurtbox.bounds;
        if (!hitbox.Intersects(bossBounds))
        {
            return;
        }
        SpendCooldown();
        bool pointBlank = bossBounds.SqrDistance(session.motor.Position) <= Data.extra * Data.extra;
        SpawnEffect(center, new Vector2(_request.range, _request.height), 0f, Data.duration,
            MakeHit(DamageKind.Magic, Data.damage), true, pointBlank);
    }
}
