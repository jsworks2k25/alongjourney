using Godot;

/// <summary>
/// 游戏全局配置单例，只保留真正需要全局共享的配置
/// 其他配置应使用组件的 Export 属性在 Inspector 中设置
/// </summary>
public partial class GameConfig : Node
{
    public static GameConfig Instance { get; private set; }

    // --- 玩家组名（全局共享） ---
    [ExportGroup("Groups")]
    [Export] public string PlayerGroupName = "Player";

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 获取玩家组名（空值安全）
    /// </summary>
    public static string GetPlayerGroupName()
    {
        return Instance?.PlayerGroupName ?? "Player";
    }
}

