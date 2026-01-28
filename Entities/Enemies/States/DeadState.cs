namespace AlongJourney.Entities.Enemies.States;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Core;

public partial class DeadState : State
{
    public override void Enter()
    {
        if (Owner == null)
        {
            return;
        }

        // 停止所有移动
        Owner.Velocity = Vector2.Zero;
        Owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.InputVector, Vector2.Zero);
        Owner.SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);
        
        // 禁用碰撞和受击盒
        Owner.SetCollisionEnabled(false);
        Owner.SetHurtboxEnabled(false);
    }

    public override void Update(double delta)
    {
        // 死亡状态不处理任何逻辑
    }
}
