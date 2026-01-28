namespace AlongJourney.Core;

using Godot;
using AlongJourney.Entities;

/// <summary>
/// 状态基类：所有 Actor 状态的基类
/// 每个状态负责读取输入/AI，调用 Actor 的组件方法，并决定何时转换状态
/// </summary>
public abstract partial class State : Node
{
    protected new Actor Owner;
    protected StateMachine StateMachine;

    public override void _Ready()
    {
        // 查找父节点中的 Actor 和 StateMachine
        // State 是 StateMachine 的子节点，StateMachine 是 Actor 的子节点
        StateMachine = GetParent<StateMachine>();
        Owner = StateMachine?.GetParent<Actor>();
        if (Owner == null)
        {
            GD.PushError($"{Name}: State requires an Actor parent.");
            return;
        }
        if (StateMachine == null)
        {
            GD.PushError($"{Name}: State requires a StateMachine parent.");
            return;
        }
    }

    /// 进入状态时调用
    public virtual void Enter(){}

    /// 每帧更新（在 StateMachine._PhysicsProcess 中调用）
    public virtual void Update(double delta){}

    /// 退出状态时调用
    public virtual void Exit(){}
}
