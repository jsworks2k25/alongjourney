using Godot;
using System;

public partial class WoodDrop : Area2D
{
    // 配置参数
    [Export] public float MagnetSpeed = 300.0f;     // 初始飞行速度
    [Export] public float Acceleration = 10.0f;     // 加速度
    [Export] public float DetectRadius = 60.0f;     // 触发磁铁的距离

    private bool _isMagnetized = false; // 是否已经被吸住
    private Node2D _playerTarget = null; // 玩家引用
    private Vector2 _velocity = Vector2.Zero; // 当前速度

    public override void _Ready()
    {
        // 1. 出生时的“爆出”效果 (Juice!)
        // 随机一个方向弹开
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        Vector2 randomDir = new Vector2(rng.RandfRange(-1, 1), rng.RandfRange(-1, 1)).Normalized();
        
        // 使用 Tween 制作一个简单的抛物线跳跃效果
        Tween tween = CreateTween();
        tween.SetParallel(true); // 让位移和缩放同时发生
        
        // 向随机方向移动一点距离 (模拟掉落散开)
        tween.TweenProperty(this, "position", Position + randomDir * 15, 0.3f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
             
        // 只是为了视觉好看，可以加个简单的弹跳缩放
        Scale = new Vector2(0.5f, 0.5f);
        tween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.3f)
             .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        // 绑定碰撞信号（当有东西进入Area2D）
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 2. 磁铁逻辑
        if (_isMagnetized && _playerTarget != null)
        {
            // 计算方向
            Vector2 direction = (_playerTarget.GlobalPosition - GlobalPosition).Normalized();
            
            // 从 GameConfig 获取配置
            float magnetSpeed = GameConfig.Instance != null ? GameConfig.Instance.ItemMagnetSpeed : MagnetSpeed;
            float acceleration = GameConfig.Instance != null ? GameConfig.Instance.ItemAcceleration : Acceleration;
            float collectDistance = GameConfig.Instance != null ? GameConfig.Instance.ItemCollectionDistance : 10.0f;
            
            // 简单的加速逻辑 (越飞越快)
            _velocity = _velocity.Lerp(direction * magnetSpeed, (float)delta * acceleration);
            GlobalPosition += _velocity * (float)delta;

            // 3. 收集判定 (距离足够近就吃掉)
            if (GlobalPosition.DistanceTo(_playerTarget.GlobalPosition) < collectDistance)
            {
                CollectItem();
            }
        }
        else 
        {
            // 如果还没被吸附，可以手动检测玩家距离（或者依靠 BodyEntered）
            // 这里演示依靠 BodyEntered 触发吸附
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        // 使用 Group 或接口查找玩家
        string groupName = GameConfig.Instance != null ? GameConfig.Instance.PlayerGroupName : "Player";
        if (body.IsInGroup(groupName) || body is ITargetable targetable)
        {
            _isMagnetized = true;
            _playerTarget = body;
            
            // 可选：在这里播放一个"发现目标"的小音效
        }
    }

    private void CollectItem()
    {
        // TODO: 在这里调用玩家的背包系统，比如 body.Inventory.Add("Wood", 1);
        GD.Print("木头被收集了！");
        
        // 销毁自己
        QueueFree();
    }
}