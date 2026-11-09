using UnityEngine;

public sealed class A11Runtime : AbilityRuntime
{
    public A11Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind == PlayerActionKind.Attack)
        {
            _request.damage = Data.damage;
        }
    }

    public override void ModifyGuard(ref GuardPolicy _policy)
    {
        _policy.physical = false;
        _policy.magic = true;
    }
}
