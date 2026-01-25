using Godot;

/// <summary>
/// 玩家输入组件：负责读取 Input 并写入黑板
/// 解耦玩家输入逻辑，让 MovementComponent 不依赖 Input
/// </summary>
public partial class PlayerInputComponent : BaseComponent
{
    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null || !Owner.IsAlive)
        {
            return;
        }

        // 读取输入并写入黑板
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        
        // 同时写入两个键以保持兼容性
        Owner.SetBlackboardValue(Actor.KeyInputVector, input);
        Owner.SetBlackboardValue(Actor.KeyMoveDirection, input);
    }
}
