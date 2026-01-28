namespace AlongJourney.Components;

using Godot;

/// <summary>
/// 击退组件：处理受击时的击退效果和击退速度衰减
/// </summary>
public partial class KnockbackComponent : BaseComponent
{
    [ExportGroup("Knockback Settings")]
    [Export] public float KnockbackForce = 50f;
    [Export] public float KnockbackFriction = 600f;
    [Export] public float StopThreshold = 5f;

    private Vector2 _knockbackVelocity = Vector2.Zero;

    public Vector2 KnockbackVelocity => _knockbackVelocity;
    public bool IsKnockbackActive => _knockbackVelocity.Length() > StopThreshold;

    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null || !Owner.IsAlive)
        {
            return;
        }
        if (!IsKnockbackActive)
        {
            _knockbackVelocity = Vector2.Zero;
            return;
        }

        _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * (float)delta);
        Owner.Velocity = _knockbackVelocity;
        Owner.ApplyMovement();
    }

    public void ApplyKnockback(Vector2 sourcePosition)
    {
        Vector2 knockbackDir = (Owner.GlobalPosition - sourcePosition).Normalized();
        _knockbackVelocity = knockbackDir * KnockbackForce;
    }

    /// <summary>
    /// 手动设置击退速度（用于外部控制）
    /// </summary>
    public void SetKnockbackVelocity(Vector2 velocity)
    {
        _knockbackVelocity = velocity;
    }

}
