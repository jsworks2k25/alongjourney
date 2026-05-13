namespace AlongJourney.Entities.Player;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Entities.Items;
using AlongJourney.Entities.States;
using AlongJourney.Core;
using AlongJourney.Components;
using AlongJourney.Resources.Items;

public partial class Player : Actor
{
    [Signal]
    public delegate void PlayerDiedEventHandler(Player player);

    [Signal]
    public delegate void ActiveSlotChangedEventHandler(int slotIndex);

    [Signal]
    public delegate void ActiveItemChangedEventHandler(ItemData item, int slotIndex);

    private Weapon _currentWeapon;
    private InventoryComponent _inventory;
    private BuildingSystem _buildingSystem;
    private ItemData _activeItem;
    private string _equippedItemId = string.Empty;
    private int _activeSlotIndex = -1;

    [Export] public Marker2D WeaponHolder;
    [Export] public int HotbarSize = 5;
    [Export] public int PlayerId { get; private set; } = GameConstants.DefaultLocalPlayerId;
    [Export] public bool IsLocalPlayer { get; private set; } = true;
    [Export] public int InputDeviceId { get; private set; } = GameConstants.KeyboardAndMouseDeviceId;

    [ExportGroup("References")]
    [Export] private SelectionManager _selectionManager;

    public InventoryComponent Inventory => _inventory;
    public ItemData ActiveItem => _activeItem;
    public int ActiveSlotIndex => _activeSlotIndex;

    public override void _Ready()
    {
        base._Ready();

        // 添加到 Player 组
        AddToGroup(GameConstants.PlayerGroupName);
        UpdateLocalPlayerGroup();

        // 订阅 HealthComponent 信号
        if (HealthComponent != null)
        {
            HealthComponent.Died += HandleDied;
            HealthComponent.HealthChanged += HandleHealthChanged;
        }

        _inventory = GetNodeOrNull<InventoryComponent>("Inventory");
        if (_inventory != null)
        {
            _inventory.InventoryUpdated += OnInventoryUpdated;
        }

        CallDeferred(nameof(InitializeInventoryState));
    }

    public override void _ExitTree()
    {
        if (HealthComponent != null)
        {
            HealthComponent.Died -= HandleDied;
            HealthComponent.HealthChanged -= HandleHealthChanged;
        }

        if (_inventory != null)
        {
            _inventory.InventoryUpdated -= OnInventoryUpdated;
        }

        if (_buildingSystem != null)
        {
            _buildingSystem.ObjectPlaced -= OnObjectPlaced;
        }

        DetachCurrentWeapon();
    }

    private SelectionManager GetSelectionManager()
    {
        if (_selectionManager != null && IsInstanceValid(_selectionManager) && _selectionManager.IsInsideTree())
        {
            return _selectionManager;
        }

        return null;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!CanHandleLocalInput() || !IsInputEventForThisPlayer(@event) || @event.IsEcho() || GetTree().Paused)
        {
            return;
        }

        if (HandleHotbarInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!@event.IsActionPressed("attack"))
        {
            return;
        }

        if (_activeItem != null && _activeItem.IsPlaceableItem())
        {
            return;
        }

        if (TryUseActiveItem())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        if (IsLocalPlayer)
        {
            HandleWeaponAiming();
        }

