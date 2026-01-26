using Godot;

/// <summary>
/// 通用物品掉落物基类：使用 ItemData Resource 系统
/// 支持磁吸效果、自动收集和出生动画
/// 所有掉落物都可以使用这个类，只需配置 ItemData 即可
/// </summary>
public partial class ItemDrop : Area2D
{
    [ExportGroup("Item Settings")]
    [Export] public ItemData ItemData;
    [Export] public int Amount = 1;

    [ExportGroup("Movement")]
    [Export] public float MagnetSpeed = 300.0f;
    [Export] public float Acceleration = 10.0f;

    [ExportGroup("Spawn Animation")]
    [Export] public bool EnableSpawnAnimation = true;
    [Export] public float SpawnOffsetDistance = 15.0f;
    [Export] public float SpawnAnimationDuration = 0.3f;

    private bool _isMagnetized = false;
    private Node2D _playerTarget = null;
    private Vector2 _velocity = Vector2.Zero;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        
        // 如果 ItemData 已设置，自动应用图标
        ApplyItemData();
        
        // 播放出生动画
        if (EnableSpawnAnimation)
        {
            PlaySpawnAnimation();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isMagnetized && _playerTarget != null)
        {
            UpdateMagnetMovement(delta);
            CheckCollection();
        }
    }

    /// <summary>
    /// 应用 ItemData 的图标到 Sprite2D
    /// </summary>
    private void ApplyItemData()
    {
        if (ItemData?.Icon != null)
        {
            var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (sprite != null)
            {
                sprite.Texture = ItemData.Icon;
            }
        }
    }

    /// <summary>
    /// 播放出生动画：随机方向弹出
    /// </summary>
    private void PlaySpawnAnimation()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();
        Vector2 randomDir = new Vector2(
            rng.RandfRange(-1f, 1f),
            rng.RandfRange(-1f, 1f)
        ).Normalized();

        Tween tween = CreateTween();
        tween.SetParallel(true);

        // 位移动画
        tween.TweenProperty(this, "position", Position + randomDir * SpawnOffsetDistance, SpawnAnimationDuration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);

        // 缩放动画
        Vector2 originalScale = Scale;
        Scale = originalScale * 0.5f;
        tween.TweenProperty(this, "scale", originalScale, SpawnAnimationDuration)
             .SetTrans(Tween.TransitionType.Elastic)
             .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// 更新磁吸移动
    /// </summary>
    private void UpdateMagnetMovement(double delta)
    {
        Vector2 direction = (_playerTarget.GlobalPosition - GlobalPosition).Normalized();
        
        float magnetSpeed = GameConfig.Instance?.ItemMagnetSpeed ?? MagnetSpeed;
        float acceleration = GameConfig.Instance?.ItemAcceleration ?? Acceleration;

        _velocity = _velocity.Lerp(direction * magnetSpeed, (float)delta * acceleration);
        GlobalPosition += _velocity * (float)delta;
    }

    /// <summary>
    /// 检查收集距离
    /// </summary>
    private void CheckCollection()
    {
        float collectDistance = GameConfig.Instance?.ItemCollectionDistance ?? 10.0f;
        
        if (GlobalPosition.DistanceTo(_playerTarget.GlobalPosition) < collectDistance)
        {
            CollectItem();
        }
    }

    /// <summary>
    /// 玩家进入检测范围
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        string groupName = GameConfig.GetPlayerGroupName();
        if (body.IsInGroup(groupName))
        {
            _isMagnetized = true;
            _playerTarget = body;
        }
    }

    /// <summary>
    /// 收集物品到背包
    /// </summary>
    private void CollectItem()
    {
        if (_playerTarget == null || ItemData == null)
        {
            return;
        }

        // 查找背包组件
        var inventory = FindInventoryComponent(_playerTarget);
        
        if (inventory != null)
        {
            int added = inventory.AddItem(ItemData, Amount);
            
            if (added > 0)
            {
                // 成功收集，销毁掉落物
                OnItemCollected();
                QueueFree();
            }
            else
            {
                // 背包满了，停止磁吸
                OnInventoryFull();
                _isMagnetized = false;
                _playerTarget = null;
            }
        }
    }

    /// <summary>
    /// 物品被收集时的回调（可被子类重写）
    /// </summary>
    protected virtual void OnItemCollected()
    {
        // 可以在这里播放音效、特效等
    }

    /// <summary>
    /// 背包满时的回调（可被子类重写）
    /// </summary>
    protected virtual void OnInventoryFull()
    {
        // 可以在这里给玩家反馈，比如播放提示音效
    }

    /// <summary>
    /// 查找背包组件（支持多种查找方式）
    /// </summary>
    private InventoryComponent FindInventoryComponent(Node2D target)
    {
        // 方式1：直接路径查找
        var inventory = target.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory != null)
        {
            return inventory;
        }

        // 方式2：在子节点中查找
        foreach (var child in target.GetChildren())
        {
            if (child is InventoryComponent comp)
            {
                return comp;
            }
        }

        // 方式3：在父节点中查找（如果组件挂在父节点）
        var parent = target.GetParent();
        if (parent != null)
        {
            return parent.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        }

        return null;
    }
}
