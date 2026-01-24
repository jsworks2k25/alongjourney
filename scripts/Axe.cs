using Godot;
using System;

public partial class Axe : Weapon
{
    [Export] private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        base._Ready(); // 执行基类的 Hitbox 查找逻辑

        if (_animPlayer == null)
            _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // 动画结束时，告诉玩家解锁 Attack 状态
        _animPlayer.AnimationFinished += (animName) => {
            GD.Print("动画结束: " + animName);
            if (animName == "swing")
            {
                _isAttacking = false;
                StartCooldown(); // 开启冷却计时
                EmitSignal(SignalName.AttackFinished);
            }
        };
    }

    public override bool Attack(Vector2 targetDirection)
    {
        // 1. 基础检查（冷却中或正在攻击则返回）
        if (_isAttacking || _isOnCooldown) return false;

        // 2. 进入攻击状态
        _isAttacking = true;

        // 3. 将当前武器的伤害同步给 Hitbox 组件
        if (_hitbox != null)
        {
            _hitbox.DamageAmount = Damage;
            // 注意：Hitbox 的 Monitoring 应该由 AnimationPlayer 的轨道控制开关
        }

        // 4. 播放动画
        // 既然动画是统一朝右画的，Player 已经旋转了父节点，这里直接播即可
        _animPlayer.Play("swing");
        return true;
    }
}