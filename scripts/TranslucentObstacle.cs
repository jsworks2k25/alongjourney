using Godot;

public partial class TranslucentObstacle : StaticBody2D
{
    [Export]
    public float TransparencyAlpha = 0.4f; // 遮挡时的透明度 (0.0 - 1.0)
    
    [Export]
    public float FadeDuration = 0.2f; // 渐变时间 (秒)

    private Sprite2D _sprite;
    private Area2D _occlusionArea;
    private Tween _tween;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _occlusionArea = GetNode<Area2D>("OcclusionArea");

        _occlusionArea.BodyEntered += OnBodyEntered;
        _occlusionArea.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        string groupName = GameConfig.GetPlayerGroupName();
        if (body.IsInGroup(groupName) || body is ITargetable)
        {
            FadeTo(TransparencyAlpha);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        string groupName = GameConfig.GetPlayerGroupName();
        if (body.IsInGroup(groupName) || body is ITargetable)
        {
            FadeTo(1.0f);
        }
    }

    private void FadeTo(float targetAlpha)
    {
        if (_tween != null && _tween.IsRunning())
        {
            _tween.Kill();
        }

        _tween = CreateTween();
        
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Cubic);

        Color targetColor = _sprite.Modulate;
        targetColor.A = targetAlpha;

        _tween.TweenProperty(_sprite, "modulate", targetColor, FadeDuration);
    }
}