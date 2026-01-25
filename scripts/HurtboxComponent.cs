using Godot;

// 这是一个专门用来"挨打"的区域
public partial class HurtboxComponent : Area2D, IDamageable
{
    [Export]
    public HealthComponent HealthComponent; // 依赖注入：把第一步的血条组件拖进来

    private Actor _owner;

    public override void _Ready()
    {
        _owner = FindActorOwner();
    }

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {
        if (_owner != null)
        {
            _owner.RequestDamage(amount, sourcePosition);
            return;
        }

        if (HealthComponent != null)
        {
            HealthComponent.TakeDamage(amount, sourcePosition);
        }
    }

    private Actor FindActorOwner()
    {
        Node current = GetParent();
        while (current != null && !(current is Actor))
        {
            current = current.GetParent();
        }

        return current as Actor;
    }
}