namespace AlongJourney.UI.Inventory;

using Godot;
using AlongJourney.Components;

public partial class InventorySlotView : Button
{
    [Signal] public delegate void SlotPressedEventHandler(int slotIndex);
    [Signal] public delegate void SlotDroppedEventHandler(int fromSlotIndex, int toSlotIndex);

    public int SlotIndex { get; private set; } = -1;
    public bool IsHotbarSlot { get; private set; }
    public InventoryComponent.Slot SlotData { get; private set; }

    public void Configure(int slotIndex, bool isHotbarSlot, InventoryComponent.Slot slotData)
    {
        SlotIndex = slotIndex;
        IsHotbarSlot = isHotbarSlot;
        SlotData = slotData;
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);

        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            mouseButton.Pressed)
        {
            EmitSignal(SignalName.SlotPressed, SlotIndex);
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (SlotData == null || SlotData.IsEmpty)
        {
            return default;
        }

        var preview = new PanelContainer();
        preview.CustomMinimumSize = new Vector2(96, 72);
        var label = new Label
        {
            Text = $"{SlotData.Item.Name}\nx{SlotData.Count}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        preview.AddChild(label);
        SetDragPreview(preview);

        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            { "from_slot", SlotIndex },
        };
        return Variant.CreateFrom(data);
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (SlotIndex < 0)
        {
            return false;
        }

        var dictionary = data.AsGodotDictionary();
        if (!dictionary.ContainsKey("from_slot"))
        {
            return false;
        }

        int fromSlot = (int)dictionary["from_slot"];
        return fromSlot >= 0 && fromSlot != SlotIndex;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.ContainsKey("from_slot"))
        {
            return;
        }

        int fromSlot = (int)dictionary["from_slot"];
        EmitSignal(SignalName.SlotDropped, fromSlot, SlotIndex);
    }
}
