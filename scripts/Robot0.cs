using Godot;
using System;

public partial class Robot0 : CharacterBody2D, IDamageable
{
	[Export] public float Speed = 100.0f;
	[Export] public int Health = 30;

	private Vector2[] _directions = new Vector2[] {
		new Vector2(1, 0.5f).Normalized(),   // 右下
		new Vector2(-1, 0.5f).Normalized(),  // 左下
		new Vector2(1, -0.5f).Normalized(),  // 右上
		new Vector2(-1, -0.5f).Normalized()  // 左上
	};

	private Vector2 _currentDir;
	private Random _random = new Random();

	[Export] private Sprite2D _sprite;
	[Export] private RayCast2D _rayCast;
	[Export] private Timer _moveTimer;
	[Export] private AnimationPlayer _animPlayer;

	public override void _Ready()
	{
		PickRandomDirection();
		
		// 绑定计时器，每隔几秒换个方向
		_moveTimer.Timeout += PickRandomDirection;
	}

	public override void _PhysicsProcess(double delta)
	{
		bool collided = MoveAndSlide();
		// 1. 避障检测：如果前方有墙
		if (_rayCast.IsColliding())
		{
			PickRandomDirection();
		}

		// 2. 移动
		Velocity = _currentDir * Speed;
		MoveAndSlide();

		// 3. 处理方向贴图和翻转
		UpdateAnimation();
		
		if (collided)
		{
			var collision = GetSlideCollision(0);
			// collision.GetNormal() 会给你一个垂直于碰撞表面的向量（指向外面）
			// 我们让方向沿着墙面（或玩家表面）滑动
			_currentDir = _currentDir.Slide(collision.GetNormal()).Normalized();
		}
	}

	private void PickRandomDirection()
	{
		int index = _random.Next(0, _directions.Length);
		_currentDir = _directions[index];
		
		// 让探测器的方向指向移动方向
		_rayCast.TargetPosition = _currentDir * 40; 
	}

	private void UpdateAnimation()
	{
		string state = Velocity.Length() > 0.1f ? "move" : "idle";
		string direction = "";
		bool flip = false;

		// 根据速度向量判断 4 个斜向0
		// 等距视角 4 方向：(1, 0.5), (-1, 0.5), (1, -0.5), (-1, -0.5)
		
		if (Velocity.Y >= 0) // 向下系列 (右下、左下)
		{
			direction = "down";
			// 如果 X < 0，说明是左下，我们需要翻转“右下”的贴图
			flip = Velocity.X < 0;
		}
		else // 向上系列 (右上、左上)
		{
			direction = "up";
			// 如果 X > 0，说明是右上，我们需要翻转“左上”的贴图
			flip = Velocity.X > 0;
		}

		// 拼接动画名字，例如 "idle_down" 或 "move_up"
		string animName = $"{state}_{direction}";

		// 执行播放和翻转
		if (_animPlayer.CurrentAnimation != animName)
		{
			_animPlayer.Play(animName);
		}
		_sprite.FlipH = flip;
		string dir = Velocity.Y >= 0 ? "down" : "up";
	
		_sprite.FlipH = (Velocity.Y >= 0 && Velocity.X < 0) || (Velocity.Y < 0 && Velocity.X > 0);
	
		_animPlayer.Play($"{state}_{dir}");
	}

	// 供玩家调用的受击函数
	public void TakeDamage(int damage)
	{
		Health -= damage;

		// 简单的受击反馈：闪红光
		var tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate", Colors.Red, 0.1f);
		tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1f);

		if (Health <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		QueueFree();
	}

}
