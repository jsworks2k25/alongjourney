using Godot;

/// <summary>
/// 可被作为目标的接口，用于解耦敌人对玩家的直接依赖
/// </summary>
public interface ITargetable
{
    Vector2 GlobalPosition { get; }
    bool IsAlive { get; }
}

