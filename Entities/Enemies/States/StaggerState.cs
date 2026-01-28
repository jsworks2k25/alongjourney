namespace AlongJourney.Entities.Enemies.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Components;

public partial class StaggerState : State
{
    private KnockbackComponent _knockbackComponent;

    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        _knockbackComponent = Owner.KnockbackComponent;
        
        // 通知 KnockbackComponent 应用击退效果
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

        // 检查是否死亡
        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            StateMachine.ChangeStateByType<DeadState>();
            return;
        }

        if (_knockbackComponent == null || !_knockbackComponent.IsKnockbackActive)
        {
            Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
            Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);

            if (moveDir.LengthSquared() > 0.01f || inputVector.LengthSquared() > 0.01f)
            {
                StateMachine.ChangeStateByType<ChaseState>();
            }
            else
            {
                StateMachine.ChangeStateByType<IdleState>();
            }
        }
    }
}
