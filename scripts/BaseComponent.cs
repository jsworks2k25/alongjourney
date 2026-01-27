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
        // 向上查找 Actor 父节点（可能通过 CoreComponents 等中间节点）
        Owner = GetParent() as Actor;
        if (Owner == null)
        {
            // 如果直接父节点不是 Actor，向上递归查找
            Node current = GetParent();
            while (current != null && Owner == null)
            {
                if (current is Actor actor)
                {
                    Owner = actor;
                    break;
                }
                current = current.GetParent();
            }
        }
        
        if (Owner == null)
        {
            GD.PushError($"{Name}: BaseComponent requires an Actor parent. Path: {GetPath()}");
            return;
        }

        // 确保组件可以处理物理更新（Node 默认不会调用 _PhysicsProcess）
        ProcessMode = ProcessModeEnum.Inherit;

        Owner.StateChanged += OnOwnerStateChanged;
        Owner.BlackboardChanged += OnOwnerBlackboardChanged;
        Initialize();
    }

    public virtual void Initialize()
    {
    }

    protected virtual void OnOwnerStateChanged(string newStateName)
    {
    }

    protected virtual void OnOwnerBlackboardChanged(string key, Variant value)
    {
    }
}
