namespace AlongJourney.Components;

using Godot;
using AlongJourney.Entities;

/// <summary>
/// 受击反馈组件，统一处理闪烁、抖动等视觉效果
/// </summary>
public partial class HitEffectComponent : BaseComponent
{
    [Export] public bool EnableFlash = true;
    [Export] public bool EnableShake = false;

    [ExportGroup("Flash Settings")]
    [Export] public Color FlashColor = Colors.Red;
    [Export] public float FlashDuration = 0.1f;

    [ExportGroup("Shake Settings")]
    [Export] public float ShakeAngle = 5.0f;
    [Export] public float ShakeDuration = 0.15f;

    private Node2D _targetNode;
    private Sprite2D _sprite;

    public override void Initialize()
    {
        _targetNode = Owner;
        _sprite = Owner?.GetNodeOrNull<Sprite2D>("Sprite2D");
    }

    protected override void OnOwnerBlackboardChanged(string key, Variant value)
    {
        if (Owner == null)
        {
            return;
        }

        if (key != Actor.BlackboardKeys.HitPending.ToString() || !value.AsBool())
        {
            return;
        }

        Vector2 source = Owner.GetBlackboardVector(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
        bool hasSource = !float.IsNaN(source.X) && !float.IsNaN(source.Y);
        PlayHitEffect(hasSource ? source : (Vector2?)null);

        Owner.SetBlackboardValue(Actor.BlackboardKeys.HitPending, false);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
    }


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

    /// <summary>
    /// 重置所有效果状态（用于重生等场景）
    /// </summary>
    public void Reset()
    {
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

