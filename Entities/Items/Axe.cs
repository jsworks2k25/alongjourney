namespace AlongJourney.Entities.Items;

using Godot;

/// <summary>
/// 斧头：近战武器，可以砍树
/// </summary>
public partial class Axe : Weapon
{
    [Export] private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        base._Ready();
        AttackRange = AttackRangeType.Melee;
        if (MeleeRange <= 0)
        {
            MeleeRange = 50f;
        }

        if (_animPlayer == null)
            _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // 动画结束时，告诉玩家解锁 Attack 状态
        const string SwingAnim = "swing";
        _animPlayer.AnimationFinished += (animName) => {
            if (animName == SwingAnim)
            {
                _isAttacking = false;
                StartCooldown();
                EmitSignal(SignalName.AttackFinished);
            }
        };
    }

    public override bool Attack(Vector2 targetDirection)
    {
        if (_isAttacking || _isOnCooldown) return false;

        _isAttacking = true;

        if (_hitbox != null)
        {
            _hitbox.DamageAmount = Damage;
        }

        const string SwingAnim = "swing";
        _animPlayer.Play(SwingAnim);
        return true;
    }
}