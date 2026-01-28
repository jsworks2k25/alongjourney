namespace AlongJourney.Entities.Player.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Components;

/// <summary>
/// 受击状态：Actor 受击时的状态
/// 僵直时间由本状态控制，击退力由 KnockbackComponent 提供
/// </summary>
public partial class StaggerState : State
{
    [Export] public float StaggerDuration = 0.25f;

    private KnockbackComponent _knockbackComponent;
    private float _staggerTimer;

    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        // 查找 KnockbackComponent（在 Enter 时查找，确保组件已初始化）
        _knockbackComponent = Owner.GetNodeOrNull<KnockbackComponent>("CoreComponents/Knockback")
            ?? Owner.GetNodeOrNull<KnockbackComponent>("Knockback");

        _staggerTimer = Mathf.Max(0f, StaggerDuration);
        
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

        _staggerTimer -= (float)delta;
        if (_staggerTimer <= 0f)
        {
            // 僵直结束，根据是否有移动输入决定转换到哪个状态
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
    }
}
