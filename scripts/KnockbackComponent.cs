using Godot;

/// <summary>
/// 击退组件：处理受击时的击退效果和击退速度衰减
/// 负责在 Stagger 状态下控制 Actor 的击退速度
/// </summary>
public partial class KnockbackComponent : BaseComponent
{
    [ExportGroup("Knockback Settings")]
    [Export] public float KnockbackForce = 50f;
    [Export] public float KnockbackFriction = 600f;
    [Export] public float StaggerThreshold = 10f;

    private Vector2 _knockbackVelocity = Vector2.Zero;
    private bool _isStaggered = false;

    [Signal]
    public delegate void StaggerEndedEventHandler();

    public event StaggerEndedEventHandler OnStaggerEnded;

    public override void Initialize()
    {
        // 击退配置现在通过 Export 属性在 Inspector 中配置
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null || !Owner.IsAlive)
        {
            return;
        }

        // 只在 Stagger 状态下处理击退
        if (_isStaggered && Owner.IsInState<StaggerState>())
        {
            // 应用击退摩擦力
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * (float)delta);
            
            // 直接设置 Actor 的速度（组件负责物理计算）
            Owner.Velocity = _knockbackVelocity;
            Owner.ApplyMovement();

            // 如果速度足够小，恢复正常
            if (_knockbackVelocity.Length() < StaggerThreshold)
            {
                _isStaggered = false;
                _knockbackVelocity = Vector2.Zero;
                Owner.Velocity = Vector2.Zero;
                OnStaggerEnded?.Invoke();
            }
        }
    }

    /// <summary>
    /// 应用击退效果
    /// </summary>
    /// <param name="sourcePosition">受击源位置</param>
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        if (Owner == null)
        {
            return;
        }

        Vector2 knockbackDir = (Owner.GlobalPosition - sourcePosition).Normalized();
        _knockbackVelocity = knockbackDir * KnockbackForce;
        _isStaggered = true;
    }

    /// <summary>
    /// 手动设置击退速度（用于外部控制）
    /// </summary>
    public void SetKnockbackVelocity(Vector2 velocity)
    {
        _knockbackVelocity = velocity;
        _isStaggered = velocity.Length() > StaggerThreshold;
    }

    /// <summary>
    /// 检查是否处于击退状态
    /// </summary>
    public bool IsStaggered => _isStaggered;

    /// <summary>
    /// 重置击退状态（用于重生等场景）
    /// </summary>
    public void Reset()
    {
        _isStaggered = false;
        _knockbackVelocity = Vector2.Zero;
        
        if (Owner != null)
        {
            Owner.Velocity = Vector2.Zero;
        }
    }
}
