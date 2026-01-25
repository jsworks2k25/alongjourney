using Godot;

/// <summary>
/// 受击反馈组件，统一处理闪烁、击退、抖动等效果
/// </summary>
public partial class HitEffectComponent : Node
{
    [Export] public bool EnableFlash = true;
    [Export] public bool EnableKnockback = false;
    [Export] public bool EnableShake = false;

    [ExportGroup("Flash Settings")]
    [Export] public Color FlashColor = Colors.Red;
    [Export] public float FlashDuration = 0.1f;

    [ExportGroup("Knockback Settings")]
    [Export] public float KnockbackForce = 50f;
    [Export] public float KnockbackFriction = 600f;
    [Export] public float StaggerThreshold = 10f;

    [ExportGroup("Shake Settings")]
    [Export] public float ShakeAngle = 5.0f;
    [Export] public float ShakeDuration = 0.15f;

    private Node2D _targetNode;
    private Sprite2D _sprite;
    private CharacterBody2D _characterBody;
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private bool _isStaggered = false;

    public override void _Ready()
    {
        _targetNode = GetParent<Node2D>();
        _sprite = _targetNode.GetNodeOrNull<Sprite2D>("Sprite2D");
        _characterBody = _targetNode as CharacterBody2D;

        // 如果父节点是 CharacterBody2D，启用击退功能
        if (_characterBody != null)
        {
            EnableKnockback = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isStaggered && _characterBody != null)
        {
            // 应用击退摩擦力
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackFriction * (float)delta);
            _characterBody.Velocity = _knockbackVelocity;

            // 如果速度足够小，恢复正常
            if (_knockbackVelocity.Length() < StaggerThreshold)
            {
                _isStaggered = false;
                OnStaggerEnded?.Invoke();
            }
        }
    }

    [Signal]
    public delegate void StaggerEndedEventHandler();

    public event StaggerEndedEventHandler OnStaggerEnded;

    /// <summary>
    /// 播放受击效果
    /// </summary>
    public void PlayHitEffect(Vector2? sourcePosition = null)
    {
        if (EnableFlash && _sprite != null)
        {
            PlayFlash();
        }

        if (EnableShake)
        {
            PlayShake();
        }

        if (EnableKnockback && _characterBody != null && sourcePosition.HasValue)
        {
            ApplyKnockback(sourcePosition.Value);
        }
    }

    private void PlayFlash()
    {
        Tween tween = CreateTween();
        _sprite.Modulate = FlashColor;
        tween.TweenProperty(_sprite, "modulate", Colors.White, FlashDuration);
    }

    private void PlayShake()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_targetNode, "rotation_degrees", ShakeAngle, ShakeDuration / 3);
        tween.TweenProperty(_targetNode, "rotation_degrees", -ShakeAngle, ShakeDuration / 3);
        tween.TweenProperty(_targetNode, "rotation_degrees", 0.0f, ShakeDuration / 3);
    }

    private void ApplyKnockback(Vector2 sourcePosition)
    {
        Vector2 knockbackDir = (_targetNode.GlobalPosition - sourcePosition).Normalized();
        _knockbackVelocity = knockbackDir * KnockbackForce;
        _isStaggered = true;
    }

    /// <summary>
    /// 手动设置击退速度（用于外部控制）
    /// </summary>
    public void SetKnockbackVelocity(Vector2 velocity)
    {
        _knockbackVelocity = velocity;
        _isStaggered = true;
    }

    /// <summary>
    /// 检查是否处于击退状态
    /// </summary>
    public bool IsStaggered => _isStaggered;

    /// <summary>
    /// 重置所有效果状态（用于重生等场景）
    /// </summary>
    public void Reset()
    {
        _isStaggered = false;
        _knockbackVelocity = Vector2.Zero;
        
        if (_characterBody != null)
        {
            _characterBody.Velocity = Vector2.Zero;
        }
        
        if (_sprite != null)
        {
            _sprite.Modulate = Colors.White;
        }
        
        if (_targetNode != null)
        {
            _targetNode.RotationDegrees = 0f;
        }
    }
}

