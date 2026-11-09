using System;
using UnityEngine;

public sealed class AbilityController : MonoBehaviour
{
    public ArenaSession session;
    public AbilityDefinition[] definitions;
    public AbilityRuntime Runtime { get; private set; }
    public string Id => Runtime == null ? string.Empty : Runtime.Data.id;

    public void Equip(string _id)
    {
        Cleanup();
        var definition = Array.Find(definitions, _entry => _entry.id == _id);
        if (!definition)
        {
            throw new InvalidOperationException("Missing ability definition: " + _id);
        }
        session.life.ResetHealth();
        session.damage.ResetHits();
        session.actions.ResetGuard();
        Runtime = AbilityRuntime.Create(definition.Data, session);
    }

    public void Cleanup()
    {
        Runtime?.Cleanup();
        Runtime = null;
        session.actions.ResetGuard();
        session.motor.ClearTransientState();
        session.life.ResetHealth();
    }

    public void Modify(ref PlayerActionRequest _request)
    {
        Runtime?.Modify(ref _request);
    }
    public void After(PlayerActionRequest _request)
    {
        Runtime?.After(_request);
    }
    public void Landed(float _speed)
    {
        Runtime?.Landed(_speed);
    }
    public bool Defend(HitContext _hit)
    {
        return Runtime != null && Runtime.Defend(_hit);
    }
    public void Activate()
    {
        Activate(session.actions.AimDirection);
    }
    public void Activate(Vector2 _direction)
    {
        if (Runtime != null && Runtime.Data.active && Runtime.Ready)
        {
            Runtime.CaptureAim(_direction);
            Runtime.Activate();
        }
    }
    void FixedUpdate()
    {
        if (session.life.Alive && !session.Finished)
        {
            Runtime?.Tick(Time.fixedDeltaTime);
        }
    }
}
