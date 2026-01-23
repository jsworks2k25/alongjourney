using Godot;

public partial class Enemy : CharacterBody2D
{
    // 获取子节点的引用
    protected HealthComponent _healthComponent;
    
    // 简单的状态机枚举
    protected enum EnemyState { Idle, Chase, Attack }
    protected EnemyState CurrentState = EnemyState.Idle;
    
    [Export] public float Speed = 50f;
    public Player TargetPlayer; // 目标引用

    public override void _Ready()
    {
        // 自动寻找组件
        _healthComponent = GetNode<HealthComponent>("HealthComponent");
        
        // 订阅死亡信号：死了就播放动画并消失
        _healthComponent.Died += OnDied;
    }

    // 子类可以重写这个方法来实现不同的 AI
    public override void _PhysicsProcess(double delta)
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
                // 简单的巡逻逻辑或发呆
                break;
            case EnemyState.Chase:
                if (TargetPlayer != null)
                {
                    Vector2 direction = (TargetPlayer.GlobalPosition - GlobalPosition).Normalized();
                    Velocity = direction * Speed;
                    MoveAndSlide();
                }
                break;
        }
    }

    private void OnDied()
    {
        // 播放死亡动画，掉落物品等
        QueueFree(); // 简单销毁
    }
}