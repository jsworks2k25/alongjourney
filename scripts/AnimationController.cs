using Godot;

/// <summary>
/// 动画控制器组件，统一管理动画播放逻辑
/// </summary>
public partial class AnimationController : BaseComponent
{
    [Export] private AnimationPlayer _animPlayer;
    [Export] private Sprite2D _sprite;

    [ExportGroup("Animation Names")]
    [Export] public string AnimIdleFront = "idle_front";
    [Export] public string AnimIdleBack = "idle_back";
    [Export] public string AnimMoveFront = "move_front";
    [Export] public string AnimMoveBack = "move_back";
    [Export] public string AnimDie = "die";

    [ExportGroup("Settings")]
    [Export] public float VelocityDeadZone = 5f;
    [Export] public bool UseIsometricDirections = false; // true: 前后, false: 上下左右

    private string _currentAnimation = "";
    private bool _isDead = false;
    private Vector2 _lastMoveDir = Vector2.Down;

    public override void Initialize()
    {
        if (_animPlayer == null && Owner != null)
            _animPlayer = Owner.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        if (_sprite == null && Owner != null)
            _sprite = Owner.GetNodeOrNull<Sprite2D>("Sprite2D");

        // 动画名称和阈值现在通过 Export 属性在 Inspector 中配置

        if (Owner != null)
        {
            _isDead = Owner.GetBlackboardBool(Actor.BlackboardKeys.IsDead, false);
            UpdateAnimation(Owner.GetBlackboardVector(Actor.BlackboardKeys.Velocity, Vector2.Zero));
        }
    }

    protected override void OnOwnerStateChanged(string newStateName)
    {
        if (newStateName == "DeadState" || Owner.GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            PlayDeathAnimation();
        }
    }

    protected override void OnOwnerBlackboardChanged(string key, Variant value)
    {
        if (key == Actor.BlackboardKeys.Velocity.ToString())
        {
            UpdateAnimation(value.AsVector2());
        }
    }

    /// <summary>
    /// 统一的动画更新方法：根据速度判断方向，使用 front/back 动画，通过 FlipH 处理左右翻转
    /// </summary>
    /// <param name="velocity">移动速度向量</param>
    public void UpdateAnimation(Vector2 velocity)
    {
        if (_isDead || _animPlayer == null) return;

        Vector2 dir = velocity;
        if (velocity.Length() > VelocityDeadZone)
        {
            _lastMoveDir = velocity;
            PlayAnimation(velocity.Y < 0 ? AnimMoveBack : AnimMoveFront);
        }
        else
        {
            dir = _lastMoveDir;
            PlayAnimation(dir.Y < 0 ? AnimIdleBack : AnimIdleFront);
        }
        if (_sprite != null)
            _sprite.FlipH = dir.X < 0;
    }

    public void PlayAnimation(string animName)
    {
        if (_animPlayer == null || _currentAnimation == animName) return;

        if (_animPlayer.HasAnimation(animName))
        {
            _animPlayer.Play(animName);
            _currentAnimation = animName;
        }
    }

    public void PlayDeathAnimation()
    {
        _isDead = true;
        PlayAnimation(AnimDie);
    }

}

