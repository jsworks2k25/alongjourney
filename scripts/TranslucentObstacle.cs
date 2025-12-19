using Godot;
using System;

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
        // 1. 获取节点引用
        // 假设 Sprite2D 的名字就是 "Sprite2D"
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _occlusionArea = GetNode<Area2D>("OcclusionArea");

        // 2. 连接信号
        // 当有物体进入/离开检测区域时触发
        _occlusionArea.BodyEntered += OnBodyEntered;
        _occlusionArea.BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        // 检查进入的是不是玩家
        // 建议给 Player 节点添加一个 Group 叫 "Player"，或者检查名字
        if (body.Name == "Player" || body.IsInGroup("Player"))
        {
            FadeTo(TransparencyAlpha);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.Name == "Player" || body.IsInGroup("Player"))
        {
            FadeTo(1.0f); // 恢复完全不透明
        }
    }

    private void FadeTo(float targetAlpha)
    {
        // 防止 Tween 冲突，如果正在变色，先杀掉上一个 Tween
        if (_tween != null && _tween.IsRunning())
        {
            _tween.Kill();
        }

        // 创建新的 Tween 动画
        _tween = CreateTween();
        
        // 设置缓动效果 (EaseInOut 让变化更平滑)
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Cubic);

        // 修改 Sprite 的 Modulate 颜色的 Alpha 通道
        // 注意：Godot 4 C# 中颜色通常是 Color 结构体
        Color targetColor = _sprite.Modulate;
        targetColor.A = targetAlpha;

        _tween.TweenProperty(_sprite, "modulate", targetColor, FadeDuration);
    }
}