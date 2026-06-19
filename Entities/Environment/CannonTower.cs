namespace AlongJourney.Entities.Environment;

using Godot;
using AlongJourney.Components;
using AlongJourney.Entities;

/// <summary>
/// 防御塔：检测范围内敌人、转向并按间隔开火。
/// 节点原点 = 2×2 占地中心（四格交叉点）；Sprite offset 仅做美术微调。
/// </summary>
public partial class CannonTower : StaticBody2D
{
    [ExportGroup("Combat")]
    [Export] public int DamagePerShot = 8;
    [Export] public float FireInterval = 1.0f;
    [Export] public float AttackRange = 120f;

    [ExportGroup("Visual")]
    [Export] public float RecoilPixels = 3f;
    [Export] public float RecoilOutDuration = 0.04f;
    [Export] public float RecoilBackDuration = 0.1f;

    [ExportGroup("Nodes")]
    [Export] private Area2D _rangeArea;
    [Export] private Sprite2D _sprite;

    private float _fireTimer;
    private Vector2 _spriteBaseOffset;
    private Tween _fireTween;

    public override void _Ready()
    {
        _sprite ??= GetNodeOrNull<Sprite2D>("Sprite2D");
        _rangeArea ??= GetNodeOrNull<Area2D>("RangeArea");

        if (_sprite != null)
        {
            _spriteBaseOffset = _sprite.Offset;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Actor target = FindBestTarget();
        if (target == null)
        {
            // 短暂丢目标（击退出范围等）时保留 CD，不重置
            return;
        }

        UpdateFacing(target.GlobalPosition);

        _fireTimer -= (float)delta;
        if (_fireTimer <= 0f)
        {
            FireAt(target);
            _fireTimer = FireInterval;
        }
    }

    private Actor FindBestTarget()
    {
        if (_rangeArea == null)
        {
            return null;
        }

        Actor closest = null;
        float bestDistSq = AttackRange * AttackRange;

        foreach (Area2D area in _rangeArea.GetOverlappingAreas())
        {
            if (area is not HurtboxComponent)
            {
                continue;
            }

            var actor = area.GetParent<Actor>();
            if (actor == null || !actor.IsAlive)
            {
                continue;
            }

            float distSq = GlobalPosition.DistanceSquaredTo(actor.GlobalPosition);
            if (distSq <= bestDistSq)
            {
                bestDistSq = distSq;
                closest = actor;
            }
        }

        return closest;
    }

    private void FireAt(Actor target)
    {
        GetHurtbox(target)?.TakeDamage(DamagePerShot, GlobalPosition);
        PlayFireRecoil();
    }

    private static HurtboxComponent GetHurtbox(Actor actor)
    {
        if (actor.HurtboxComponent != null)
        {
            return actor.HurtboxComponent;
        }

        return actor.GetNodeOrNull<HurtboxComponent>("Hurtbox");
    }

    private void UpdateFacing(Vector2 targetPos)
    {
        if (_sprite == null)
        {
            return;
        }

        Vector2 dir = (targetPos - GlobalPosition).Normalized();
        _sprite.Frame = DirectionToFrame(dir);
    }

    private void PlayFireRecoil()
    {
        if (_sprite == null)
        {
            return;
        }

        _fireTween?.Kill();
        _fireTween = CreateTween();

        Vector2 fireDir = FrameToFireDirection(_sprite.Frame);
        Vector2 recoilOffset = _spriteBaseOffset - fireDir * RecoilPixels;

        _fireTween.TweenProperty(_sprite, "offset", recoilOffset, RecoilOutDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        _fireTween.TweenProperty(_sprite, "offset", _spriteBaseOffset, RecoilBackDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private static Vector2 FrameToFireDirection(int frame)
    {
        return frame switch
        {
            1 => Vector2.Right,
            2 => Vector2.Up,
            3 => Vector2.Left,
            _ => Vector2.Down,
        };
    }

    /// <summary>
    /// cannon.png 帧顺序：0=下, 1=右, 2=上, 3=左
    /// </summary>
    private static int DirectionToFrame(Vector2 dir)
    {
        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
        {
            return dir.X > 0 ? 1 : 3;
        }

        return dir.Y > 0 ? 0 : 2;
    }
}
