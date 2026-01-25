using Godot;
using System;

public partial class Robot0 : Enemy
{
	[Export] private Vector2[] _directions = new Vector2[] {
		new Vector2(1, 0.5f).Normalized(),   // 右下
		new Vector2(-1, 0.5f).Normalized(),  // 左下
		new Vector2(1, -0.5f).Normalized(),  // 右上
		new Vector2(-1, -0.5f).Normalized()  // 左上
	};

	private Vector2 _currentDir;
	private Random _random = new Random();

	[Export] private RayCast2D _rayCast;
	[Export] private Timer _moveTimer;
	private bool isMoving = false;
	
	public override void _Ready()
	{
		base._Ready(); // 调用基类初始化
		
		PickRandomDirection();
		
		// 绑定计时器，每隔几秒换个方向
		if (_moveTimer != null)
		{
			_moveTimer.Timeout += PickRandomDirection;
		}
		else
		{
			_moveTimer = GetNodeOrNull<Timer>("MoveTimer");
			if (_moveTimer != null)
			{
				_moveTimer.Timeout += PickRandomDirection;
			}
		}

		// 自动查找组件
		if (_rayCast == null)
			_rayCast = GetNodeOrNull<RayCast2D>("RayCast2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta); // 调用基类逻辑
		
		// 1. 避障检测：如果前方有墙
		if (_rayCast != null && _rayCast.IsColliding() && isMoving)
		{
			PickRandomDirection();
		}

		// 2. 移动
		if(isMoving)
		{
			Velocity = _currentDir * Speed;
		}
		else
		{
			Velocity = Vector2.Zero;
		}
		
		// 3. 处理方向贴图和翻转
		UpdateAnimation();
		
		// 碰撞滑动处理
		if (GetSlideCollisionCount() > 0)
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
		
		if(isMoving)
		{
			isMoving = _random.Next(0, 10) > 3;
		}
		else
		{
			isMoving = true;
		}
		
		if (_moveTimer != null)
		{
			_moveTimer.WaitTime = (float)_random.NextDouble() * 2.0f + 2.0f;
		}
		
		// 让探测器的方向指向移动方向
		if (_rayCast != null)
		{
			_rayCast.TargetPosition = _currentDir * 40; 
		}
	}

	private void UpdateAnimation()
	{
		if (_animationController != null)
		{
			// 使用统一方法，根据 velocity.Y 自动判断方向，自动翻转
			_animationController.UpdateAnimation(Velocity);
		}
	}
}
