using Godot;
using System;

public interface IDamageable
{
	// 这里只定义"能被打"的规格，不需要写具体代码
	// sourcePosition: 攻击者的位置，用于计算击退方向（可选）
	void TakeDamage(int damage, Vector2? sourcePosition = null);
}
