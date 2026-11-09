using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerActionKind { Jump, Dash, Attack, Dive }

public struct PlayerActionRequest
{
    public PlayerActionKind kind;
    public float damage, range, height, speed, duration;
    public bool cancelled, phase, replaceBasic;
    public float sinkAcceleration;
    public Vector2 direction;
}

public sealed class PlayerActionController : MonoBehaviour
{
    public ArenaSession session;
    public InputActionAsset source;
    public Camera aimCamera;
    public Vector2 AimDirection => session.motor.AimDirection;
    InputActionAsset inputs;
    InputAction move, jump, dash, attack, skill, aim, guard;
    Vector2 movement, jumpAim, dashAim, attackAim, skillAim;
    bool wantsJump, wantsDash, wantsAttack, wantsSkill;
    float jumpBuffer, attackTimer, dashTimer;
    bool guardHeld, guardPressed, guardReleased;
    public bool Guarding { get; private set; }

    public void ResetGuard()
    {
        Guarding = guardHeld = guardPressed = guardReleased = false;
        if (session && session.motor && session.motor.guardVisual)
        {
            session.motor.guardVisual.gameObject.SetActive(false);
        }
    }

    void ProcessGuard()
    {
        bool next = (guardHeld || guardPressed) && !session.motor.Locked;
        bool started = next && (!Guarding || guardPressed);
        if (started)
        {
            session.abilities.Runtime?.GuardStarted();
        }
        if (next != Guarding || guardReleased)
        {
            Guarding = next;
            session.life.PublishState(next ? "guard_start" : "guard_end");
        }
        guardPressed = guardReleased = false;
        if (session && session.motor && session.motor.guardVisual)
        {
            session.motor.guardVisual.gameObject.SetActive(Guarding);
        }
    }

    public bool TryGuard(HitContext _hit)
    {
        if (!Guarding || session.motor.Locked || _hit.lifeId != 0 || !_hit.guardable || !_hit.hasOrigin || _hit.fatal)
        {
            return false;
        }
        var policy = new GuardPolicy { physical = true, halfAngle = session.Data.player.guardHalfAngle };
        session.abilities.Runtime?.ModifyGuard(ref policy);
        bool eligible = (_hit.kind == DamageKind.Physical && policy.physical) || (_hit.kind == DamageKind.Magic && policy.magic);
        Vector2 towardSource = _hit.incomingDirection.sqrMagnitude > 0.0001f
            ? -_hit.incomingDirection.normalized : (_hit.sourcePosition - session.motor.Position).normalized;
        if (!eligible || Vector2.Dot(AimDirection, towardSource) < Mathf.Cos(policy.halfAngle * Mathf.Deg2Rad))
        {
            return false;
        }
        session.abilities.Runtime?.GuardBlocked(_hit, AimDirection);
        session.life.PublishState("guard_block", _hit.source);
        return true;
    }

    void OnEnable()
    {
        inputs = Instantiate(source);
        move = inputs.FindAction("Player/Move", true);
        jump = inputs.FindAction("Player/Jump", true);
        dash = inputs.FindAction("Player/Dash", true);
        attack = inputs.FindAction("Player/Attack", true);
        skill = inputs.FindAction("Player/Skill", true);
        aim = inputs.FindAction("Player/Aim", true);
        guard = inputs.FindAction("Player/Guard", true);
        inputs.FindActionMap("Player", true).Enable();
    }

    void OnDisable()
    {
        ResetGuard();
        wantsJump = wantsDash = wantsAttack = wantsSkill = false;
        if (inputs)
        {
            inputs.Disable();
            Destroy(inputs);
        }
    }

    void Update()
    {
        movement = move.ReadValue<Vector2>();
        guardHeld = guard.IsPressed();
        guardPressed |= guard.WasPressedThisFrame();
        guardReleased |= guard.WasReleasedThisFrame();
        ReadAim();
        CaptureAim(jump, ref wantsJump, ref jumpAim);
        CaptureAim(dash, ref wantsDash, ref dashAim);
        CaptureAim(attack, ref wantsAttack, ref attackAim);
        CaptureAim(skill, ref wantsSkill, ref skillAim);
    }

