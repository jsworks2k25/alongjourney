using Godot;
using System.Collections.Generic;

/// <summary>
/// 背包组件：管理物品的存储和操作
/// </summary>
public partial class InventoryComponent : BaseComponent
{
    /// <summary>
    /// 背包槽位结构
    /// </summary>
    public class Slot
    {
        public ItemData Item;
        public int Count;

        public bool IsEmpty => Item == null || Count <= 0;
        public int RemainingSpace => Item != null ? Item.MaxStack - Count : 0;
    }

    [ExportGroup("Inventory Settings")]
    [Export] public int Capacity = 20;

    // 核心数据：槽位列表
    public List<Slot> Slots { get; private set; } = new List<Slot>();

    // 信号：物品变化时发出，UI 可以监听此信号刷新显示
    [Signal] public delegate void InventoryUpdatedEventHandler();
    [Signal] public delegate void ItemAddedEventHandler(ItemData item, int amount);
    [Signal] public delegate void ItemRemovedEventHandler(ItemData item, int amount);

    public override void Initialize()
    {
        // 初始化空槽位
        Slots.Clear();
        for (int i = 0; i < Capacity; i++)
        {
            Slots.Add(new Slot());
        }
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <returns>成功添加的数量（可能小于请求的数量）</returns>
    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;

        // 1. 先尝试堆叠到已有槽位
        foreach (var slot in Slots)
        {
            if (slot.Item == item && slot.Count < item.MaxStack)
            {
                int canAdd = System.Math.Min(remaining, slot.RemainingSpace);
                slot.Count += canAdd;
                remaining -= canAdd;

                if (remaining <= 0)
                {
                    EmitSignal(SignalName.InventoryUpdated);
                    EmitSignal(SignalName.ItemAdded, item, amount);
                    return amount;
                }
            }
        }

        // 2. 找空位放置剩余物品
        while (remaining > 0)
        {
            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                break; // 背包满了
            }

            int canAdd = System.Math.Min(remaining, item.MaxStack);
            emptySlot.Item = item;
            emptySlot.Count = canAdd;
            remaining -= canAdd;
        }

        int added = amount - remaining;
        if (added > 0)
        {
            EmitSignal(SignalName.InventoryUpdated);
            EmitSignal(SignalName.ItemAdded, item, added);
        }

        return added;
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    /// <returns>成功移除的数量</returns>
    public int RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;

        foreach (var slot in Slots)
        {
            if (slot.Item == item)
            {
                int canRemove = System.Math.Min(remaining, slot.Count);
                slot.Count -= canRemove;
                remaining -= canRemove;

                if (slot.Count <= 0)
                {
                    slot.Item = null;
                    slot.Count = 0;
                }

                if (remaining <= 0)
                {
                    break;
                }
            }
        }

        int removed = amount - remaining;
        if (removed > 0)
        {
            EmitSignal(SignalName.InventoryUpdated);
            EmitSignal(SignalName.ItemRemoved, item, removed);
        }

        return removed;
    }

    /// <summary>
    /// 查询物品数量
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null)
        {
            return 0;
        }

        int total = 0;
        foreach (var slot in Slots)
        {
            if (slot.Item == item)
            {
                total += slot.Count;
            }
        }

        return total;
    }

    /// <summary>
    /// 检查是否有足够的物品
    /// </summary>
    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    /// <summary>
    /// 查找空槽位
    /// </summary>
    private Slot FindEmptySlot()
    {
        foreach (var slot in Slots)
        {
            if (slot.IsEmpty)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    public void Clear()
    {
        foreach (var slot in Slots)
        {
            slot.Item = null;
            slot.Count = 0;
        }

        EmitSignal(SignalName.InventoryUpdated);
    }
}