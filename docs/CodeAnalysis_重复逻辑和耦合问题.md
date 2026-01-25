# 代码分析报告：逻辑重复和耦合问题

## 1. 代码重复问题

### 1.1 FindActorOwner 方法重复
**位置：**
- `BaseComponent.cs` (37-46行)
- `HurtboxComponent.cs` (30-39行)

**问题：** 两个类中实现了完全相同的 `FindActorOwner()` 方法

**建议：** 
- `HurtboxComponent` 应该继承 `BaseComponent` 或使用其 `Owner` 属性
- 或者将 `FindActorOwner` 提取为静态工具方法

---

### 1.2 GetPlayerGroupName 方法重复
**位置：**
- `Player.cs` (92-95行)
- `GameManager.cs` (56-59行)
- 内联使用：`Enemy.cs`, `WoodDrop.cs`, `TranslucentObstacle.cs`

**问题：** 多个地方重复实现获取玩家组名的逻辑

**建议：**
- 在 `GameConfig` 中添加静态辅助方法：`GetPlayerGroupName()`
- 或者创建 `GameConfigHelper` 工具类

---

### 1.3 UpdateAnimation 方法重复
**位置：**
- `Ghost.cs` (84-91行)
- `Robot0.cs` (109-116行)

**问题：** 两个类中实现了完全相同的 `UpdateAnimation()` 方法

**建议：**
- 这两个类都继承自 `Enemy`，可以在 `Enemy` 基类中实现此方法
- 或者通过 `AnimationController` 组件自动处理（已有部分实现）

---

### 1.4 玩家组名检查逻辑重复
**位置：**
- `WoodDrop.cs` (73-74行)
- `TranslucentObstacle.cs` (32-33行, 41-42行)
- `Enemy.cs` (37行)

**问题：** 多个地方重复检查 `body.IsInGroup(groupName) || body is ITargetable`

**建议：**
- 创建扩展方法：`IsPlayer(this Node2D body)`
- 或者创建工具类方法：`PlayerHelper.IsPlayer(Node2D body)`

---

### 1.5 GameConfig 空值检查模式重复
**位置：** 几乎所有使用 `GameConfig.Instance` 的地方

**问题：** 大量重复的 `GameConfig.Instance != null ? GameConfig.Instance.XXX : defaultValue` 模式

**建议：**
- 在 `GameConfig` 中添加空值安全的属性访问器
- 或者创建扩展方法：`GameConfig.GetValue<T>(Func<GameConfig, T> getter, T defaultValue)`

**示例：**
```csharp
// 在 GameConfig 中添加
public static string GetPlayerGroupName() => Instance?.PlayerGroupName ?? "Player";
public static float GetItemMagnetSpeed(float defaultValue) => Instance?.ItemMagnetSpeed ?? defaultValue;
```

---

### 1.6 动画名称获取逻辑重复
**位置：**
- `Axe.cs` (16行, 45行)

**问题：** 重复获取动画名称：`GameConfig.Instance != null ? GameConfig.Instance.AnimSwing : "swing"`

**建议：**
- 在 `GameConfig` 中添加辅助方法：`GetAnimSwing()`

---

## 2. 耦合问题

### 2.1 速度处理冲突
**位置：**
- `Actor._PhysicsProcess` (86-90行)：Attack 状态时处理速度
- `HitEffectComponent._PhysicsProcess` (64-79行)：Stagger 状态时处理击退速度

**问题：** 
- `HitEffectComponent` 直接修改 `_characterBody.Velocity`
- `Actor` 也在 `_PhysicsProcess` 中处理速度
- 可能导致速度被覆盖或冲突

**建议：**
- `HitEffectComponent` 应该通过 Actor 的黑板系统或信号来请求速度变化
- 或者统一在 `Actor._PhysicsProcess` 中处理所有速度逻辑
- 考虑使用优先级系统：Stagger > Attack > Normal

---

### 2.2 Actor 与 HitEffectComponent 的耦合
**位置：**
- `Actor.cs` (74行)：订阅 `HitEffectComponent.OnStaggerEnded`
- `HitEffectComponent.cs` (76行)：触发 `OnStaggerEnded` 事件

**问题：** Actor 需要知道 HitEffectComponent 的存在和接口

