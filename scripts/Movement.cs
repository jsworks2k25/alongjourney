using Godot;
using System;

public partial class Movement : CharacterBody2D
{
	[Export]
	public int Speed { get; set; } = 100;

	private AnimatedSprite2D body;
	private Vector2 isoVec = new Vector2(1f, 0.5f);
	private bool lastFacingUp = false; // 用于静止时判断上/下朝向

	public override void _Ready()
	{
		body = GetNode<AnimatedSprite2D>("Body");
	}

	public void GetInput()
	{
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Velocity = input * Speed * isoVec;

		// --- 动画播放逻辑 ---
		if (input == Vector2.Zero)
		{
			// 静止状态：根据上一次方向选择 idle / idle_rear
			if (lastFacingUp)
				body.Play("idle_rear");
			else
				body.Play("idle");
		}
		else
		{
			// 有输入：播放前/后动画
			if (input.Y < 0)
			{
				body.Play("run_rear"); // 朝上
				lastFacingUp = true;
			}
			else
			{
				body.Play("run"); // 朝下
				lastFacingUp = false;
			}
			// 左右翻转
			if (input.X != 0)
				body.FlipH = input.X < 0;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		MoveAndSlide();
	}
}
