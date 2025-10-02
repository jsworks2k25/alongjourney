using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export] public int Speed = 200;

    private AnimationPlayer _anim;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Vector2.Zero;

        if (Input.IsActionPressed("right"))
            velocity.X += 1;
        if (Input.IsActionPressed("left"))
            velocity.X -= 1;
        if (Input.IsActionPressed("up"))
            velocity.Y += 0.5f;
        if (Input.IsActionPressed("down"))
            velocity.Y -= 0.5f;

        velocity = velocity.Normalized() * Speed;
        Velocity = velocity;
        MoveAndSlide();

        UpdateAnimation(velocity);
    }

    private void UpdateAnimation(Vector2 velocity)
    {
        if (velocity.Length() > 0)
        {
            if (Math.Abs(velocity.X) > Math.Abs(velocity.Y))
            {
                if (velocity.X > 0)
                    _anim.Play("walk_right");
                else
                    _anim.Play("walk_left");
            }
            else
            {
                if (velocity.Y > 0)
                    _anim.Play("walk_down");
                else
                    _anim.Play("walk_up");
            }
        }
        else
        {
            _anim.Play("idle");
        }
    }
}
