using Godot;

/// <summary>
/// 空闲状态：Actor 静止时的状态
/// 检查是否需要转换到 Run 或 Attack 状态
/// </summary>
public partial class IdleState : State
{
    public override void Enter()
    {
        // 停止移动
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

        // 检查是否死亡
        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            StateMachine.ChangeStateByType<DeadState>();
            return;
        }

        // 检查是否有移动输入
        Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        
        if (moveDir.LengthSquared() > 0.01f || inputVector.LengthSquared() > 0.01f)
        {
            // 有移动输入，转换到 Run 状态
            StateMachine.ChangeStateByType<RunState>();
            return;
        }

        // 检查是否正在攻击
        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.IsAttacking, false))
        {
            StateMachine.ChangeStateByType<AttackState>();
            return;
        }

        // 检查是否有待处理的伤害（需要进入 Stagger 状态）
        if (Owner.GetBlackboardBool(Actor.BlackboardKeys.HitPending, false))
        {
            StateMachine.ChangeStateByType<StaggerState>();
            return;
        }
    }
}
