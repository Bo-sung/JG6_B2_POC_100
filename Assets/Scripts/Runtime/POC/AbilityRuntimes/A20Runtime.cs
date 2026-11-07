using UnityEngine;

// A20 유체화 — the dash turns phased: enemy attacks, obstacles and even the floor are
// passed through; dash across the pit and the void below finishes the trip.
public sealed class A20Runtime : AbilityRuntime
{
    public A20Runtime(AbilityData _data, ArenaSession _session) : base(_data, _session)
    {
    }

    public override void Modify(ref PlayerActionRequest _request)
    {
        if (_request.kind != PlayerActionKind.Dash || _request.cancelled)
        {
            return;
        }
        _request.phase = true;
    }
}