    void ReadAim()
    {
        if (!aimCamera || !Application.isFocused || aim.activeControl == null || !aim.activeControl.device.added)
        {
            return;
        }
        var pointer = aim.ReadValue<Vector2>();
        if (!IsFinite(pointer.x) || !IsFinite(pointer.y))
        {
            return;
        }
        var ray = aimCamera.ScreenPointToRay(pointer);
        var plane = new Plane(Vector3.forward, session.motor.transform.position);
        if (plane.Raycast(ray, out float distance))
        {
            session.motor.SetAim((Vector2)ray.GetPoint(distance) - session.motor.Position);
        }
    }

    static bool IsFinite(float _value)
    {
        return !float.IsNaN(_value) && !float.IsInfinity(_value);
    }

    void CaptureAim(InputAction _action, ref bool _pending, ref Vector2 _direction)
    {
        if (_action.WasPressedThisFrame() && !_pending)
        {
            _direction = AimDirection;
            _pending = true;
        }
    }

    public void ResetState()
    {
        movement = Vector2.zero;
        ResetGuard();
        wantsJump = wantsDash = wantsAttack = wantsSkill = false;
        jumpBuffer = attackTimer = dashTimer = 0;
        jumpAim = dashAim = attackAim = skillAim = AimDirection;
    }

    void FixedUpdate()
    {
        if (!session.life.Alive || session.Finished)
        {
            ResetState();
            return;
        }
        ProcessGuard();
        var data = session.Data.player;
        float dt = Time.fixedDeltaTime;
        attackTimer -= dt;
        dashTimer -= dt;
        jumpBuffer = wantsJump ? data.jumpBuffer : jumpBuffer - dt;
        session.motor.SetMovement(movement);
        if (jumpBuffer > 0 && session.motor.CanJump)
        {
            Execute(PlayerActionKind.Jump, jumpAim);
            jumpBuffer = 0;
        }
        if (wantsDash && dashTimer <= 0)
        {
            Execute(PlayerActionKind.Dash, dashAim);
            dashTimer = data.dashCooldown;
        }
        if (wantsAttack && attackTimer <= 0 && !session.motor.Locked)
        {
            Execute(PlayerActionKind.Attack, attackAim);
            attackTimer = data.attackCooldown;
        }
        if (movement.y < -0.5f && !session.motor.Grounded)
        {
            Execute(PlayerActionKind.Dive, Vector2.down);
        }
        if (wantsSkill && !session.motor.Locked)
        {
            session.abilities.Activate(skillAim);
        }
        wantsJump = wantsDash = wantsAttack = wantsSkill = false;
    }

    public void Execute(PlayerActionKind _kind, Vector2 _aim)
    {
        if (session.motor.Locked || !session.life.Alive || session.Finished)
        {
            return;
        }
        var data = session.Data.player;
        var request = new PlayerActionRequest
        {
            kind = _kind,
            damage = data.attackDamage,
            range = data.attackRange,
            height = data.attackHeight,
            speed = data.dashSpeed,
            duration = data.dashDuration,
            direction = PlayerMotor2D.NormalizeAim(_aim, AimDirection)
        };
        session.abilities.Modify(ref request);
        if (request.cancelled || session.motor.Locked)
        {
            return;
        }
        switch (_kind)
        {
            case PlayerActionKind.Jump:
                session.motor.Jump();
                break;
            case PlayerActionKind.Dash:
                session.motor.Dash(request);
                break;
            case PlayerActionKind.Attack:
                if (request.replaceBasic)
                {
                    break;
                }
                var center = session.motor.Position + request.direction * (request.range * 0.5f);
                session.effects.Spawn(center, new Vector2(request.range, request.height), 0, data.attackDuration,
                    new HitContext(session.life.LifeId, session.abilities.Id, DamageKind.Physical, request.damage), true, false,
                    _angle: EffectSystem.Angle(request.direction));
                break;
        }
        session.abilities.After(request);
    }
}
