using Godot;
using System;

public partial class Player : CharacterBody2D
{

	[Export] private AnimationPlayer _animPlayer;
	[Export] private Marker2D _attackPivot;
	[Export] private Area2D _weaponArea;
	[Export] private Sprite2D _slashSprite;
	private bool _isAttacking = false;
	public int Speed { get; set; } = 150;
	private AnimatedSprite2D body;
	private Vector2 isoVec = new Vector2(1f, 0.5f);
	private bool lastFacingUp = false; // 用于静止时判断上/下朝向

	public override void _Ready()
	{
		_slashSprite.Visible = false;
		body = GetNode<AnimatedSprite2D>("Body");
		_weaponArea.Monitoring = false;

		_weaponArea.AreaEntered += _on_weapon_area_area_entered;
		
		_animPlayer.AnimationFinished += (animName) => {
			if (animName == "meleeAttack") _isAttacking = false;
		};
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
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("attack") && !_isAttacking)
		{
			ExecuteAttack();
		}
	}
	private void ExecuteAttack()
	{
		_isAttacking = true;

		// 1. 计算鼠标方向
		Vector2 mousePos = GetGlobalMousePosition();
		Vector2 dir = (mousePos - GlobalPosition).Normalized();

		// 2. 将角度锁定到 45 度的倍数 (PI / 4)
		float angle = dir.Angle();
		float snappedAngle = Mathf.Snapped(angle, Mathf.Pi / 4.0f);

		// 3. 旋转 Pivot 节点
		_weaponArea.Rotation = snappedAngle - (Mathf.Pi / 2.0f);

		// 5. 播放动画
		_animPlayer.Play("meleeAttack");
	}
	
	private void _on_weapon_area_area_entered(Area2D area)
	{
		// area 是进入的那个受击区 (Hurtbox)
		// 我们要通过 Hurtbox 找到它的父节点，即 Enemy 脚本所在的地方
		Node victim = area.Owner; // 获取场景的拥有者

		if (victim is IDamageable enemy)
		{
			enemy.TakeDamage(10);
		}
	}
}
