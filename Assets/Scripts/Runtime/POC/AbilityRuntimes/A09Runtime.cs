using UnityEngine;

public sealed class A09Runtime : AbilityRuntime
{
    public A09Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind == PlayerActionKind.Attack)
        {
            _request.direction = AimToBoss();
            _request.range = Data.range;
            _request.height = Data.height;
            _request.damage = Data.damage;
        }
    }

    public override void ModifyGuard(ref GuardPolicy _policy)
    {
        _policy.physical = _policy.magic = false;
    }
}
