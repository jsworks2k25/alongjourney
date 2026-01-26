using Godot;

[GlobalClass] // 让你能在编辑器里新建这个资源
public partial class ItemData : Resource
{
    [Export] public string Id;
    [Export] public string Name;
    [Export] public Texture2D Icon;
    [Export] public int MaxStack = 99;
    [Export] public bool IsConsumable = false;
    
    // 你可以在这里加更复杂的逻辑，比如
    // [Export] public int DamageBonus;
    // [Export] public PackedScene BuildingPrefab; // 如果是造塔的物品
}