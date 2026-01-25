using Godot;

/// <summary>
/// 组件基类：所有 Actor 组件的基类
/// 自动查找 Actor 父节点并连接信号
/// </summary>
public partial class BaseComponent : Node
{
    protected Actor Owner;

    public override void _Ready()
    {
        Owner = ActorHelper.FindActorOwner(this);
        if (Owner == null)
        {
            GD.PushWarning($"{Name}: BaseComponent requires an Actor parent.");
            return;
        }

        Owner.StateChangedTyped += OnOwnerStateChanged;
        Owner.BlackboardChanged += OnOwnerBlackboardChanged;
        Initialize();
    }

    public virtual void Initialize()
    {
    }

    protected virtual void OnOwnerStateChanged(Actor.ActorState newState)
    {
    }

    protected virtual void OnOwnerBlackboardChanged(string key, Variant value)
    {
    }
}
