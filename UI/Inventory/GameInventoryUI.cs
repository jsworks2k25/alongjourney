namespace AlongJourney.UI.Inventory;

using System.Collections.Generic;
using Godot;
using AlongJourney.Components;
using AlongJourney.Core;
using AlongJourney.Entities.Player;
using AlongJourney.Resources.Items;

public partial class GameInventoryUI : CanvasLayer
{
    private const double RebindIntervalSeconds = 0.5d;

    private Player _player;
    private InventoryComponent _inventory;
    private Control _root;
    private PanelContainer _hotbarPanel;
    private HBoxContainer _hotbarRow;
    private PanelContainer _inventoryPanel;
    private GridContainer _inventoryGrid;
    private Label _selectedItemLabel;
    private Label _dragHintLabel;
    private readonly List<InventorySlotView> _hotbarButtons = new();
    private readonly List<InventorySlotView> _inventoryButtons = new();
    private double _rebindCooldown;

    public override void _Ready()
    {
        Layer = 64;
        BuildUi();
        RebindToCurrentPlayer();
        RefreshUi();
    }

    public override void _Process(double delta)
    {
        if (IsBoundPlayerValid())
        {
            return;
        }

        _rebindCooldown -= delta;
        if (_rebindCooldown > 0d)
        {
            return;
        }

        RebindToCurrentPlayer();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (keyEvent.Keycode != Key.I && keyEvent.Keycode != Key.Tab)
        {
            return;
        }

        ToggleInventory();
        GetViewport().SetInputAsHandled();
    }

