using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public int Speed { get; set; } = 100; 
    public Vector2 isoVec = new Vector2(1f, 0.5f);   // 等距移动方向缩放
    private AnimationPlayer _anim;    // 动画组件

    public override void _Ready()
    {
        // 获取 AnimationPlayer 节点（请确保你的节点名是 "AnimationPlayer"）
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public void GetInput()
    {
        Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
        // 移动向量（加入等距系数）
        Velocity = inputDirection * Speed * isoVec;
        // 动画控制
        UpdateAnimation(inputDirection);
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndSlide();
    }

    private void UpdateAnimation(Vector2 input)
    {
        // 没输入则播放 idle
        if (input == Vector2.Zero)
        {
            _anim.Play("idle");
            return;
        }

        // 判断主方向（x 或 y）
        if (Mathf.Abs(input.X) > Mathf.Abs(input.Y))
        {
            if (input.X > 0)
                _anim.Play("walk_right");
            else
                _anim.Play("walk_left");
        }
        else
        {
            if (input.Y > 0)
                _anim.Play("walk_down");
            else
                _anim.Play("walk_up");
        }
    }
}
