namespace AlongJourney.Components;

using Godot;
using AlongJourney.Entities;

/// <summary>
/// 动画控制器组件，管理动画播放逻辑
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
    [Export] public bool UseIsometricDirections = false; // true: 前后, false: 上下左右

    /// <summary>与 <see cref="MovementComponent"/> 一致：意图长度平方大于此视为“在移动”（用于选 move/idle 与朝向），与击退后的实际速度解耦。</summary>
    private const float MoveIntentLengthSqThreshold = 0.01f;

    private string _currentAnimation = "";
    private bool _isDead = false;
    private Vector2 _lastAnimFacingDir = Vector2.Down;

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
            UpdateAnimation();
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
        if (key == Actor.BlackboardKeys.Velocity.ToString()
            || key == Actor.BlackboardKeys.MoveDirection.ToString()
            || key == Actor.BlackboardKeys.InputVector.ToString())
        {
            UpdateAnimation();
        }
    }

    /// <summary>
    /// 根据黑板上的移动意图（玩家输入 / AI 写入的 <see cref="Actor.BlackboardKeys.MoveDirection"/>）选择前后与 FlipH；
    /// move / idle 与朝向均不跟实际 <see cref="Actor.BlackboardKeys.Velocity"/>，击退时身体可反向运动但贴图朝向保持意图方向。
    /// </summary>
    public void UpdateAnimation()
    {
        if (_isDead || Owner == null || _animPlayer == null) return;

        Vector2 intent = GetMoveIntent(Owner);
        bool hasIntent = intent.LengthSquared() > MoveIntentLengthSqThreshold;

        Vector2 facing = hasIntent ? intent : _lastAnimFacingDir;
        if (hasIntent)
            _lastAnimFacingDir = intent;

        if (hasIntent)
            PlayAnimation(facing.Y < 0 ? AnimMoveBack : AnimMoveFront);
        else
            PlayAnimation(facing.Y < 0 ? AnimIdleBack : AnimIdleFront);

        if (_sprite != null)
            _sprite.FlipH = facing.X < 0;
    }

    private static Vector2 GetMoveIntent(Actor actor)
    {
        Vector2 moveDir = actor.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        if (moveDir.LengthSquared() < MoveIntentLengthSqThreshold)
            moveDir = actor.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        return moveDir;
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

