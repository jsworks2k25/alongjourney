using Godot;
using System;

public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(int newHealth, int maxHealth, Vector2 sourcePosition);
    
    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth = 100;

    private int _currentHealth;
    
    // 使用一个特殊值来表示"没有攻击者位置"
    public static readonly Vector2 NoSourcePosition = new Vector2(float.NaN, float.NaN);

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _isDead = false;
    }

    private bool _isDead = false;

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {
        // 如果已经死亡，不再处理伤害
        if (_isDead) return;

        _currentHealth -= amount;
        Vector2 sourcePos = sourcePosition ?? NoSourcePosition;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth, sourcePos);

        if (_currentHealth <= 0 && !_isDead)
        {
            _isDead = true;
            EmitSignal(SignalName.Died);
        }
    }

    /// <summary>
    /// 恢复血量
    /// </summary>
    public void Heal(int amount)
    {
        int oldHealth = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
        
        // 如果血量恢复到 > 0，重置死亡状态
        if (_isDead && _currentHealth > 0)
        {
            _isDead = false;
        }
        
        // 如果血量有变化，发送信号
        if (_currentHealth != oldHealth)
        {
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth, NoSourcePosition);
        }
    }

    /// <summary>
    /// 完全恢复血量到最大值
    /// </summary>
    public void FullHeal()
    {
        _isDead = false; // 重置死亡状态
        if (_currentHealth != MaxHealth)
        {
            _currentHealth = MaxHealth;
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth, NoSourcePosition);
        }
    }

    /// <summary>
    /// 获取当前血量
    /// </summary>
    public int CurrentHealth => _currentHealth;
}