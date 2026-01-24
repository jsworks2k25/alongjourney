using Godot;
using System;

// 这是一个抽象基类，所有具体的武器（斧头、剑、枪）都继承它
public abstract partial class Weapon : Node2D
{
    [Signal] public delegate void AttackFinishedEventHandler();

    [Export] public int Damage = 10;
    [Export] public float Cooldown = 0.2f;

    [Export] protected HitboxComponent _hitbox;

    protected bool _isAttacking = false;
    protected bool _isOnCooldown = false;

    public override void _Ready()
    {
        if (_hitbox == null)
            _hitbox = GetNodeOrNull<HitboxComponent>("Hitbox");

        // [改进] 初始化时就同步伤害
        if (_hitbox != null)
            _hitbox.DamageAmount = Damage;
    }

    public abstract bool Attack(Vector2 targetDirection);

    protected void StartCooldown()
    {
        _isOnCooldown = true;
        // 使用 Timer 的一种更现代的写法
        GetTree().CreateTimer(Cooldown).Timeout += () => _isOnCooldown = false;
    }
}