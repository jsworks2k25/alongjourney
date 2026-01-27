using Godot;

/// <summary>
/// 树：可被砍伐的障碍物，使用 ItemData Resource 系统配置掉落物
/// </summary>
public partial class Tree : TranslucentObstacle, IDamageable, IInteractable
{
    [ExportGroup("Tree Properties")]
    [Export] public int MaxHealth = 30;

    [ExportGroup("Drops")]
    [Export] public ItemData DropItemData;          // 使用 ItemData Resource
    [Export] public PackedScene DropItemScene;      // 通用的掉落物场景模板
    [Export] public int DropCount = 3;
    [Export] public int DropAmountPerItem = 1;      // 每个掉落物的数量

    [ExportGroup("Interaction")]
    [Export] public Color HoverColor = new Color(1.2f, 1.2f, 1.0f, 1.0f); // 悬停时的颜色（稍微变亮变黄）
    [Export] public float HoverTransitionDuration = 0.15f; // 悬停颜色过渡时间

    private int _currentHealth;
    private Sprite2D _sprite;
    private Color _originalColor;
    private Tween _hoverTween;
    private bool _isHovered = false;

    public override void _Ready()
    {
        base._Ready();
        _currentHealth = MaxHealth;
        
        // 获取 Sprite2D 引用
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite != null)
        {
            _originalColor = _sprite.Modulate;
        }
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        _currentHealth -= damage;

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

        // 获取场景根节点（更可靠的方法）
        Node sceneRoot = GetTree().CurrentScene;
        if (sceneRoot == null)
        {
            // 如果 CurrentScene 不可用，尝试获取根节点
            sceneRoot = GetTree().Root.GetChild(GetTree().Root.GetChildCount() - 1);
        }

        if (sceneRoot == null)
        {
            GD.PrintErr("错误：无法获取场景根节点！");
            QueueFree();
            return;
        }

        Vector2 treePosition = GlobalPosition; // 保存位置，因为 QueueFree 后 GlobalPosition 可能不可用

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
            drop.GlobalPosition = treePosition + randomOffset;

            // 添加到场景
            sceneRoot.AddChild(drop);
        }

        QueueFree();
    }

    // ========== IInteractable 接口实现 ==========

    public InteractionType GetInteractionType()
    {
        return InteractionType.Object;
    }

    public Vector2 GetInteractionPosition()
    {
        return GlobalPosition;
    }

    public bool CanInteractWith(Weapon weapon)
    {
        // 只有斧头可以砍树（未来可以扩展支持其他工具）
        return weapon is Axe;
    }

    public void OnHoverEnter()
    {
        if (_isHovered || _sprite == null)
        {
            return;
        }

        _isHovered = true;

        // 停止之前的过渡动画
        if (_hoverTween != null && _hoverTween.IsValid())
        {
            _hoverTween.Kill();
        }

        // 创建颜色过渡动画
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_sprite, "modulate", HoverColor, HoverTransitionDuration);
    }

    public void OnHoverExit()
    {
        if (!_isHovered || _sprite == null)
        {
            return;
        }

        _isHovered = false;

        // 停止之前的过渡动画
        if (_hoverTween != null && _hoverTween.IsValid())
        {
            _hoverTween.Kill();
        }

        // 恢复原始颜色
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_sprite, "modulate", _originalColor, HoverTransitionDuration);
    }
}