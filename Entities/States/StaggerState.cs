namespace AlongJourney.Entities.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Components;
using AlongJourney.Entities;

public enum StaggerExitPolicy
{
    WhenTimerEnds,
    WhenKnockbackInactive
}

/// <summary>
/// Hit reaction: knockback from <see cref="Actor.BlackboardKeys.HitSource"/>.
/// Exit either after a fixed time (player-style) or when knockback finishes (NPC-style), configured per scene.
/// </summary>
public partial class StaggerState : State
{
    [Export] public StaggerExitPolicy ExitPolicy { get; set; } = StaggerExitPolicy.WhenTimerEnds;
    [Export] public float StaggerDuration { get; set; } = 0.25f;
    [Export] public bool ClearMoveIntentOnEnter { get; set; } = true;

    private KnockbackComponent _knockbackComponent;
    private float _staggerTimer;

    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        _knockbackComponent = Owner.KnockbackComponent
            ?? Owner.GetNodeOrNull<KnockbackComponent>("CoreComponents/Knockback")
            ?? Owner.GetNodeOrNull<KnockbackComponent>("Knockback");

        if (ExitPolicy == StaggerExitPolicy.WhenTimerEnds)
        {
            _staggerTimer = Mathf.Max(0f, StaggerDuration);
        }
        else
        {
            _staggerTimer = 0f;
        }

        if (ClearMoveIntentOnEnter)
        {
            Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
            Owner.SetBlackboardValue(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        }

        if (_knockbackComponent != null)
        {
            Vector2 hitSource = Owner.GetBlackboardVector(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
            bool hasSource = !float.IsNaN(hitSource.X) && !float.IsNaN(hitSource.Y);
            if (hasSource)
            {
                _knockbackComponent.ApplyKnockback(hitSource);
            }
        }
    }

    public override void Update(double delta)
    {
        if (Owner == null || StateMachine == null)
        {
            return;
        }

        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            StateMachine.ChangeStateByType<DeadState>();
            return;
        }

        if (ExitPolicy == StaggerExitPolicy.WhenTimerEnds)
        {
            _staggerTimer -= (float)delta;
            if (_staggerTimer > 0f)
            {
                return;
            }
        }
        else if (_knockbackComponent != null && _knockbackComponent.IsKnockbackActive)
        {
            return;
        }

        Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);

        if (moveDir.LengthSquared() > 0.01f || inputVector.LengthSquared() > 0.01f)
        {
            StateMachine.ChangeStateByType<MoveState>();
        }
        else
        {
            StateMachine.ChangeStateByType<IdleState>();
        }
    }
}
