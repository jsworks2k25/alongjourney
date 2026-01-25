using Godot;

public partial class Enemy : Actor
{
    // 简单的状态机枚举
    protected enum EnemyState { Idle, Chase, Attack }
    protected EnemyState CurrentState = EnemyState.Idle;
    
    [Export] public float Speed = 50f;
    protected ITargetable _target; // 使用接口而不是直接引用 Player

    protected Vector2 _isoVec;

    public override void _Ready()
    {
        base._Ready();

        // 从 GameConfig 获取等距向量
        _isoVec = GameConfig.Instance != null ? GameConfig.Instance.IsometricVector : new Vector2(1f, 0.5f);

        // 将 Speed 写入黑板，供 MovementComponent 使用
        SetBlackboardValue(Actor.KeyMoveSpeed, Speed);

        // 查找目标（玩家）
        FindTarget();
    }

    /// <summary>
    /// 查找目标（使用 Group 或接口）
    /// </summary>
    protected virtual void FindTarget()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target as GodotObject) && _target.IsAlive)
            return;
        _target = null;

        string groupName = GameConfig.GetPlayerGroupName();
        var players = GetTree().GetNodesInGroup(groupName);

        foreach (var player in players)
        {
            if (player is ITargetable targetable && targetable.IsAlive)
            {
                _target = targetable;
                break;
            }
        }
    }

    /// <summary>
    /// 获取目标位置，子类可以重写
    /// </summary>
    protected virtual Vector2? GetTargetPosition()
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target as GodotObject)) return null;
        if (!_target.IsAlive) return null;
        return _target.GlobalPosition;
    }

    // 子类可以重写这个方法来实现不同的 AI
    public override void _PhysicsProcess(double delta)
    {
        if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
            _target = null;
        if (_target == null || !_target.IsAlive)
            FindTarget();

        switch (CurrentState)
        {
            case EnemyState.Idle:
                // 简单的巡逻逻辑或发呆
                HandleIdleState(delta);
                break;
            case EnemyState.Chase:
                HandleChaseState(delta);
                break;
        }
        base._PhysicsProcess(delta);
    }

    protected virtual void HandleChaseState(double delta)
    {
        var targetPos = GetTargetPosition();
        if (targetPos.HasValue)
        {
            Vector2 direction = (targetPos.Value - GlobalPosition).Normalized();
            // 写入移动意图到黑板，由 MovementComponent 处理
            SetBlackboardValue(Actor.KeyMoveDirection, direction);
        }
        else
        {
            SetBlackboardValue(Actor.KeyMoveDirection, Vector2.Zero);
        }
    }

    protected virtual void HandleIdleState(double delta)
    {
        // 空闲时停止移动
        SetBlackboardValue(Actor.KeyMoveDirection, Vector2.Zero);
    }

    protected override void HandleDied()
    {
        base.HandleDied();

        // 延迟销毁，让动画播放完
        GetTree().CreateTimer(0.5f).Timeout += QueueFree;
    }
}