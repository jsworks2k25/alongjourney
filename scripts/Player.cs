using Godot;

public partial class Player : CharacterBody2D, ITargetable
{
    [Signal]
    public delegate void PlayerDiedEventHandler(Player player);

    // --- 1. 组件引用 ---
    [ExportGroup("Components")]
    [Export] private Sprite2D _bodySprite;        // 玩家身体
    [Export] private HealthComponent _healthComponent;
    [Export] private HurtboxComponent _hurtboxComponent;
    [Export] private AnimationController _animationController;
    [Export] private HitEffectComponent _hitEffectComponent;
    
    [ExportGroup("Weapon System")]
    [Export] private Marker2D _weaponHolder; // 场景中一定要有一个 Marker2D 作为手部挂点
    private Weapon _currentWeapon;         // 当前持有的武器实例

    // --- 2. 基础属性 ---
    [Export] public float Speed = 100.0f;
    [Export] private MovementSmoothingComponent _movementSmoothing;
    
    // --- 3. 状态机 ---
    private enum PlayerState { Normal, Attack, Stagger, Dead }
    private PlayerState _currentState = PlayerState.Normal;

    private Vector2 _isoVec;
    private bool _isFacingUp = false;

    private CollisionShape2D _collisionShape;

    // ITargetable 实现
    public bool IsAlive => _currentState != PlayerState.Dead;

    public override void _Ready()
    {
        // 添加到 Player 组
        AddToGroup(GetPlayerGroupName());

        // 从 GameConfig 获取等距向量
        _isoVec = GameConfig.Instance != null ? GameConfig.Instance.IsometricVector : new Vector2(1f, 0.5f);

        // 自动查找组件
        if (_animationController == null)
            _animationController = GetNodeOrNull<AnimationController>("AnimationController");
        
        if (_hitEffectComponent == null)
            _hitEffectComponent = GetNodeOrNull<HitEffectComponent>("HitEffectComponent");

        if (_hurtboxComponent == null)
            _hurtboxComponent = GetNodeOrNull<HurtboxComponent>("HurtboxComponent");

        // 查找碰撞体
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_healthComponent != null)
        {
            _healthComponent.Died += OnDied;
            _healthComponent.HealthChanged += OnHurt;
        }

        if (_hitEffectComponent != null)
        {
            _hitEffectComponent.OnStaggerEnded += OnStaggerEnded;
        }

        if (_movementSmoothing == null)
            _movementSmoothing = GetNodeOrNull<MovementSmoothingComponent>("MovementSmoothing");

        if (_weaponHolder != null && _weaponHolder.GetChildCount() > 0)
        {
            _currentWeapon = _weaponHolder.GetChild<Weapon>(0);
            _currentWeapon.AttackFinished += OnWeaponAttackFinished;
        }
    }

    private string GetPlayerGroupName()
    {
        return GameConfig.Instance != null ? GameConfig.Instance.PlayerGroupName : "Player";
    }

    public override void _PhysicsProcess(double delta)
    {
        // 死亡时不处理物理
        if (!IsAlive)
        {
            return;
        }

        // 始终让武器座标跟随鼠标旋转（即使在移动中）
        // 如果想让攻击时锁定朝向，可以把这行放到 HandleMovementState 里
        HandleWeaponAiming();

        switch (_currentState)
        {
            case PlayerState.Normal:
                HandleMovementState(delta);
                break;
            
            case PlayerState.Attack:
                // 攻击中通常减速或定身
                float friction = GameConfig.Instance != null ? GameConfig.Instance.KnockbackFriction : 600f;
                Velocity = Velocity.MoveToward(Vector2.Zero, friction * (float)delta);
                break;

            case PlayerState.Stagger:
                // 击退逻辑由 HitEffectComponent 处理
                break;
        }

        MoveAndSlide();
        UpdateBodyAnimation(); 
    }

    private void OnStaggerEnded()
    {
        if (_currentState == PlayerState.Stagger)
        {
            _currentState = PlayerState.Normal;
        }
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
        if (!IsAlive || _weaponHolder == null) return;
        // 获取鼠标方向
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        
        _weaponHolder.Rotation = mouseDir.Angle();

        // 鼠标在左：翻转 Y 轴；鼠标在右：正常
        _weaponHolder.Scale = new Vector2(1, mouseDir.X < 0 ? -1 : 1);
    }

    private void HandleMovementState(double delta)
    {
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Vector2 targetVelocity = input * Speed * _isoVec;
        float deltaSeconds = (float)delta;

        if (_movementSmoothing != null)
        {
            Velocity = input.Length() > 0
                ? _movementSmoothing.Accelerate(Velocity, targetVelocity, deltaSeconds)
                : _movementSmoothing.Brake(Velocity, deltaSeconds);
        }
        else
        {
            Velocity = targetVelocity;
        }

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
        if (IsAlive)
            _currentState = PlayerState.Normal;
    }
    
    private void UpdateBodyAnimation()
    {
        if (!IsAlive) return;
        
        if (_animationController != null)
        {
            _animationController.UpdateAnimation(Velocity);
        }
    }
    // 如果是 HealthComponent 传来的信号，调用受击反馈组件
    private void OnHurt(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (!IsAlive) return;

        // 检查是否有有效的攻击者位置（不是NaN）
        if (!float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y))
        {
            // 进入击退状态
            _currentState = PlayerState.Stagger;
            
            // 使用受击反馈组件处理效果
            if (_hitEffectComponent != null)
            {
                _hitEffectComponent.PlayHitEffect(sourcePosition);
            }
        }
        else
        {
            // 如果没有攻击者位置，只做简单的闪白效果
            if (_hitEffectComponent != null)
            {
                _hitEffectComponent.PlayHitEffect(null);
            }
        }
    }

    private void SetCollisionEnabled(bool enabled)
    {
        if (_collisionShape != null)
        {
            _collisionShape.SetDeferred("disabled", !enabled);
        }
    }

    private void SetHurtboxEnabled(bool enabled)
    {
        if (_hurtboxComponent != null)
        {
            _hurtboxComponent.SetDeferred("monitoring", enabled);
            _hurtboxComponent.SetDeferred("monitorable", enabled);
        }
    }

    // --- 死亡逻辑 (Feature 4) ---
    private void OnDied()
    {
        if (_currentState == PlayerState.Dead) return;

        Velocity = Vector2.Zero;
        _currentState = PlayerState.Dead;

        _animationController?.PlayDeathAnimation();
        SetCollisionEnabled(false);
        SetHurtboxEnabled(false);

        EmitSignal(SignalName.PlayerDied, this);
    }
}