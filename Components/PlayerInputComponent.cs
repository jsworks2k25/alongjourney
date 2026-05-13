namespace AlongJourney.Components;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Entities.Characters.Player;
using AlongJourney.Core;

/// <summary>
/// 玩家输入组件：负责读取 Input 并写入黑板
/// 解耦玩家输入逻辑，让 MovementComponent 不依赖 Input
/// </summary>
public partial class PlayerInputComponent : BaseComponent
{
    public override void _Ready()
    {
        base._Ready();
        // 确保物理处理被启用
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null)
        {
            GD.PushError($"{Name}: PlayerInputComponent Owner is null!");
            return;
        }
        
        if (!Owner.IsAlive)
        {
            return;
        }

        Player player = Owner as Player;
        if (player != null && !player.CanHandleLocalInput())
        {
            return;
        }

        PlayerInputIntent intent = ReadLocalIntent(player);
        
        // 同时写入两个键以保持兼容性
        Owner.SetBlackboardValue(Actor.BlackboardKeys.InputVector, intent.MoveDirection);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, intent.MoveDirection);
    }

    private static PlayerInputIntent ReadLocalIntent(Player player)
    {
        // Current project has one keyboard/mouse input map. The player parameter
        // keeps this boundary ready for per-device or peer-routed input sources.
        Vector2 moveDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        return new PlayerInputIntent(moveDirection);
    }
}
