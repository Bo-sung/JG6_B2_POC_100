using UnityEngine;

// A13 초근력 — the punch is massively stronger, but the same swing launches the owner
// backwards (force exceeds the wall death speed) toward walls and ledges.
// damage: punch, force: recoil launch, duration: recoil time, cooldown: unused.
public sealed class A13Runtime : AbilityRuntime
{
    public A13Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled)
        {
            return;
        }
        _request.damage = Data.damage;
    }

    public override void After(PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Attack || _request.cancelled)
        {
            return;
        }
        float direction = Mathf.Abs(_request.direction.x) > 0.1f ? Mathf.Sign(_request.direction.x) : session.motor.Facing;
        session.motor.Impulse(new Vector2(-direction * Data.force, 0f), Data.duration);
    }
}