    private void BuildUi()
    {
        _root = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _selectedItemLabel = new Label
        {
            Text = "未选择物品",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _selectedItemLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _selectedItemLabel.Position = new Vector2(20, 20);
        _root.AddChild(_selectedItemLabel);

        _dragHintLabel = new Label
        {
            Text = "拖拽槽位可交换或合并物品",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _dragHintLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _dragHintLabel.Position = new Vector2(20, 44);
        _root.AddChild(_dragHintLabel);

        _inventoryPanel = new PanelContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(420, 360),
        };
        _inventoryPanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _inventoryPanel.OffsetLeft = -460;
        _inventoryPanel.OffsetTop = 20;
        _inventoryPanel.OffsetRight = -20;
        _inventoryPanel.OffsetBottom = 420;
        _root.AddChild(_inventoryPanel);

        var inventoryMargin = new MarginContainer();
        inventoryMargin.AddThemeConstantOverride("margin_left", 12);
        inventoryMargin.AddThemeConstantOverride("margin_top", 12);
        inventoryMargin.AddThemeConstantOverride("margin_right", 12);
        inventoryMargin.AddThemeConstantOverride("margin_bottom", 12);
        _inventoryPanel.AddChild(inventoryMargin);

        var inventoryVBox = new VBoxContainer();
        inventoryVBox.AddThemeConstantOverride("separation", 8);
        inventoryMargin.AddChild(inventoryVBox);

        var title = new Label
        {
            Text = "Backpack",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        inventoryVBox.AddChild(title);

        _inventoryGrid = new GridContainer
        {
            Columns = 4,
        };
        _inventoryGrid.AddThemeConstantOverride("h_separation", 8);
        _inventoryGrid.AddThemeConstantOverride("v_separation", 8);
        inventoryVBox.AddChild(_inventoryGrid);

        _hotbarPanel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(480, 96),
        };
        _hotbarPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _hotbarPanel.OffsetLeft = 320;
        _hotbarPanel.OffsetTop = -120;
        _hotbarPanel.OffsetRight = -320;
        _hotbarPanel.OffsetBottom = -20;
        _root.AddChild(_hotbarPanel);

        var hotbarMargin = new MarginContainer();
        hotbarMargin.AddThemeConstantOverride("margin_left", 8);
        hotbarMargin.AddThemeConstantOverride("margin_top", 8);
        hotbarMargin.AddThemeConstantOverride("margin_right", 8);
        hotbarMargin.AddThemeConstantOverride("margin_bottom", 8);
        _hotbarPanel.AddChild(hotbarMargin);

        _hotbarRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _hotbarRow.AddThemeConstantOverride("separation", 8);
        hotbarMargin.AddChild(_hotbarRow);
    }

    private bool IsBoundPlayerValid()
    {
        return _player != null && IsInstanceValid(_player) && _player.IsInsideTree();
    }

    private void RebindToCurrentPlayer()
    {
        var nextPlayer = GetTree().GetFirstNodeInGroup(GameConstants.PlayerGroupName) as Player;
        if (_player == nextPlayer && IsBoundPlayerValid())
        {
            return;
        }

        DisconnectCurrentPlayer();
        _player = nextPlayer;
        _inventory = _player?.Inventory;
        _rebindCooldown = RebindIntervalSeconds;

        if (_player != null)
        {
            _player.ActiveSlotChanged += OnActiveSelectionChanged;
            _player.ActiveItemChanged += OnActiveItemChanged;
        }

        if (_inventory != null)
        {
            _inventory.InventoryUpdated += OnInventoryUpdated;
        }

        RebuildSlotButtons();
        RefreshUi();
    }

    private void DisconnectCurrentPlayer()
    {
        if (_player != null)
        {
            _player.ActiveSlotChanged -= OnActiveSelectionChanged;
            _player.ActiveItemChanged -= OnActiveItemChanged;
        }

        if (_inventory != null)
        {
            _inventory.InventoryUpdated -= OnInventoryUpdated;
        }

        _player = null;
        _inventory = null;
    }

    private void RebuildSlotButtons()
    {
        ClearButtons(_hotbarButtons, _hotbarRow);
        ClearButtons(_inventoryButtons, _inventoryGrid);

        int hotbarCount = _player?.GetHotbarSlotCount() ?? 0;
        for (int i = 0; i < hotbarCount; i++)
        {
            int slotIndex = i;
            var button = CreateSlotButton(new Vector2(88, 72));
            button.Configure(slotIndex, true, _inventory?.GetSlot(slotIndex));
            button.SlotPressed += OnSlotPressed;
            button.SlotDropped += OnSlotDropped;
            _hotbarButtons.Add(button);
            _hotbarRow.AddChild(button);
        }

        int inventoryCount = _inventory?.Slots.Count ?? 0;
        for (int i = 0; i < inventoryCount; i++)
        {
            int slotIndex = i;
            var button = CreateSlotButton(new Vector2(92, 72));
            button.Configure(slotIndex, false, _inventory?.GetSlot(slotIndex));
            button.SlotPressed += OnSlotPressed;
            button.SlotDropped += OnSlotDropped;
            _inventoryButtons.Add(button);
            _inventoryGrid.AddChild(button);
        }
    }

    private static InventorySlotView CreateSlotButton(Vector2 minSize)
    {
        return new InventorySlotView
        {
            CustomMinimumSize = minSize,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Top,
            Alignment = HorizontalAlignment.Center,
            Text = "Empty",
            FocusMode = Control.FocusModeEnum.None,
        };
    }

    private static void ClearButtons(List<InventorySlotView> buttons, Node parent)
    {
        buttons.Clear();

        foreach (Node child in parent.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void ToggleInventory()
    {
        _inventoryPanel.Visible = !_inventoryPanel.Visible;
    }

    private void OnInventoryUpdated()
    {
        if (_inventoryButtons.Count != (_inventory?.Slots.Count ?? 0) ||
            _hotbarButtons.Count != (_player?.GetHotbarSlotCount() ?? 0))
        {
            RebuildSlotButtons();
        }

        RefreshUi();
    }

    private void OnActiveSelectionChanged(int slotIndex)
    {
        RefreshUi();
    }

    private void OnActiveItemChanged(ItemData item, int slotIndex)
    {
        RefreshUi();
    }

    private void RefreshUi()
    {
        RefreshSelectedItemLabel();
        RefreshHotbarButtons();
        RefreshInventoryButtons();
    }

    private void RefreshSelectedItemLabel()
    {
        if (_player == null || _inventory == null)
        {
            _selectedItemLabel.Text = "等待玩家生成...";
            return;
        }

        var activeItem = _player.ActiveItem;
        if (activeItem == null)
        {
            _selectedItemLabel.Text = $"当前槽位: {_player.ActiveSlotIndex + 1}";
            return;
        }

        _selectedItemLabel.Text = $"当前物品: {activeItem.Name}  x{_inventory.GetItemCount(activeItem)}";
    }

    private void RefreshHotbarButtons()
    {
        for (int i = 0; i < _hotbarButtons.Count; i++)
        {
            var slot = _inventory?.GetSlot(i);
            UpdateButtonVisual(_hotbarButtons[i], slot, i, true);
        }
    }

    private void RefreshInventoryButtons()
    {
        for (int i = 0; i < _inventoryButtons.Count; i++)
        {
            var slot = _inventory?.GetSlot(i);
            UpdateButtonVisual(_inventoryButtons[i], slot, i, false);
        }
    }

    private void UpdateButtonVisual(Button button, InventoryComponent.Slot slot, int slotIndex, bool isHotbar)
    {
        if (button is InventorySlotView slotView)
        {
            slotView.Configure(slotIndex, isHotbar, slot);
        }

        bool isSelected = _player != null && _player.ActiveSlotIndex == slotIndex;
        button.Modulate = isSelected ? new Color(1.2f, 1.15f, 0.8f) : Colors.White;
        button.Icon = slot?.Item?.Icon;

        string prefix = isHotbar ? $"[{slotIndex + 1}]" : $"#{slotIndex + 1}";
        if (slot == null || slot.IsEmpty)
        {
            button.Text = $"{prefix}\nEmpty";
            button.TooltipText = "空槽位";
            return;
        }

        string countText = slot.Item.IsStackable ? $"x{slot.Count}" : "Ready";
        button.Text = $"{prefix}\n{slot.Item.Name}\n{countText}";
        button.TooltipText = slot.Item.GetFullDescription();
    }

    private void OnSlotPressed(int slotIndex)
    {
        _player?.SelectInventorySlot(slotIndex);
    }

    private void OnSlotDropped(int fromSlotIndex, int toSlotIndex)
    {
        if (_inventory == null)
        {
            return;
        }

        if (_inventory.MoveOrMergeSlot(fromSlotIndex, toSlotIndex))
        {
            _player?.SelectInventorySlot(toSlotIndex);
        }
    }
}
