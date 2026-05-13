namespace AlongJourney.Entities.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Entities;

/// <summary>
/// Hit reaction: fixed-duration stagger. Movement intent stays on the blackboard (keyboard or AI);
/// <see cref="MovementComponent"/> does not apply locomotion while the state node is Stagger.
/// </summary>
public partial class StaggerState : State
{
    [Export] public float StaggerDuration { get; set; } = 0.25f;

    private float _staggerTimer;

    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        _staggerTimer = Mathf.Max(0f, StaggerDuration);
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

        _staggerTimer -= (float)delta;
        if (_staggerTimer > 0f)
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
