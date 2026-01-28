namespace AlongJourney.Resources.Items;

using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 物品数据库单例：统一管理所有物品数据
/// 自动扫描 resources/items/ 目录并加载所有 ItemData 资源
/// </summary>
public partial class ItemDatabase : Node
{
    public static ItemDatabase Instance { get; private set; }

    /// <summary>
    /// 物品数据字典：Key 为物品 ID，Value 为 ItemData
    /// </summary>
    private Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>();

    /// <summary>
    /// 物品列表（按 ID 排序）
    /// </summary>
    public IReadOnlyList<ItemData> AllItems => _items.Values.OrderBy(item => item.Id).ToList();

    /// <summary>
    /// 物品数量
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// 是否已加载完成
    /// </summary>
    public bool IsLoaded { get; private set; } = false;

    [Signal]
    public delegate void DatabaseLoadedEventHandler();

    [Signal]
    public delegate void ItemRegisteredEventHandler(string itemId, ItemData itemData);

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr("ItemDatabase: 检测到重复实例，销毁当前实例");
            QueueFree();
            return;
        }

        Instance = this;
        LoadItems();
    }

    /// <summary>
    /// 加载所有物品数据
    /// </summary>
    private void LoadItems()
    {
        _items.Clear();
        IsLoaded = false;

        string itemsPath = "res://resources/items/";
        
        // 检查目录是否存在
        if (!DirAccess.DirExistsAbsolute(itemsPath))
        {
            GD.PrintErr($"ItemDatabase: 物品目录不存在: {itemsPath}");
            IsLoaded = true;
            EmitSignal(SignalName.DatabaseLoaded);
            return;
        }

        // 扫描目录中的所有 .tres 文件
        using var dir = DirAccess.Open(itemsPath);
        if (dir == null)
        {
            GD.PrintErr($"ItemDatabase: 无法打开物品目录: {itemsPath}");
            IsLoaded = true;
            EmitSignal(SignalName.DatabaseLoaded);
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        int loadedCount = 0;
        int errorCount = 0;

        while (!string.IsNullOrEmpty(fileName))
        {
            // 跳过目录和隐藏文件
            if (dir.CurrentIsDir() || fileName.StartsWith("."))
            {
                fileName = dir.GetNext();
                continue;
            }

            // 只处理 .tres 文件
            if (fileName.EndsWith(".tres"))
            {
                string filePath = itemsPath + fileName;
                LoadItemFromPath(filePath, ref loadedCount, ref errorCount);
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();

        IsLoaded = true;
        EmitSignal(SignalName.DatabaseLoaded);
    }

    /// <summary>
    /// 从文件路径加载物品
    /// </summary>
    private void LoadItemFromPath(string filePath, ref int loadedCount, ref int errorCount)
    {
        var resource = GD.Load<ItemData>(filePath);
        
        if (resource == null)
        {
            GD.PrintErr($"ItemDatabase: 无法加载物品资源: {filePath}");
            errorCount++;
            return;
        }

        // 验证物品数据
        if (!resource.Validate(out string errorMessage))
        {
            GD.PrintErr($"ItemDatabase: 物品验证失败 [{filePath}]: {errorMessage}");
            errorCount++;
            return;
        }

        // 检查 ID 重复
        if (_items.ContainsKey(resource.Id))
        {
            GD.PrintErr($"ItemDatabase: 物品 ID 重复: {resource.Id} (文件: {filePath})");
            errorCount++;
            return;
        }

        // 注册物品
        _items[resource.Id] = resource;
        loadedCount++;
        
        EmitSignal(SignalName.ItemRegistered, resource.Id, resource);
    }

    /// <summary>
    /// 根据 ID 获取物品数据
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <returns>物品数据，如果不存在返回 null</returns>
    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        return _items.GetValueOrDefault(itemId);
    }

    /// <summary>
    /// 检查物品是否存在
    /// </summary>
    public bool HasItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && _items.ContainsKey(itemId);
    }

    /// <summary>
    /// 根据类型获取所有物品
    /// </summary>
    public List<ItemData> GetItemsByType(ItemType type)
    {
        return _items.Values.Where(item => item.Type == type).ToList();
    }

    /// <summary>
    /// 根据稀有度获取所有物品
    /// </summary>
    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        return _items.Values.Where(item => item.Rarity == rarity).ToList();
    }

    /// <summary>
    /// 搜索物品（根据名称或描述）
    /// </summary>
    public List<ItemData> SearchItems(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<ItemData>();
        }

        string lowerKeyword = keyword.ToLower();
        return _items.Values
            .Where(item => 
                item.Name.ToLower().Contains(lowerKeyword) ||
                (!string.IsNullOrEmpty(item.Description) && item.Description.ToLower().Contains(lowerKeyword)) ||
                item.Id.ToLower().Contains(lowerKeyword))
            .ToList();
    }

    /// <summary>
    /// 手动注册物品（用于运行时动态添加）
    /// </summary>
    public bool RegisterItem(ItemData itemData)
    {
        if (itemData == null)
        {
            GD.PrintErr("ItemDatabase: 尝试注册空的物品数据");
            return false;
        }

        if (!itemData.Validate(out string errorMessage))
        {
            GD.PrintErr($"ItemDatabase: 物品验证失败: {errorMessage}");
            return false;
        }

        if (_items.ContainsKey(itemData.Id))
        {
            GD.PushWarning($"ItemDatabase: 物品 ID 已存在，将覆盖: {itemData.Id}");
        }

        _items[itemData.Id] = itemData;
        EmitSignal(SignalName.ItemRegistered, itemData.Id, itemData);
        
        return true;
    }

    /// <summary>
    /// 重新加载所有物品（用于编辑器或热重载）
    /// </summary>
    public void Reload()
    {
        LoadItems();
    }

    /// <summary>
    /// 获取所有物品 ID 列表
    /// </summary>
    public List<string> GetAllItemIds()
    {
        return _items.Keys.OrderBy(id => id).ToList();
    }

    /// <summary>
    /// 验证数据库完整性（检查是否有重复 ID、无效数据等）
    /// </summary>
    public bool ValidateDatabase(out List<string> errors)
    {
        errors = new List<string>();
        var seenIds = new HashSet<string>();

        foreach (var kvp in _items)
        {
            var item = kvp.Value;
            
            // 验证物品数据
            if (!item.Validate(out string errorMessage))
            {
                errors.Add($"物品 [{item.Id}]: {errorMessage}");
            }

            // 检查 ID 重复（理论上不应该发生，但检查一下）
            if (seenIds.Contains(item.Id))
            {
                errors.Add($"重复的物品 ID: {item.Id}");
            }
            seenIds.Add(item.Id);
        }

        return errors.Count == 0;
    }
}
