namespace AlongJourney.Components;

using Godot;
using AlongJourney.Interfaces;

public partial class HitboxComponent : Area2D
{
    [Export] public int DamageAmount = 10;

    public override void _Ready()
    {
        // 监听进入区域的物体
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        // 如果碰到的东西是 IDamageable (也就是 HurtboxComponent)
        if (area is IDamageable target)
        {
            // 传递攻击者的位置（HitboxComponent的全局位置）
            target.TakeDamage(DamageAmount, GlobalPosition);
        }
    }
}