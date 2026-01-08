using Godot;
using System;

// 关键点 1: 继承 TranslucentObstacle 而不是 Node2D/Area2D
// 关键点 2: 实现 IDamageable 接口
public partial class Tree : TranslucentObstacle, IDamageable
{
    [ExportGroup("Tree Properties")]
    [Export] public int MaxHealth = 30;             // 树的血量（比如砍3下倒）
    
    [ExportGroup("Drops")]
    [Export] public PackedScene DropItemScene;     // 拖入你之前做的 WoodDrop.tscn
    [Export] public int DropCount = 3;             // 掉落数量

    private int _currentHealth;

    public override void _Ready()
    {
        // 关键点 3: 必须调用基类的 _Ready，否则半透明逻辑可能会失效！
        base._Ready();
        
        _currentHealth = MaxHealth;
    }

    // 接口方法的具体实现
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        GD.Print($"当前血量: {_currentHealth}");
        
        // 视觉反馈：简单的受击闪白或抖动 (Juice!)
        PlayHitEffect();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect()
    {
        // 创建一个简单的抖动动画
        Tween tween = CreateTween();
        // 向右歪一点 -> 向左歪一点 -> 回正
        tween.TweenProperty(this, "rotation_degrees", 5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees", -5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees", 0.0f, 0.05f);
        
        // 如果想变色提示受击，也可以加 Modulate 闪烁
        // tween.TweenProperty(this, "modulate", Colors.Red, 0.1f);
        // tween.TweenProperty(this, "modulate", Colors.White, 0.1f);
    }

    private void Die(){
        if (DropItemScene != null)
        {
            // 获取当前场景根节点
            var rootNode = GetTree().CurrentScene;

            for (int i = 0; i < DropCount; i++)
            {
                // 1. 实例化
                Node2D drop = DropItemScene.Instantiate<Node2D>();
                
                // 2. 设置位置
                Vector2 randomOffset = new Vector2(GD.Randf() * 10 - 5, GD.Randf() * 10 - 5);
                drop.GlobalPosition = this.GlobalPosition + randomOffset;
                
                // 3. 添加到场景 (Godot 4 C# 标准写法)
                rootNode.CallDeferred(Node.MethodName.AddChild, drop);
            }
        }
        else
        {
            GD.PrintErr("错误：DropItemScene 没拖进去！树虽然死了但没掉东西。");
        }

        QueueFree();
}
}