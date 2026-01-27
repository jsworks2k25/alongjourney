using Godot;

/// <summary>
/// 跑步状态：Actor 移动时的状态
/// 读取移动输入，调用 MovementComponent，检查是否需要转换状态
/// </summary>
public partial class RunState : State
{
    public override void Update(double delta)
    {
        if (Owner == null || StateMachine == null)
        {
            return;
        }

        // 检查移动输入（由 PlayerInputComponent 或 AI 写入黑板）
        Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Vector2 inputVector = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        
        // 如果没有移动输入，转换到 Idle 状态
        if (moveDir.LengthSquared() < 0.01f && inputVector.LengthSquared() < 0.01f)
        {
            StateMachine.ChangeStateByType<IdleState>();
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