        // MovementComponent 和 PlayerInputComponent 会自动处理移动
        base._PhysicsProcess(delta);
    }

    public void ConfigureMultiplayerIdentity(int playerId, bool isLocalPlayer, int inputDeviceId)
    {
        PlayerId = playerId <= GameConstants.InvalidPlayerId
            ? GameConstants.DefaultLocalPlayerId
            : playerId;
        IsLocalPlayer = isLocalPlayer;
        InputDeviceId = inputDeviceId;
        UpdateLocalPlayerGroup();
    }

    public bool CanHandleLocalInput()
    {
        return IsAlive && IsLocalPlayer;
    }

    public bool IsInputEventForThisPlayer(InputEvent @event)
    {
        return InputDeviceId == GameConstants.KeyboardAndMouseDeviceId ||
               @event.Device == InputDeviceId;
    }

    private void UpdateLocalPlayerGroup()
    {
        if (!IsInsideTree())
        {
            return;
        }

        if (IsLocalPlayer)
        {
            AddToGroup(GameConstants.LocalPlayerGroupName);
        }
        else
        {
            RemoveFromGroup(GameConstants.LocalPlayerGroupName);
        }
    }

    private void HandleDied()
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);
        RequestStateChange<DeadState>();
        
        EmitSignal(SignalName.PlayerDied, this);
    }

    private void HandleHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, sourcePosition);
            RequestStateChange<StaggerState>();
            SetBlackboardValue(Actor.BlackboardKeys.HitPending, true);
        }
        else
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
        }
    }

    private void InitializeInventoryState()
    {
        _buildingSystem = ResolveBuildingSystem();
        if (_buildingSystem != null)
        {
            _buildingSystem.ObjectPlaced += OnObjectPlaced;
        }

        int initialSlot = FindFirstOccupiedSlot();
        if (initialSlot < 0)
        {
            initialSlot = 0;
        }

        SetActiveSlot(initialSlot, emitSignals: true);
    }

    private BuildingSystem ResolveBuildingSystem()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<BuildingSystem>("BuildingSystem");
    }

    private bool TryStartAttack()
    {
        if (_currentWeapon == null)
        {
            return false;
        }

        // 优先尝试攻击选中的目标
        var selectionManager = GetSelectionManager();
        if (selectionManager != null)
        {
            var hoveredTarget = selectionManager.GetCurrentHoveredTarget();
            if (hoveredTarget != null)
            {
                Vector2 targetPos = hoveredTarget.GetInteractionPosition();
                if (_currentWeapon.IsInRange(GlobalPosition, targetPos))
                {
                    if (_currentWeapon.AttackTarget(hoveredTarget, GlobalPosition))
                    {
                        SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, true);
                        return true;
                    }
                }
            }
        }

        // 如果没有选中目标或攻击失败，回退到鼠标方向攻击
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        if (_currentWeapon.Attack(mouseDir))
        {
            SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, true);
            return true;
        }

        return false;
    }

    private void HandleWeaponAiming()
    {
        if (WeaponHolder == null)
        {
            return;
        }

        Vector2 targetDir;

        // 如果有选中的目标，优先瞄准目标
        var selectionManager = GetSelectionManager();
        if (selectionManager != null)
        {
            var hoveredTarget = selectionManager.GetCurrentHoveredTarget();
            if (hoveredTarget != null)
            {
                targetDir = (hoveredTarget.GetInteractionPosition() - GlobalPosition).Normalized();
            }
            else
            {
                targetDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
            }
        }
        else
        {
            targetDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        }

        WeaponHolder.Rotation = targetDir.Angle();
        WeaponHolder.Scale = new Vector2(1, targetDir.X < 0 ? -1 : 1);
    }

    private void OnWeaponAttackFinished()
    {
        if (IsAlive)
        {
            // 清除攻击标志，状态机会自动转换回 Idle/Run
            SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, false);
        }
    }

    private bool TryUseActiveItem()
    {
        if (_activeItem != null && _activeItem.IsConsumableItem())
        {
            return TryConsumeActiveItem();
        }

        return TryStartAttack();
    }

    private bool TryConsumeActiveItem()
    {
        if (_inventory == null || _activeItem == null || HealthComponent == null)
        {
            return false;
        }

        if (_activeItem.HealAmount <= 0 || HealthComponent.CurrentHealth >= HealthComponent.MaxHealth)
        {
            return false;
        }

        int removed = _inventory.RemoveItem(_activeItem, 1);
        if (removed <= 0)
        {
            return false;
        }

        HealthComponent.Heal(_activeItem.HealAmount);
        RefreshActiveItemState();
        return true;
    }

    private bool HandleHotbarInput(InputEvent @event)
    {
        if (_inventory == null)
        {
            return false;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            int slotIndex = GetHotbarSlotFromKey(keyEvent.Keycode);
            if (slotIndex >= 0)
            {
                return SelectHotbarSlot(slotIndex);
            }
        }

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                CycleHotbarSelection(-1);
                return true;
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                CycleHotbarSelection(1);
                return true;
            }
        }

        return false;
    }

    private int GetHotbarSlotFromKey(Key keycode)
    {
        return keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            Key.Key0 => 9,
            _ => -1
        };
    }

    public bool SelectHotbarSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= GetHotbarSlotCount())
        {
            return false;
        }

        return SetActiveSlot(slotIndex, emitSignals: true);
    }

    public bool SelectInventorySlot(int slotIndex)
    {
        return SetActiveSlot(slotIndex, emitSignals: true);
    }

    public int GetHotbarSlotCount()
    {
        if (_inventory == null)
        {
            return HotbarSize;
        }

        return Mathf.Min(HotbarSize, _inventory.Slots.Count);
    }

    public void CycleHotbarSelection(int direction)
    {
        int hotbarCount = GetHotbarSlotCount();
        if (hotbarCount <= 0)
        {
            return;
        }

        int currentSlot = Mathf.Clamp(_activeSlotIndex, 0, hotbarCount - 1);
        int nextSlot = (currentSlot + direction + hotbarCount) % hotbarCount;
        SetActiveSlot(nextSlot, emitSignals: true);
    }

    private bool SetActiveSlot(int slotIndex, bool emitSignals)
    {
        if (_inventory == null)
        {
            return false;
        }

        if (_inventory.Slots.Count == 0)
        {
            slotIndex = -1;
        }
        else
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, _inventory.Slots.Count - 1);
        }

        _activeSlotIndex = slotIndex;
        RefreshActiveItemState(emitSignals);
        return true;
    }

    private void RefreshActiveItemState(bool emitSignals = true)
    {
        _activeItem = _inventory?.GetSlot(_activeSlotIndex)?.Item;
        ApplySelectedItemMode();

        if (emitSignals)
        {
            EmitSignal(SignalName.ActiveSlotChanged, _activeSlotIndex);
            EmitSignal(SignalName.ActiveItemChanged, _activeItem, _activeSlotIndex);
        }
    }

    private void ApplySelectedItemMode()
    {
        if (_buildingSystem == null || !IsInstanceValid(_buildingSystem))
        {
            _buildingSystem = ResolveBuildingSystem();
            if (_buildingSystem != null)
            {
                _buildingSystem.ObjectPlaced -= OnObjectPlaced;
                _buildingSystem.ObjectPlaced += OnObjectPlaced;
            }
        }

        if (_activeItem != null && _activeItem.IsToolItem())
        {
            _buildingSystem?.CancelBuildingMode();
            EquipToolFromItem(_activeItem);
            return;
        }

        DetachCurrentWeapon();

        if (_activeItem != null && _activeItem.IsPlaceableItem())
        {
            _buildingSystem?.SetBuildingTargetForPlayer(_activeItem.Prefab, PlayerId);
            return;
        }

        _buildingSystem?.CancelBuildingMode();
    }

    private void EquipToolFromItem(ItemData item)
    {
        if (item == null || item.EquipScene == null || WeaponHolder == null)
        {
            DetachCurrentWeapon();
            return;
        }

        if (_currentWeapon != null && _equippedItemId == item.Id && IsInstanceValid(_currentWeapon))
        {
            _currentWeapon.Position = item.EquipOffset;
            _currentWeapon.ConfigureFromItem(item);
            return;
        }

        DetachCurrentWeapon();

        Node instance = item.EquipScene.Instantiate();
        if (instance is not Weapon weapon)
        {
            GD.PushError($"Player: 物品 {item.Id} 的 EquipScene 不是 Weapon");
            instance.QueueFree();
            return;
        }

        WeaponHolder.AddChild(weapon);
        weapon.Position = item.EquipOffset;
        weapon.ConfigureFromItem(item);
        weapon.AttackFinished += OnWeaponAttackFinished;
        _currentWeapon = weapon;
        _equippedItemId = item.Id;
    }

    private void DetachCurrentWeapon()
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.AttackFinished -= OnWeaponAttackFinished;
            _currentWeapon.QueueFree();
            _currentWeapon = null;
        }

        _equippedItemId = string.Empty;
    }

    private void OnInventoryUpdated()
    {
        if (_inventory == null)
        {
            return;
        }

        if (_inventory.Slots.Count == 0)
        {
            _activeSlotIndex = -1;
        }
        else if (_activeSlotIndex < 0)
        {
            _activeSlotIndex = 0;
        }
        else if (_activeSlotIndex >= _inventory.Slots.Count)
        {
            _activeSlotIndex = _inventory.Slots.Count - 1;
        }

        RefreshActiveItemState();
    }

    private void OnObjectPlaced(PackedScene scene, Vector2 position)
    {
        if (_inventory == null || _activeItem == null || scene == null)
        {
            return;
        }

        if (!_activeItem.IsPlaceableItem() || _activeItem.Prefab != scene)
        {
            return;
        }

        int removed = _inventory.RemoveItem(_activeItem, 1);
        if (removed <= 0)
        {
            return;
        }

        if (_inventory.GetItemCount(_activeItem) > 0)
        {
            RefreshActiveItemState();
            return;
        }

        int nextSlot = FindFirstOccupiedSlot();
        if (nextSlot < 0)
        {
            nextSlot = 0;
        }

        SetActiveSlot(nextSlot, emitSignals: true);
    }

    private int FindFirstOccupiedSlot()
    {
        if (_inventory == null)
        {
            return -1;
        }

        for (int i = 0; i < _inventory.Slots.Count; i++)
        {
            if (!_inventory.Slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

}