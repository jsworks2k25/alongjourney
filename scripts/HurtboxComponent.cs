using Godot;

/// <summary>
/// 受击区域组件：实现 IDamageable 接口，通过 Actor 黑板系统处理伤害
/// </summary>
public partial class HurtboxComponent : Area2D, IDamageable
{
    private Actor _owner;

    public override void _Ready()
    {
        _owner = ActorHelper.FindActorOwner(this);
    }

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {      
        _owner.RequestDamage(amount, sourcePosition);
    }
}