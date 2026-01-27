using Godot;

/// <summary>
/// 最小化的移动组件：只负责根据黑板中的移动意图计算速度
/// 不依赖玩家输入或AI寻路逻辑，只处理"移动方向"到"速度"的转换
/// 集成了移动平滑功能（加速和制动）
/// </summary>
public partial class MovementComponent : BaseComponent
{
    [Export] public float Speed = 100f;
    [Export] public float Acceleration = 200f;
    [Export] public float Friction = 150f;

    [Export] public Vector2 IsometricVector = new Vector2(1f, 0.5f);


    public override void _Ready()
    {
        base._Ready();
        // 确保物理处理被启用
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Owner == null)
        {
            GD.PushError($"{Name}: MovementComponent Owner is null!");
            return;
        }
        
        if (!Owner.IsAlive)
        {
            return;
        }

        // 状态处理由状态机负责，这里只处理移动
        // 如果处于 Stagger 或 Attack 状态，不更新速度（由 KnockbackComponent 或其他组件控制）
        if (Owner.IsInState<StaggerState>() || Owner.IsInState<AttackState>())
        {
            return;
        }

        // 优先使用 KeyMoveDirection，兼容 KeyInputVector
        Vector2 moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        if (moveDir.LengthSquared() < 0.01f)
        {
            moveDir = Owner.GetBlackboardVector(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        }

        // 优先使用黑板中的 Speed，否则使用组件自己的 Speed
        float moveSpeed = Owner.GetBlackboardFloat(Actor.BlackboardKeys.MoveSpeed, 0f);
        if (moveSpeed <= 0f)
        {
            moveSpeed = Speed;
        }

        // 计算目标速度
        Vector2 targetVelocity = moveDir * moveSpeed * IsometricVector;
        float deltaSeconds = (float)delta;

        // 应用平滑
        Owner.Velocity = moveDir.Length() > 0.01f
            ? Accelerate(Owner.Velocity, targetVelocity, deltaSeconds)
            : Brake(Owner.Velocity, deltaSeconds);

        Owner.ApplyMovement();
    }

    private Vector2 Accelerate(Vector2 currentVelocity, Vector2 targetVelocity, float delta)
    {
        if (Acceleration <= 0f)
        {
            return targetVelocity;
        }

        return currentVelocity.MoveToward(targetVelocity, Acceleration * delta);
    }

    private Vector2 Brake(Vector2 currentVelocity, float delta)
    {
        if (Friction <= 0f)
        {
            return Vector2.Zero;
        }

        return currentVelocity.MoveToward(Vector2.Zero, Friction * delta);
    }
}
