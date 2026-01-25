using Godot;

/// <summary>
/// 游戏全局配置单例，统一管理硬编码的配置值
/// </summary>
public partial class GameConfig : Node
{
    public static GameConfig Instance { get; private set; }

    // --- 等距视角配置 ---
    [Export] public Vector2 IsometricVector = new Vector2(1f, 0.5f);

    // --- 动画名称配置 ---
    [ExportGroup("Animation Names")]
    [Export] public string AnimIdleFront = "idle_front";  // 对应 down（向下/向前）
    [Export] public string AnimIdleBack = "idle_back";    // 对应 up（向上/向后）
    [Export] public string AnimMoveFront = "move_front";  // 对应 down（向下/向前）
    [Export] public string AnimMoveBack = "move_back";     // 对应 up（向上/向后）
    [Export] public string AnimDie = "die";
    [Export] public string AnimSwing = "swing";

    // --- 受击反馈配置 ---
    [ExportGroup("Hit Effect")]
    [Export] public Color HitFlashColor = Colors.Red;
    [Export] public Color HitFlashColorHDR = new Color(10, 10, 10, 1);
    [Export] public float HitFlashDuration = 0.1f;
    [Export] public float HitFlashDurationLong = 0.2f;
    [Export] public float KnockbackForce = 50f;
    [Export] public float KnockbackFriction = 600f;
    [Export] public float StaggerThreshold = 10f; // 击退速度低于此值恢复正常

    // --- 移动检测阈值 ---
    [ExportGroup("Movement Thresholds")]
    [Export] public float VelocityDeadZone = 5f; // 速度低于此值视为静止
    [Export] public float VelocitySmallThreshold = 0.1f; // 小速度阈值

    // --- 物品收集配置 ---
    [ExportGroup("Item Collection")]
    [Export] public float ItemCollectionDistance = 10.0f;
    [Export] public float ItemMagnetSpeed = 300.0f;
    [Export] public float ItemAcceleration = 10.0f;
    [Export] public float ItemDetectRadius = 60.0f;

    // --- 玩家组名 ---
    [ExportGroup("Groups")]
    [Export] public string PlayerGroupName = "Player";

    // --- 重生配置 ---
    [ExportGroup("Respawn")]
    [Export] public float RespawnDelay = 3.0f; // 重生倒计时（秒）

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }
}

