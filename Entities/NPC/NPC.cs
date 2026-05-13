namespace AlongJourney.Entities.NPC;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Entities.NPC.States;

public partial class NPC : Actor
{
    [Export] public float Speed = 50f;
    private ChaseTargetMovement _chaseMovement;

    public override void _Ready()
    {
        base._Ready();

        _chaseMovement = GetNodeOrNull<ChaseTargetMovement>("ChaseTargetMovement");

        if (HealthComponent != null)
        {
            HealthComponent.Died += OnHealthDied;
            HealthComponent.HealthChanged += OnHealthChanged;
        }

        SetBlackboardValue(Actor.BlackboardKeys.MoveSpeed, Speed);
    }

    public override void _PhysicsProcess(double delta)
    {
        _chaseMovement?.PhysicsTick(delta);
        base._PhysicsProcess(delta);
    }

    private void OnHealthDied() => OnNpcHealthDied();

    private void OnHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition) =>
        OnNpcHealthChanged(currentHp, maxHp, sourcePosition);

    /// <summary>
    /// Override for custom death teardown. Default: dead state + delayed free.
    /// </summary>
    protected virtual void OnNpcHealthDied()
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);
        RequestStateChange<DeadState>();
        GetTree().CreateTimer(0.5f).Timeout += QueueFree;
    }

    /// <summary>
    /// Override to react to HP changes (e.g. stagger, VFX). Base does nothing.
    /// </summary>
    protected virtual void OnNpcHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
    }
}