**建议：**
- 通过黑板系统解耦：HitEffectComponent 设置黑板值，Actor 监听黑板变化
- 或者使用信号系统，但不直接依赖组件类型

---

### 2.3 组件查找路径硬编码
**位置：**
- `Actor.cs` (55-64行)：多个 `GetNodeOrNull` 调用，尝试多个路径
- `MovementComponent.cs` (16-17行)：类似的路径查找

**问题：** 组件查找逻辑分散且重复

**建议：**
- 创建统一的组件查找辅助方法
- 或者使用依赖注入模式

---

### 2.4 Enemy 子类与 AnimationController 的直接耦合
**位置：**
- `Ghost.cs` (86行)：直接访问 `_animationController`
- `Robot0.cs` (111行)：直接访问 `_animationController`

**问题：** 
- `_animationController` 是 `Actor` 的 protected 字段
- 子类直接访问父类的组件引用，增加了耦合

**建议：**
- 通过黑板系统：子类设置移动方向，`AnimationController` 自动响应
- 或者将动画更新逻辑移到 `Enemy` 基类

---

### 2.5 HealthComponent 与 Actor 的双向依赖
**位置：**
- `Actor.cs` (68-69行)：订阅 `HealthComponent` 事件
- `HealthComponent.cs` (53-56行)：调用 `Owner.RequestDamage`

**问题：** 循环依赖：Actor 依赖 HealthComponent，HealthComponent 依赖 Actor

**建议：**
- 通过黑板系统完全解耦：HealthComponent 只读取/写入黑板，不直接调用 Actor 方法
- Actor 监听黑板变化并响应

---

## 3. 架构改进建议

### 3.1 创建工具类
建议创建以下工具类来减少重复：

```csharp
// GameConfigHelper.cs
public static class GameConfigHelper
{
    public static string GetPlayerGroupName() => 
        GameConfig.Instance?.PlayerGroupName ?? "Player";
    
    public static bool IsPlayer(Node2D body) =>
        body.IsInGroup(GetPlayerGroupName()) || body is ITargetable;
}

// ActorHelper.cs
public static class ActorHelper
{
    public static Actor FindActorOwner(Node node)
    {
        Node current = node.GetParent();
        while (current != null && !(current is Actor))
        {
            current = current.GetParent();
        }
        return current as Actor;
    }
}
```

### 3.2 统一速度处理
建议在 `Actor._PhysicsProcess` 中统一处理所有速度逻辑：

```csharp
public override void _PhysicsProcess(double delta)
{
    if (!IsAlive) return;
    
    // 优先级：Stagger > Attack > Normal
    if (CurrentState == Actor.ActorState.Stagger)
    {
        // HitEffectComponent 通过黑板控制速度
        // 这里只读取，不修改
    }
    else if (CurrentState == ActorState.Attack)
    {
        float friction = GameConfig.Instance?.KnockbackFriction ?? 600f;
        Velocity = Velocity.MoveToward(Vector2.Zero, friction * (float)delta);
    }
    // Normal 状态由 MovementComponent 处理
    
    MoveAndSlide();
    SetBlackboardValueIfChanged(KeyVelocity, Velocity);
}
```

### 3.3 通过黑板系统解耦
建议所有组件间通信都通过黑板系统：

- HealthComponent → 写入 `KeyDamagePending`
- HitEffectComponent → 读取 `KeyHitPending`，写入速度到黑板
- MovementComponent → 读取 `KeyMoveDirection`
- AnimationController → 读取 `KeyVelocity`

---

## 4. 优先级修复建议

### 高优先级（立即修复）
1. ✅ 提取 `FindActorOwner` 为工具方法
2. ✅ 提取 `GetPlayerGroupName` 到 `GameConfig`
3. ✅ 修复速度处理冲突（Actor vs HitEffectComponent）

### 中优先级（近期修复）
4. ✅ 提取 `UpdateAnimation` 到 `Enemy` 基类
5. ✅ 创建 `IsPlayer` 扩展方法
6. ✅ 统一 GameConfig 空值检查模式

### 低优先级（优化）
7. ✅ 重构组件查找逻辑
8. ✅ 通过黑板系统进一步解耦组件
