using Godot;
using System.Collections.Generic;

public partial class InventoryComponent : BaseComponent
{
    // 定义一个简单的槽位结构
    public class Slot {
        public ItemData Item;
        public int Count;
    }

    [Export] public int Capacity = 20; 
    
    // 核心数据：列表
    public List<Slot> Slots = new List<Slot>();

    // 信号：UI 只需监听这个信号来刷新
    [Signal] public delegate void InventoryUpdatedEventHandler();

    public override void Initialize()
    {
        // 初始化空槽位
        for(int i=0; i<Capacity; i++) Slots.Add(new Slot());
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        // 1. 先找有没有同类物品可以堆叠
        foreach(var slot in Slots) {
            if (slot.Item == item && slot.Count < item.MaxStack) {
                // 这里简写了，实际需处理 amount > 剩余空间的情况
                slot.Count += amount;
                EmitSignal(SignalName.InventoryUpdated);
                return true;
            }
        }
        
        // 2. 找空位
        foreach(var slot in Slots) {
            if (slot.Item == null) {
                slot.Item = item;
                slot.Count = amount;
                EmitSignal(SignalName.InventoryUpdated);
                return true;
            }
        }
        
        return false; // 背包满了
    }
    
    // 移除物品、查询数量等方法...
}