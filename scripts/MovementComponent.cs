using Godot;

/// <summary>
/// 最小化的移动组件：只负责根据黑板中的移动意图计算速度
/// 不依赖玩家输入或AI寻路逻辑，只处理"移动方向"到"速度"的转换
/// </summary>
public partial class MovementComponent : BaseComponent
{
    [Export] public float Speed = 100f;

    private MovementSmoothingComponent _movementSmoothing;
    private Vector2 _isoVec = new Vector2(1f, 0.5f);

    public override void Initialize()
    {
        _movementSmoothing = Owner.GetNodeOrNull<MovementSmoothingComponent>("CoreComponents/MovementSmoothingComponent")
            ?? Owner.GetNodeOrNull<MovementSmoothingComponent>("MovementSmoothingComponent");

        if (GameConfig.Instance != null)
        {
            _isoVec = GameConfig.Instance.IsometricVector;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null || !Owner.IsAlive)
        {
            return;
        }

        // 状态处理由 Actor 或状态机负责，这里只处理移动
        // 如果处于 Stagger 或 Attack 状态，不更新速度（由 HitEffectComponent 或 Actor 控制）
        if (Owner.CurrentState == Actor.ActorState.Stagger || Owner.CurrentState == Actor.ActorState.Attack)
        {
            return;
        }

        // 优先使用 KeyMoveDirection，兼容 KeyInputVector
        Vector2 moveDir = Owner.GetBlackboardVector(Actor.KeyMoveDirection, Vector2.Zero);
        if (moveDir.LengthSquared() < 0.01f)
        {
            moveDir = Owner.GetBlackboardVector(Actor.KeyInputVector, Vector2.Zero);
        }

        // 优先使用黑板中的 Speed，否则使用组件自己的 Speed
        float moveSpeed = Owner.GetBlackboardFloat(Actor.KeyMoveSpeed, 0f);
        if (moveSpeed <= 0f)
        {
            moveSpeed = Speed;
        }

        // 计算目标速度
        Vector2 targetVelocity = moveDir * moveSpeed * _isoVec;
        float deltaSeconds = (float)delta;

        // 应用平滑
        if (_movementSmoothing != null)
        {
            Owner.Velocity = moveDir.Length() > 0.01f
                ? _movementSmoothing.Accelerate(Owner.Velocity, targetVelocity, deltaSeconds)
                : _movementSmoothing.Brake(Owner.Velocity, deltaSeconds);
        }
        else
        {
            Owner.Velocity = targetVelocity;
        }
    }
}
