using Godot;

public partial class HealthComponent : BaseComponent
{
    [Signal]
    public delegate void HealthChangedEventHandler(int newHealth, int maxHealth, Vector2 sourcePosition);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth = 100;

    private int _currentHealth;
    private bool _isDead = false;

    // 使用一个特殊值来表示"没有攻击者位置"
    public static readonly Vector2 NoSourcePosition = new Vector2(float.NaN, float.NaN);

    public override void Initialize()
    {
        _currentHealth = MaxHealth;
        _isDead = false;
        WriteToBlackboard();
    }

    protected override void OnOwnerBlackboardChanged(string key, Variant value)
    {
        if (Owner == null)
        {
            return;
        }

        if (key != Actor.KeyDamagePending || !value.AsBool())
        {
            return;
        }

        int amount = Owner.GetBlackboardInt(Actor.KeyDamageAmount, 0);
        Vector2 source = Owner.GetBlackboardVector(Actor.KeyDamageSource, NoSourcePosition);
        if (amount > 0)
        {
            ApplyDamage(amount, source);
        }

        Owner.SetBlackboardValue(Actor.KeyDamagePending, false);
        Owner.SetBlackboardValue(Actor.KeyDamageAmount, 0);
        Owner.SetBlackboardValue(Actor.KeyDamageSource, NoSourcePosition);
    }

    public void TakeDamage(int amount, Vector2? sourcePosition = null)
    {
        if (Owner != null)
        {
            Owner.RequestDamage(amount, sourcePosition);
            return;
        }

        ApplyDamage(amount, sourcePosition ?? NoSourcePosition);
    }

    private void ApplyDamage(int amount, Vector2 sourcePosition)
    {
        // 如果已经死亡，不再处理伤害
        if (_isDead || amount <= 0)
        {
            return;
        }

        _currentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth, sourcePosition);

        if (_currentHealth <= 0 && !_isDead)
        {
            _isDead = true;
            EmitSignal(SignalName.Died);
        }

        WriteToBlackboard();
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

        WriteToBlackboard();
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

        WriteToBlackboard();
    }

    /// <summary>
    /// 获取当前血量
    /// </summary>
    public int CurrentHealth => _currentHealth;

    private void WriteToBlackboard()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.SetBlackboardValue(Actor.KeyCurrentHealth, _currentHealth);
        Owner.SetBlackboardValue(Actor.KeyMaxHealth, MaxHealth);
        Owner.SetBlackboardValue(Actor.KeyIsDead, _isDead);
    }
}