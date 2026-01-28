namespace AlongJourney.Entities.Player.States;

using Godot;
using AlongJourney.Core;

/// <summary>
/// 攻击状态：Actor 攻击时的状态
/// 在攻击期间保持此状态，攻击结束后转换回 Idle/Run
/// </summary>
public partial class AttackState : State
{
    public override void Enter()
    {
        if (Owner != null)
        {
            Owner.SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, true);
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

        // 检查是否有待处理的伤害（需要进入 Stagger 状态）
        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.HitPending, false))
        {
            StateMachine.ChangeStateByType<StaggerState>();
            return;
        }

        // 检查攻击是否结束
        if (!Owner.GetBlackboardBool(Actor.BlackboardKeys.IsAttacking, false))
        {
            // 攻击结束，根据是否有移动输入决定转换到哪个状态
            Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
            Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);
            
            if (moveDir.LengthSquared() > 0.01f || inputVector.LengthSquared() > 0.01f)
            {
                StateMachine.ChangeStateByType<RunState>();
            }
            else
            {
                StateMachine.ChangeStateByType<IdleState>();
            }
            return;
        }
    }

    public override void Exit()
    {
        if (Owner != null)
        {
            Owner.SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, false);
        }
    }
}
