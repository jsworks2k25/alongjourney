using Godot;
using System;

public partial class Player : CharacterBody2D
{
    // --- 1. 组件引用 ---
    [ExportGroup("Components")]
    [Export] private AnimationPlayer _animPlayer; // 只负责身体移动动画
    [Export] private Sprite2D _bodySprite;        // 玩家身体
    [Export] private HealthComponent _healthComponent;
    
    [ExportGroup("Weapon System")]
    [Export] private Marker2D _weaponHolder; // 场景中一定要有一个 Marker2D 作为手部挂点
    private Weapon _currentWeapon;         // 当前持有的武器实例

    // --- 2. 基础属性 ---
    [Export] public float Speed = 100.0f;
    [Export] public float Friction = 600.0f; 
    
    // --- 3. 状态机 ---
    private enum PlayerState { Normal, Attack, Stagger, Dead }
    private PlayerState _currentState = PlayerState.Normal;

    private Vector2 _isoVec = new Vector2(1f, 0.5f);
    private bool _isFacingUp = false;
    private Vector2 _knockbackVelocity = Vector2.Zero;

    public override void _Ready()
    {
        if (_healthComponent != null)
        {
            _healthComponent.Died += OnDied;
            _healthComponent.HealthChanged += OnHurt;
        }

        if (_weaponHolder != null && _weaponHolder.GetChildCount() > 0)
        {
            _currentWeapon = _weaponHolder.GetChild<Weapon>(0);
            _currentWeapon.AttackFinished += OnWeaponAttackFinished;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // 始终让武器座标跟随鼠标旋转（即使在移动中）
        // 如果想让攻击时锁定朝向，可以把这行放到 HandleMovementState 里
        HandleWeaponAiming();

        switch (_currentState)
        {
            case PlayerState.Normal:
                HandleMovementState();
                break;
            
            case PlayerState.Attack:
                // 攻击中通常减速或定身
                Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
                break;

            case PlayerState.Stagger:
                HandleStaggerState(delta);
                break;
        }

        MoveAndSlide();
        UpdateBodyAnimation(); 
    }
    private void EnterAttackState()
    {
        // 尝试让武器攻击
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        
        // 只有当武器不在冷却时才进入攻击状态
        // 这里只是为了状态同步，Weapon 内部也会检查冷却
        
        if (_currentWeapon.Attack(mouseDir))
        {
            _currentState = PlayerState.Attack;
        }
    }
    private void HandleWeaponAiming()
    {
        if (_currentState == PlayerState.Dead) return;
        // 获取鼠标方向
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        
        _weaponHolder.Rotation = mouseDir.Angle();

        if (mouseDir.X < 0)
        {
            // 鼠标在左：翻转 Y 轴
            _weaponHolder.Scale = new Vector2(1, -1); 
        }

    }

    private void HandleMovementState()
    {
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = input * Speed * _isoVec;

        if (input.Length() > 0)
        {
            if (input.Y != 0) _isFacingUp = input.Y < 0;
            if (input.X != 0) _bodySprite.FlipH = input.X < 0;
        }

        // 攻击输入
        if (Input.IsActionJustPressed("attack"))
        {
            if (_currentWeapon != null)
            {
                GD.Print("检测到攻击按键，尝试进入攻击状态...");
                EnterAttackState();
            }
        }
    }

    // 收到武器信号，解除锁定
    private void OnWeaponAttackFinished()
    {
        if (_currentState != PlayerState.Dead)
            _currentState = PlayerState.Normal;
    }
    
    // ... (HandleStaggerState, UpdateBodyAnimation, OnDied, OnHurt 保持不变) ...
    // 注意：UpdateBodyAnimation 只需要管 Idle/Run，不需要管 Attack 了
    private void UpdateBodyAnimation()
    {
        if (_currentState == PlayerState.Dead) return;
        
        string animName = Velocity.Length() > 0 
            ? (_isFacingUp ? "move_back" : "move_front") 
            : (_isFacingUp ? "idle_back" : "idle_front");

        if (_animPlayer.CurrentAnimation != animName)
            _animPlayer.Play(animName);
    }
    
    private void HandleStaggerState(double delta)
    {
        // 施加摩擦力，让击飞速度逐渐归零
        _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, Friction * (float)delta);
        Velocity = _knockbackVelocity;

        // 如果速度足够小，恢复正常状态
        if (_knockbackVelocity.Length() < 10f)
        {
            _currentState = PlayerState.Normal;
        }
    }
    // --- 受击反馈 (Feature 3) ---
    
    // 这个函数供外部调用，或者连接到 HealthComponent 的信号
    // 参数 sourcePosition: 攻击者的位置，用来计算击飞方向
    public void TakeHit(int damage, Vector2 sourcePosition)
    {
        // 1. 变红闪烁 (Tween)
        Tween tween = CreateTween();
        _bodySprite.Modulate = Colors.Red; // 先设置为红色
        tween.TweenProperty(_bodySprite, "modulate", Colors.White, 0.2f); // 然后变回白色

        // 2. 击飞 (Knockback)
        if (_currentState != PlayerState.Dead)
        {
            _currentState = PlayerState.Stagger;
            Vector2 knockbackDir = (GlobalPosition - sourcePosition).Normalized();
            _knockbackVelocity = knockbackDir * 50f; // 击飞力度
        }
    }
    
    // 如果是 HealthComponent 传来的信号，调用TakeHit来处理击退和变色
    private void OnHurt(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        // 检查是否有有效的攻击者位置（不是NaN）
        if (!float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y))
        {
            TakeHit(0, sourcePosition); // damage参数这里不需要，因为已经在HealthComponent中处理了
        }
        else
        {
            // 如果没有攻击者位置，只做简单的闪白效果
            Tween tween = CreateTween();
            _bodySprite.Modulate = new Color(10, 10, 10, 1); // HDR 高亮白
            tween.TweenProperty(_bodySprite, "modulate", Colors.White, 0.15f);
        }
    }

    // --- 死亡逻辑 (Feature 4) ---
    private void OnDied()
    {
        _currentState = PlayerState.Dead;
        Velocity = Vector2.Zero;
        
        // 播放死亡动画
        _animPlayer.Play("die");
        
        // 禁用碰撞，防止尸体挡路
        GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);
        
        // 游戏结束逻辑...
        GD.Print("Player Dead");
    }
}