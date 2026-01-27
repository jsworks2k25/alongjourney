using Godot;

/// <summary>
/// 武器攻击范围类型
/// </summary>
public enum AttackRangeType
{
    Melee,   // 近战
    Ranged   // 远程
}

public abstract partial class Weapon : Node2D
{
    [Signal] public delegate void AttackFinishedEventHandler();

    [Export] public int Damage = 10;
    [Export] public float Cooldown = 0.2f;

    [ExportGroup("Attack Range")]
    [Export] public AttackRangeType AttackRange = AttackRangeType.Melee;
    [Export] public float MeleeRange = 50f; // 近战攻击范围（像素）

    [Export] protected HitboxComponent _hitbox;

    protected bool _isAttacking = false;
    protected bool _isOnCooldown = false;

    public override void _Ready()
    {
        if (_hitbox == null)
            _hitbox = GetNodeOrNull<HitboxComponent>("Hitbox");

        if (_hitbox != null)
            _hitbox.DamageAmount = Damage;
    }

    /// <summary>
    /// 攻击指定方向（原有方法，保持向后兼容）
    /// </summary>
    public abstract bool Attack(Vector2 targetDirection);

    /// <summary>
    /// 攻击指定目标（新方法，支持选中目标攻击）
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <param name="attackerPosition">攻击者位置</param>
    /// <returns>是否成功发起攻击</returns>
    public virtual bool AttackTarget(IInteractable target, Vector2 attackerPosition)
    {
        if (_isAttacking || _isOnCooldown)
        {
            return false;
        }

        if (!target.CanInteractWith(this))
        {
            return false;
        }

        Vector2 targetPos = target.GetInteractionPosition();
        float distance = attackerPosition.DistanceTo(targetPos);

        if (AttackRange == AttackRangeType.Melee && distance > MeleeRange)
        {
            return false;
        }

        Vector2 direction = (targetPos - attackerPosition).Normalized();

        ApplyDirectDamage(target, attackerPosition);

        if (Attack(direction))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 对目标直接造成伤害（用于选中目标攻击）
    /// </summary>
    protected virtual void ApplyDirectDamage(IInteractable target, Vector2 attackerPosition)
    {
        if (target is IDamageable damageable)
        {
            damageable.TakeDamage(Damage, attackerPosition);
        }
        else
        {
            GD.PrintErr($"[Weapon.ApplyDirectDamage] 目标 {target.GetType().Name} 不是 IDamageable！");
        }
    }

    /// <summary>
    /// 检查是否在攻击范围内
    /// </summary>
    public bool IsInRange(Vector2 attackerPosition, Vector2 targetPosition)
    {
        float distance = attackerPosition.DistanceTo(targetPosition);
        
        if (AttackRange == AttackRangeType.Melee)
        {
            return distance <= MeleeRange;
        }
        else
        {
            return true;
        }
    }

    protected void StartCooldown()
    {
        _isOnCooldown = true;
        GetTree().CreateTimer(Cooldown).Timeout += () => _isOnCooldown = false;
    }
}