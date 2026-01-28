namespace AlongJourney.Entities.Player;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Entities.Items;
using AlongJourney.Entities.Player.States;
using AlongJourney.Core;
using AlongJourney.Components;

public partial class Player : Actor
{
    [Signal]
    public delegate void PlayerDiedEventHandler(Player player);

    private Weapon _currentWeapon;

    [Export] public Marker2D WeaponHolder;

    [ExportGroup("References")]
    [Export] private SelectionManager _selectionManager;

    public override void _Ready()
    {
        base._Ready();

        // 添加到 Player 组
        AddToGroup(GameConfig.GetPlayerGroupName());

        // 订阅 HealthComponent 信号
        if (HealthComponent != null)
        {
            HealthComponent.Died += HandleDied;
            HealthComponent.HealthChanged += HandleHealthChanged;
        }

        if (WeaponHolder != null && WeaponHolder.GetChildCount() > 0)
        {
            _currentWeapon = WeaponHolder.GetChild<Weapon>(0);
            _currentWeapon.AttackFinished += OnWeaponAttackFinished;
        }
    }

    private SelectionManager GetSelectionManager()
    {
        if (_selectionManager != null && IsInstanceValid(_selectionManager) && _selectionManager.IsInsideTree())
        {
            return _selectionManager;
        }

        return null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        HandleWeaponAiming();

        if (Input.IsActionJustPressed("attack"))
        {
            TryStartAttack();
        }

        // MovementComponent 和 PlayerInputComponent 会自动处理移动
        base._PhysicsProcess(delta);
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

    private void TryStartAttack()
    {
        if (_currentWeapon == null)
        {
            return;
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
                        return;
                    }
                }
            }
        }

        // 如果没有选中目标或攻击失败，回退到鼠标方向攻击
        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        if (_currentWeapon.Attack(mouseDir))
        {
            SetBlackboardValue(Actor.BlackboardKeys.IsAttacking, true);
        }
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

}