using Godot;

/// <summary>
/// 状态机组件：管理 Actor 的状态转换
/// 作为 Actor 的子节点，负责调用当前状态的 Update 方法
/// </summary>
public partial class StateMachine : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(string newStateName);

    public State CurrentState { get; private set; }
    private Actor Owner;

    public override void _Ready()
    {
        Owner = GetParent<Actor>();
        if (Owner == null)
        {
            GD.PushError($"{Name}: StateMachine requires an Actor parent.");
            return;
        }

        // 自动查找并设置初始状态（第一个 State 子节点）
        foreach (Node child in GetChildren())
        {
            if (child is State state)
            {
                ChangeState(state);
                break;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (CurrentState != null && Owner != null)
        {
            // 即使死亡也要调用 Update，让 DeadState 可以处理逻辑
            // 但 DeadState 的 Update 是空的，所以不会有问题
            CurrentState.Update(delta);
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ChangeState(State newState)
    {
        if (newState == null)
        {
            GD.PushError("StateMachine: Cannot change to null state.");
            return;
        }

        if (CurrentState == newState)
        {
            return;
        }

        // 退出旧状态
        if (CurrentState != null)
        {
            CurrentState.Exit();
        }

        // 进入新状态
        var oldStateName = CurrentState?.Name ?? "None";
        CurrentState = newState;
        CurrentState.Enter();

        // 更新 Actor 的黑板
        if (Owner != null)
        {
            Owner.SetBlackboardValue(Actor.BlackboardKeys.State, CurrentState.Name);
            Owner.SetBlackboardValue(Actor.BlackboardKeys.IsDead, CurrentState is DeadState);
            Owner.SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, CurrentState is AttackState);
        }

        // 发送信号
        EmitSignal(SignalName.StateChanged, CurrentState.Name);
        
    }

    /// <summary>
    /// 通过名称查找并切换状态
    /// </summary>
    public void ChangeStateByName(string stateName)
    {
        foreach (Node child in GetChildren())
        {
            if (child is State state && child.Name == stateName)
            {
                ChangeState(state);
                return;
            }
        }
        
        GD.PushError($"StateMachine: State '{stateName}' not found.");
    }

    /// <summary>
    /// 通过类型查找并切换状态
    /// </summary>
    public void ChangeStateByType<T>() where T : State
    {
        foreach (Node child in GetChildren())
        {
            if (child is T state)
            {
                ChangeState(state);
                return;
            }
        }
        
        GD.PushError($"StateMachine: State of type '{typeof(T).Name}' not found.");
    }
}
