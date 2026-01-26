using Godot;

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
    
    [ExportGroup("Stack Settings")]
    [Export] public int MaxStack = 99;
    
    [ExportGroup("Item Type")]
    [Export] public bool IsConsumable = false;
    
    // 可以扩展更多属性
    // [Export] public int DamageBonus;
    // [Export] public PackedScene BuildingPrefab;
}