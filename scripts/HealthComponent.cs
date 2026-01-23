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
    }

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {
        _currentHealth -= amount;
        Vector2 sourcePos = sourcePosition ?? NoSourcePosition;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth, sourcePos);

        if (_currentHealth <= 0)
        {
            EmitSignal(SignalName.Died);
        }
    }
}