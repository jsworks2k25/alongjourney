using Godot;
using System;

public partial class Ghost : CharacterBody2D
{
    // --- 配置 ---
    [Export] public float Speed = 40f;
    [Export] public float Acceleration = 200f; // [新增] 加速度：数值越小，启动越慢（漂移感越强）
    [Export] public float Friction = 150f;     // [新增] 摩擦力：数值越小，刹车越慢
    
    [Export] public int DamagePerTick = 10;
    [Export] public float DamageInterval = 1.0f;

    private Vector2 _isoVec = new Vector2(1f, 0.5f);

    // --- 组件引用 ---
    [Export] private AnimationPlayer _animPlayer;
    [Export] private Area2D _detectionArea;
    [Export] private HitboxComponent _hitbox;

    // --- 状态机 ---
    private enum GhostState { Idle, Chase }
    private GhostState _currentState = GhostState.Idle;

    private Node2D _target = null;
    private float _damageTimer = 0f;

    public override void _Ready()
    {
        _detectionArea.BodyEntered += OnBodyEnteredDetection;
        _detectionArea.BodyExited += OnBodyExitedDetection;
        _animPlayer.Play("move_front");
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (_currentState)
        {
            case GhostState.Idle:
                // [新增] 惯性刹车：如果没有目标，就慢慢停下来，而不是瞬间静止
                ApplyFriction(delta);
                break;

            case GhostState.Chase:
                if (_target != null)
                {
                    ChaseTarget(delta);
                }
                else 
                {
                    // 目标丢失（比如玩家死了），转为刹车
                    ApplyFriction(delta);
                }
                break;
        }

        MoveAndSlide();
        UpdateAnimation();
        ProcessContactDamage(delta);
    }

    // --- 核心移动逻辑改动 ---

    private void ChaseTarget(double delta)
    {
        Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
        
        // [修改] 不再直接赋值 Velocity = direction * Speed
        // 而是使用 MoveToward 平滑地从“当前速度”过渡到“目标速度”
        
        Velocity = Velocity.MoveToward(direction * Speed * _isoVec, Acceleration * (float)delta);
        // 简单的朝向翻转
        var sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.FlipH = Velocity.X > 0;
    }

    private void ApplyFriction(double delta)
    {
        // [新增] 逐渐将速度降为 0
        Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
    }

    // --- 动画优化 ---

    private void UpdateAnimation()
    {
        // 只有当速度超过一定程度才切换动画，防止原地轻微漂移时动画鬼畜
        if (Velocity.Length() > 5f) 
        {
            // 给 Y 轴加一点“死区”，防止在水平移动时因为微小的 Y 轴波动而切换前后动画
            if (Velocity.Y > 5f)
                _animPlayer.Play("move_front");
            else if (Velocity.Y < -5f)
                _animPlayer.Play("move_back");
        }
        else
        {
            // 如果速度很慢，可以选择暂停动画或者播放 Idle
            // _animPlayer.Pause(); 
        }
    }

    // --- 其他逻辑保持不变 ---
    private void ProcessContactDamage(double delta)
    {
        if (_hitbox.HasOverlappingAreas()) 
        {
            _damageTimer -= (float)delta;
            if (_damageTimer <= 0)
            {
                foreach (var area in _hitbox.GetOverlappingAreas())
                {
                    var victim = area.Owner as Node; 
                    if (area is IDamageable damageable)
                    {
                        // 传递攻击者的位置（Ghost的全局位置）
                        damageable.TakeDamage(DamagePerTick, GlobalPosition);
                        _damageTimer = DamageInterval; 
                    }
                }
            }
        }
        else
        {
            _damageTimer = 0; 
        }
    }

    private void OnBodyEnteredDetection(Node2D body)
    {
        if (body.Name == "Player") 
        {
            _target = body;
            _currentState = GhostState.Chase;
        }
    }

    private void OnBodyExitedDetection(Node2D body)
    {
        if (body == _target)
        {
            _target = null;
            _currentState = GhostState.Idle;
            // 注意：这里删除了 Velocity = Vector2.Zero，交给 ApplyFriction 处理
        }
    }
}