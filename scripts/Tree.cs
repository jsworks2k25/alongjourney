using Godot;

/// <summary>
/// 树：可被砍伐的障碍物，使用 ItemData Resource 系统配置掉落物
/// </summary>
public partial class Tree : TranslucentObstacle, IDamageable
{
    [ExportGroup("Tree Properties")]
    [Export] public int MaxHealth = 30;

    [ExportGroup("Drops")]
    [Export] public ItemData DropItemData;          // 使用 ItemData Resource
    [Export] public PackedScene DropItemScene;      // 通用的掉落物场景模板
    [Export] public int DropCount = 3;
    [Export] public int DropAmountPerItem = 1;      // 每个掉落物的数量

    private int _currentHealth;

    public override void _Ready()
    {
        base._Ready();
        _currentHealth = MaxHealth;
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        _currentHealth -= damage;
        GD.Print($"树当前血量: {_currentHealth}");

        PlayHitEffect();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "rotation_degrees", 5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees", -5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees", 0.0f, 0.05f);
    }

    private void Die()
    {
        if (DropItemData == null || DropItemScene == null)
        {
            GD.PrintErr("错误：DropItemData 或 DropItemScene 未配置！");
            QueueFree();
            return;
        }

        var rootNode = GetTree().CurrentScene;

        for (int i = 0; i < DropCount; i++)
        {
            // 实例化掉落物场景（使用通用的 ItemDrop 基类）
            var drop = DropItemScene.Instantiate<ItemDrop>();
            if (drop == null)
            {
                GD.PrintErr("错误：DropItemScene 必须是 ItemDrop 类型！");
                continue;
            }

            // 设置掉落物数据（使用 Resource 系统）
            drop.ItemData = DropItemData;
            drop.Amount = DropAmountPerItem;

            // 随机位置偏移
            Vector2 randomOffset = new Vector2(
                GD.Randf() * 20f - 10f,
                GD.Randf() * 20f - 10f
            );
            drop.GlobalPosition = GlobalPosition + randomOffset;

            // 添加到场景
            rootNode.CallDeferred(Node.MethodName.AddChild, drop);
        }

        QueueFree();
    }
}