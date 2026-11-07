using UnityEngine;

// A12 태양의 서자 — the attack becomes heavy and long-reached, but trading blows with an
// attacking boss (clash while boss.Attacking) sends the received shock back into the
// owner. damage/range/height: modified attack, cooldown: unused (flaw is always on).
public sealed class A12Runtime : AbilityRuntime
{
    public A12Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled)
        {
            return;
        }
        _request.damage = Data.damage;
        _request.range = Mathf.Max(_request.range, Data.range);
        _request.height = Mathf.Max(_request.height, Data.height);
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled || !session.boss.Attacking)
        {
            return;
        }
        var center = session.motor.Position + new Vector2(session.motor.Facing * _request.range * 0.5f, 0);
        var hitbox = new Bounds(center, new Vector3(_request.range, _request.height, 1f));
        if (hitbox.Intersects(session.boss.hurtbox.bounds))
        {
            ApplySelfHit(DamageKind.Self);
        }
    }
}
