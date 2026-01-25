using Godot;

public partial class Player : Actor
{
    [Signal]
    public delegate void PlayerDiedEventHandler(Player player);

    private Marker2D _weaponHolder;
    private Weapon _currentWeapon;

    public override void _Ready()
    {
        base._Ready();

        // 添加到 Player 组
        AddToGroup(GetPlayerGroupName());

        _weaponHolder = GetNodeOrNull<Marker2D>("WeaponPivot")
            ?? GetNodeOrNull<Marker2D>("WeaponHolder");

        if (_weaponHolder != null && _weaponHolder.GetChildCount() > 0)
        {
            _currentWeapon = _weaponHolder.GetChild<Weapon>(0);
            _currentWeapon.AttackFinished += OnWeaponAttackFinished;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // 死亡时不处理物理
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

    protected override void HandleDied()
    {
        if (CurrentState == ActorState.Dead)
        {
            return;
        }

        base.HandleDied();
        EmitSignal(SignalName.PlayerDied, this);
    }

    private void TryStartAttack()
    {
        if (_currentWeapon == null)
        {
            return;
        }

        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        if (_currentWeapon.Attack(mouseDir))
        {
            SetState(ActorState.Attack);
        }
    }

    private void HandleWeaponAiming()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        Vector2 mouseDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
        _weaponHolder.Rotation = mouseDir.Angle();
        _weaponHolder.Scale = new Vector2(1, mouseDir.X < 0 ? -1 : 1);
    }

    private void OnWeaponAttackFinished()
    {
        if (IsAlive)
        {
            SetState(ActorState.Normal);
        }
    }

    private string GetPlayerGroupName()
    {
        return GameConfig.Instance != null ? GameConfig.Instance.PlayerGroupName : "Player";
    }
}