using Godot;
using System;

public interface IDamageable
{
	// 这里只定义“能被打”的规格，不需要写具体代码
	void TakeDamage(int damage);
}
