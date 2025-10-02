using Godot;
using System;


public partial class Movement : CharacterBody2D
{
    [Export]
    public int Speed { get; set; } = 100;
    public Vector2 isoVec = new Vector2(1f, 0.5f);

    public void GetInput()
    {        
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
        Velocity = inputDirection  * Speed * isoVec;
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndSlide();
    }
}