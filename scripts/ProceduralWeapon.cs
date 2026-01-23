using Godot;

public partial class ProceduralWeapon : Weapon
{
    [ExportGroup("Visuals")]
    [Export] private Sprite2D _weaponSprite;
    [Export] private float _swingAngle = 120f; // 挥砍扇形角度
    [Export] private float _windupTime = 0.1f; // 举起武器的时间（前摇）
    [Export] private float _swingTime = 0.15f; // 挥下去的时间（攻击判定）
    [Export] private float _recoverTime = 0.2f; // 收招时间（后摇）

    public override void _Ready()
    {
        base._Ready(); // 调用基类的 _Ready，确保 _hitbox 被初始化
        
        // 如果没有手动指定，尝试自动查找武器 Sprite
        if (_weaponSprite == null)
        {
            _weaponSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (_weaponSprite == null)
            {
                GD.PrintErr($"{Name}: 找不到武器 Sprite2D！请确保子节点中有名为 'Sprite2D' 的节点。");
            }
        }
        
        if (_hitbox != null)
        {
            _hitbox.DamageAmount = Damage;
            _hitbox.Monitoring = false; // 确保初始关闭
        }
        else
        {
            GD.PrintErr($"{Name}: Hitbox 为空，无法设置伤害值！");
        }
    }

    public override void Attack(Vector2 targetDirection)
    {
        if (_isAttacking || _isOnCooldown)
        {
            GD.Print($"{Name}: 攻击被阻止 - 正在攻击: {_isAttacking}, 冷却中: {_isOnCooldown}");
            return;
        }

        if (_weaponSprite == null)
        {
            GD.PrintErr($"{Name}: 无法攻击 - _weaponSprite 为空！");
            return;
        }

        _isAttacking = true;
        GD.Print($"{Name}: 开始攻击，方向: {targetDirection}");

        // 1. 设置初始朝向（武器始终朝右，作为 0 度基准）
        Rotation = targetDirection.Angle();

        // 创建 Tween 序列
        Tween tween = CreateTween();
        
        // --- 阶段 A: 前摇 (Windup) ---
        // 向后旋转 (负角度)，并稍微放大一点提示玩家
        float startAngle = Mathf.DegToRad(-_swingAngle / 2);
        float endAngle = Mathf.DegToRad(_swingAngle / 2);

        // 初始状态
        _weaponSprite.Rotation = startAngle;
        
        // 动画：向后蓄力
        tween.TweenProperty(_weaponSprite, "rotation", startAngle - 0.2f, _windupTime)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
             
        // --- 阶段 B: 挥砍 (Swing) ---
        // 开启 Hitbox
        tween.TweenCallback(Callable.From(() => {
            if (_hitbox != null) _hitbox.Monitoring = true;
        }));

        // 动画：快速挥向前方
        // 并行 Tween：同时改变旋转和缩放（拉伸产生速度感）
        tween.SetParallel(true);
        tween.TweenProperty(_weaponSprite, "rotation", endAngle, _swingTime)
             .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_weaponSprite, "scale", new Vector2(1.3f, 0.8f), _swingTime * 0.5f); // 挥砍时拉长
        tween.SetParallel(false); // 结束并行

        // --- 阶段 C: 收招 (Recover) ---
        // 关闭 Hitbox
        tween.TweenCallback(Callable.From(() => {
            if (_hitbox != null) _hitbox.Monitoring = false;
        }));

        // 动画：慢慢回正并恢复缩放
        tween.SetParallel(true);
        tween.TweenProperty(_weaponSprite, "rotation", 0f, _recoverTime)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(_weaponSprite, "scale", Vector2.One, _recoverTime);
        tween.SetParallel(false);

        // --- 结束 ---
        tween.TweenCallback(Callable.From(() => {
            _isAttacking = false;
            StartCooldown();
            EmitSignal(SignalName.AttackFinished); // 关键：通知 Player 解锁状态
        }));
    }
}