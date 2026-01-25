using Godot;

public partial class MovementSmoothingComponent : Node
{
    [Export] public float Acceleration = 200f;
    [Export] public float Friction = 150f;

    public Vector2 Accelerate(Vector2 currentVelocity, Vector2 targetVelocity, float delta)
    {
        if (Acceleration <= 0f)
        {
            return targetVelocity;
        }

        return currentVelocity.MoveToward(targetVelocity, Acceleration * delta);
    }

    public Vector2 Brake(Vector2 currentVelocity, float delta)
    {
        if (Friction <= 0f)
        {
            return Vector2.Zero;
        }

        return currentVelocity.MoveToward(Vector2.Zero, Friction * delta);
    }
}
