namespace AlongJourney.Entities.States;

using Godot;
using AlongJourney.Core;
using AlongJourney.Entities;

public partial class DeadState : State
{
    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.Velocity = Vector2.Zero;
        Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);

        Owner.SetCollisionEnabled(false);
        Owner.SetHurtboxEnabled(false);
    }

    public override void Update(double delta)
    {
    }
}
