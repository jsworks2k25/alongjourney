using Godot;

public interface IDamageable
{
	void TakeDamage(int damage, Vector2? sourcePosition = null);
}
