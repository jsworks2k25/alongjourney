using Godot;

// 这是一个专门用来"挨打"的区域
public partial class HurtboxComponent : Area2D, IDamageable
{
    [Export]
    public HealthComponent HealthComponent; // 依赖注入：把第一步的血条组件拖进来

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {
        if (HealthComponent != null)
        {
            HealthComponent.TakeDamage(amount, sourcePosition);
            // 这里还可以加受击特效，比如播放闪白 Shader
        }
    }
}