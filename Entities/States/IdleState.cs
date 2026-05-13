namespace AlongJourney.Entities.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Entities;

public partial class IdleState : State
{
    public override void Enter()
    {
        if (Owner != null)
        {
            Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
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

        Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);

        if (moveDir.LengthSquared() > 0.01f || inputVector.LengthSquared() > 0.01f)
        {
            StateMachine.ChangeStateByType<MoveState>();
            return;
        }

        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.IsAttacking, false))
        {
            StateMachine.ChangeStateByType<AttackState>();
            return;
        }

        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.HitPending, false))
        {
            StateMachine.ChangeStateByType<StaggerState>();
            return;
        }
    }
}
