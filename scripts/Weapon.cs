using Godot;
using System;

// 这是一个抽象基类，所有具体的武器（斧头、剑、枪）都继承它
public abstract partial class Weapon : Node2D
{
    [Signal] public delegate void AttackFinishedEventHandler();

    [Export] public int Damage = 10;
    [Export] public float Cooldown = 0.4f;

    // 可以在编辑器里拖拽 Hitbox 组件
    [Export] protected HitboxComponent _hitbox;

    protected bool _isAttacking = false;
    protected bool _isOnCooldown = false;

    public override void _Ready()
    {
        // 如果没有手动指定，尝试自动查找 Hitbox
        if (_hitbox == null)
        {
            _hitbox = GetNodeOrNull<HitboxComponent>("Hitbox");
            if (_hitbox == null)
            {
                GD.PrintErr($"{Name}: 找不到 Hitbox 组件！请确保子节点中有名为 'Hitbox' 的 HitboxComponent。");
            }
        }
    }

    // 子类必须重写这个方法
    public abstract void Attack(Vector2 targetDirection);

    // 辅助工具：开启冷却
    protected void StartCooldown()
    {
        _isOnCooldown = true;
        GetTree().CreateTimer(Cooldown).Timeout += () => _isOnCooldown = false;
    }
}