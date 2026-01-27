using Godot;

/// <summary>
/// 受击状态：Actor 受击时的状态
/// 由 KnockbackComponent 控制速度，击退结束后转换回 Idle/Run
/// </summary>
public partial class StaggerState : State
{
    private KnockbackComponent _knockbackComponent;

    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        // 查找 KnockbackComponent（在 Enter 时查找，确保组件已初始化）
        _knockbackComponent = Owner.GetNodeOrNull<KnockbackComponent>("CoreComponents/Knockback")
            ?? Owner.GetNodeOrNull<KnockbackComponent>("Knockback");
        
        if (_knockbackComponent != null)
        {
            _knockbackComponent.OnStaggerEnded += OnStaggerEnded;
        }

        // 清除移动输入
        Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        
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
        // 注意：不在这里清除 KeyHitPending，让 HitEffectComponent 自己处理
        // HitEffectComponent 会在处理完受击效果后清除该标志
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

        // 击退结束由 KnockbackComponent 通过 OnStaggerEnded 事件通知
        // 这里不需要主动检查
    }

    private void OnStaggerEnded()
    {
        if (StateMachine == null || Owner == null)
        {
            return;
        }

        // 击退结束，根据是否有移动输入决定转换到哪个状态
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
    }

    public override void Exit()
    {
        if (_knockbackComponent != null)
        {
            _knockbackComponent.OnStaggerEnded -= OnStaggerEnded;
        }
    }
}
