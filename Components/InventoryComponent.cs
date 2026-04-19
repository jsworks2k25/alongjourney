namespace AlongJourney.Components;

using Godot;
using System.Collections.Generic;
using AlongJourney.Resources.Items;

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

    public sealed class SlotSnapshot
    {
        public string ItemId;
        public int Count;
    }

    [ExportGroup("Inventory Settings")]
    [Export] public int Capacity = 20;
    [Export] public string[] StartingItemIds = System.Array.Empty<string>();
    [Export] public int[] StartingItemCounts = System.Array.Empty<int>();

    // 核心数据：槽位列表
    public List<Slot> Slots { get; private set; } = new List<Slot>();
    private bool _startingItemsApplied;
    private bool _waitingForDatabase;

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

        TryApplyStartingItems();
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

    public bool MoveOrMergeSlot(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex) || fromIndex == toIndex)
        {
            return false;
        }

        var fromSlot = Slots[fromIndex];
        var toSlot = Slots[toIndex];
        if (fromSlot.IsEmpty)
        {
            return false;
        }

        if (CanMergeSlots(fromSlot, toSlot))
        {
            int transfer = System.Math.Min(fromSlot.Count, toSlot.RemainingSpace);
            if (transfer <= 0)
            {
                return false;
            }

            toSlot.Count += transfer;
            fromSlot.Count -= transfer;
            if (fromSlot.Count <= 0)
            {
                fromSlot.Item = null;
                fromSlot.Count = 0;
            }
        }
        else
        {
            SwapSlotContents(fromSlot, toSlot);
        }

        EmitSignal(SignalName.InventoryUpdated);
        return true;
    }

    public Slot GetSlot(int index)
    {
        if (index < 0 || index >= Slots.Count)
        {
            return null;
        }

        return Slots[index];
    }

    public bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < Slots.Count;
    }

    /// <summary>
    /// 检查是否有足够的物品
    /// </summary>
    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    public List<SlotSnapshot> CaptureSnapshot()
    {
        var snapshots = new List<SlotSnapshot>(Slots.Count);
        foreach (var slot in Slots)
        {
            snapshots.Add(new SlotSnapshot
            {
                ItemId = slot.Item?.Id ?? string.Empty,
                Count = slot.IsEmpty ? 0 : slot.Count,
            });
        }

        return snapshots;
    }

    public void RestoreSnapshot(IReadOnlyList<SlotSnapshot> snapshots)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].Item = null;
            Slots[i].Count = 0;
        }

        if (snapshots == null)
        {
            EmitSignal(SignalName.InventoryUpdated);
            return;
        }

        for (int i = 0; i < System.Math.Min(Slots.Count, snapshots.Count); i++)
        {
            var snapshot = snapshots[i];
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ItemId) || snapshot.Count <= 0)
            {
                continue;
            }

            var item = ItemDatabase.Instance?.GetItem(snapshot.ItemId);
            if (item == null)
            {
                GD.PushWarning($"{Name}: 恢复背包时未找到物品 {snapshot.ItemId}");
                continue;
            }

            Slots[i].Item = item;
            Slots[i].Count = System.Math.Min(snapshot.Count, item.MaxStack);
        }

        _startingItemsApplied = true;
        EmitSignal(SignalName.InventoryUpdated);
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

    private static bool CanMergeSlots(Slot fromSlot, Slot toSlot)
    {
        return fromSlot.Item != null &&
               toSlot.Item == fromSlot.Item &&
               fromSlot.Item.IsStackable &&
               toSlot.Count < fromSlot.Item.MaxStack;
    }

    private static void SwapSlotContents(Slot fromSlot, Slot toSlot)
    {
        ItemData tempItem = toSlot.Item;
        int tempCount = toSlot.Count;

        toSlot.Item = fromSlot.Item;
        toSlot.Count = fromSlot.Count;
        fromSlot.Item = tempItem;
        fromSlot.Count = tempCount;
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

    public override void _ExitTree()
    {
        DisconnectDatabaseSignal();
    }

    private void TryApplyStartingItems()
    {
        if (_startingItemsApplied || StartingItemIds.Length == 0)
        {
            return;
        }

        if (ItemDatabase.Instance == null)
        {
            return;
        }

        if (!ItemDatabase.Instance.IsLoaded)
        {
            if (!_waitingForDatabase)
            {
                ItemDatabase.Instance.DatabaseLoaded += OnItemDatabaseLoaded;
                _waitingForDatabase = true;
            }

            return;
        }

        for (int i = 0; i < StartingItemIds.Length; i++)
        {
            string itemId = StartingItemIds[i];
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            int count = 1;
            if (i < StartingItemCounts.Length)
            {
                count = System.Math.Max(StartingItemCounts[i], 1);
            }

            var itemData = ItemDatabase.Instance.GetItem(itemId);
            if (itemData == null)
            {
                GD.PushWarning($"{Name}: 未找到初始物品 {itemId}");
                continue;
            }

            AddItem(itemData, count);
        }

        _startingItemsApplied = true;
        DisconnectDatabaseSignal();
    }

    private void OnItemDatabaseLoaded()
    {
        TryApplyStartingItems();
    }

    private void DisconnectDatabaseSignal()
    {
        if (!_waitingForDatabase || ItemDatabase.Instance == null)
        {
            return;
        }

        ItemDatabase.Instance.DatabaseLoaded -= OnItemDatabaseLoaded;
        _waitingForDatabase = false;
    }
}