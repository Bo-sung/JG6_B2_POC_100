using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerActionKind { Jump, Dash, Attack, Dive }

public struct PlayerActionRequest
{
    public PlayerActionKind kind;
    public float damage, range, height, speed, duration;
    public bool cancelled, phase;
    public Vector2 direction;
}

public sealed class PlayerActionController : MonoBehaviour
{
    public ArenaSession session;
    public InputActionAsset source;
    InputActionAsset inputs;
    InputAction move, jump, dash, attack, skill;
    Vector2 movement;
    bool wantsJump, wantsDash, wantsAttack, wantsSkill;
    float jumpBuffer, attackTimer, dashTimer;

    void OnEnable()
    {
        inputs = Instantiate(source);
        move = inputs.FindAction("Player/Move", true);
        jump = inputs.FindAction("Player/Jump", true);
        dash = inputs.FindAction("Player/Dash", true);
        attack = inputs.FindAction("Player/Attack", true);
        skill = inputs.FindAction("Player/Skill", true);
        inputs.FindActionMap("Player", true).Enable();
    }

    void OnDisable()
    {
        if (inputs)
        {
            inputs.Disable();
            Destroy(inputs);
        }
    }

    void Update()
    {
        movement = move.ReadValue<Vector2>();
        wantsJump |= jump.WasPressedThisFrame();
        wantsDash |= dash.WasPressedThisFrame();
        wantsAttack |= attack.WasPressedThisFrame();
        wantsSkill |= skill.WasPressedThisFrame();
    }

    public void ResetState()
    {
        movement = Vector2.zero;
        wantsJump = wantsDash = wantsAttack = wantsSkill = false;
        jumpBuffer = attackTimer = dashTimer = 0;
    }

    void FixedUpdate()
    {
        if (!session.life.Alive || session.Finished)
        {
            ResetState();
            return;
        }
        var data = session.Data.player;
        float dt = Time.fixedDeltaTime;
        attackTimer -= dt;
        dashTimer -= dt;
        jumpBuffer = wantsJump ? data.jumpBuffer : jumpBuffer - dt;
        session.motor.SetMovement(movement);
        if (jumpBuffer > 0 && session.motor.CanJump)
        {
            Execute(PlayerActionKind.Jump, movement);
            jumpBuffer = 0;
        }
        if (wantsDash && dashTimer <= 0)
        {
            Execute(PlayerActionKind.Dash, movement);
            dashTimer = data.dashCooldown;
        }
        if (wantsAttack && attackTimer <= 0 && !session.motor.Locked)
        {
            Execute(PlayerActionKind.Attack, movement);
            attackTimer = data.attackCooldown;
        }
        if (movement.y < -0.5f && !session.motor.Grounded)
        {
            Execute(PlayerActionKind.Dive, movement);
        }
        if (wantsSkill && !session.motor.Locked)
        {
            session.abilities.Activate();
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
            direction = _aim.sqrMagnitude > 0.1f ? _aim.normalized : new Vector2(session.motor.Facing, 0)
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
                var center = session.motor.Position + new Vector2(session.motor.Facing * request.range * 0.5f, 0);
                session.effects.Spawn(center, new Vector2(request.range, request.height), 0, data.attackDuration,
                    new HitContext(session.life.LifeId, session.abilities.Id, DamageKind.Physical, request.damage), true, false);
                break;
        }
        session.abilities.After(request);
    }
}
