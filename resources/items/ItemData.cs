namespace AlongJourney.Resources.Items;

using Godot;
using Godot.Collections;

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Material,      // 材料（如木材、石头）
    Consumable,    // 消耗品（如药水、食物）
    Weapon,        // 武器
    Tool,          // 工具（如斧头、镐子）
    Equipment,     // 装备（如护甲、饰品）
    Quest,         // 任务物品
    Misc           // 其他
}

/// <summary>
/// 物品稀有度枚举
/// </summary>
public enum ItemRarity
{
    Common,        // 普通（白色）
    Uncommon,      //  uncommon（绿色）
    Rare,          // 稀有（蓝色）
    Epic,          // 史诗（紫色）
    Legendary      // 传说（橙色）
}

/// <summary>
/// 物品数据资源，使用 Godot Resource 系统
/// 在编辑器中创建 .tres 文件来定义物品属性
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
    [ExportGroup("Basic Info")]
    [Export] public string Id = "";
    [Export] public string Name = "";
    [Export] public string Description = "";
    [Export] public Texture2D Icon;
    
    [ExportGroup("Item Classification")]
    [Export] public ItemType Type = ItemType.Material;
    [Export] public ItemRarity Rarity = ItemRarity.Common;
    
    [ExportGroup("Stack Settings")]
    [Export] public int MaxStack = 99;
    [Export] public bool IsStackable = true;  // 是否可堆叠
    
    [ExportGroup("Item Properties")]
    [Export] public bool IsConsumable = false;
    [Export] public bool IsTradeable = true;  // 是否可交易
    [Export] public bool IsDroppable = true;  // 是否可丢弃
    
    [ExportGroup("Combat Stats")]
    [Export] public int AttackPower = 0;      // 攻击力加成
    [Export] public int DefensePower = 0;     // 防御力加成
    [Export] public float AttackSpeed = 1.0f; // 攻击速度倍率
    [Export] public int HealthBonus = 0;      // 生命值加成
    
    [ExportGroup("Usage")]
    [Export] public bool HasUseEffect = false;
    [Export] public int HealAmount = 0;       // 使用后恢复的生命值
    [Export] public float UseCooldown = 0f;  // 使用冷却时间（秒）
    
    [ExportGroup("Advanced")]
    [Export] public PackedScene Prefab;       // 物品预制体（如建筑、特殊物品）
    // 注意：Godot 不支持直接导出 Dictionary，如需自定义数据请使用其他方式
    // 例如：使用多个 Export 字段，或使用 JSON 字符串存储
    public Dictionary<string, Variant> CustomData = new(); // 自定义数据字典（运行时使用）
    
    /// <summary>
    /// 验证物品数据是否有效
    /// </summary>
    public bool Validate(out string errorMessage)
    {
        errorMessage = "";
        
        if (string.IsNullOrEmpty(Id))
        {
            errorMessage = "物品 ID 不能为空";
            return false;
        }
        
        if (string.IsNullOrEmpty(Name))
        {
            errorMessage = "物品名称不能为空";
            return false;
        }
        
        if (MaxStack < 1)
        {
            errorMessage = "最大堆叠数必须大于 0";
            return false;
        }
        
        if (!IsStackable && MaxStack > 1)
        {
            errorMessage = "不可堆叠的物品最大堆叠数应为 1";
            return false;
        }
        
        if (AttackSpeed <= 0)
        {
            errorMessage = "攻击速度必须大于 0";
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取物品的显示颜色（基于稀有度）
    /// </summary>
    public Color GetRarityColor()
    {
        return Rarity switch
        {
            ItemRarity.Common => Colors.White,
            ItemRarity.Uncommon => Colors.LimeGreen,
            ItemRarity.Rare => Colors.Cyan,
            ItemRarity.Epic => Colors.Magenta,
            ItemRarity.Legendary => Colors.Orange,
            _ => Colors.White
        };
    }
    
    /// <summary>
    /// 获取物品的完整描述（包含属性信息）
    /// </summary>
    public string GetFullDescription()
    {
        string desc = Description;
        
        if (AttackPower > 0)
            desc += $"\n攻击力: +{AttackPower}";
        if (DefensePower > 0)
            desc += $"\n防御力: +{DefensePower}";
        if (HealthBonus > 0)
            desc += $"\n生命值: +{HealthBonus}";
        if (AttackSpeed != 1.0f)
            desc += $"\n攻击速度: {AttackSpeed:P0}";
        if (IsConsumable && HealAmount > 0)
            desc += $"\n使用效果: 恢复 {HealAmount} 生命值";
        
        return desc;
    }
    
    /// <summary>
    /// 检查物品是否可用于战斗
    /// </summary>
    public bool IsCombatItem()
    {
        return Type == ItemType.Weapon || 
               Type == ItemType.Equipment || 
               AttackPower > 0 || 
               DefensePower > 0;
    }
}
