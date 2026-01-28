# Project Structure Summary | 项目文件系统分类总结

本文档展示了 `alongjourney` 项目的文件系统分类结构，旨在帮助开发者快速了解项目布局。

## 📂 核心目录结构

```text
alongjourney/
├── 📂 .cursor/             # AI 辅助配置与工作区元数据
├── 📂 addons/              # 第三方插件 (AsepriteWizard, PhantomCamera 等)
├── 📂 Components/          # 原子化逻辑组件 (C#)
│   ├── MovementComponent.cs
│   ├── HealthComponent.cs
│   └── ... (Hitbox, Hurtbox, Knockback, etc.)
├── 📂 Core/                # 游戏核心框架与全局管理
│   ├── GameManager.cs      # 游戏主控制
│   ├── StateMachine.cs     # 状态机基类
│   └── SelectionManager.cs # 选择管理
├── 📂 docs/                # 项目文档 (GDD, Devlog, Reflection)
├── 📂 Entities/            # 游戏实体 (场景 + 脚本)
│   ├── 📂 Player/          # 玩家相关 (Player.tscn, States)
│   ├── 📂 Enemies/         # 敌人相关 (Ghost, Robot)
│   ├── 📂 Items/           # 物品与掉落物 (Axe, ItemDrop)
│   └── 📂 Environment/     # 环境装饰 (Tree, Rock)
├── 📂 Interfaces/          # C# 接口定义 (IDamageable, IInteractable)
├── 📂 Resources/           # Godot 资源文件 (.tres, .gd)
│   └── 📂 Items/           # 物品数据库与数据定义
├── 📂 Sprites/             # 美术素材 (PNG, JSON, Import)
├── 📂 UI/                  # 交互
└── 📂 Shader, Music.../    # 更多资产类型
```

## 🛠 分类逻辑说明

| 分类 | 说明 | 包含内容 |
| :--- | :--- | :--- |
| **Components** | **逻辑组件** | 纯逻辑脚本，可挂载到不同实体上实现特定功能。 |
| **Core** | **系统核心** | 驱动游戏运行的基础设施，如状态机框架、全局管理器。 |
| **Entities** | **实体表现** | 游戏世界中的具体对象，通常包含 `.tscn` 场景和特定的控制脚本。 |
| **Interfaces** | **交互协议** | 定义对象间如何通信（如：如何被伤害、如何被交互）。 |
| **Resources** | **数据驱动** | 存储静态数据（如物品属性、数据库配置）。 |
| **Sprites** | **视觉资产** | 所有的图片素材及 Aseprite 导出的动画配置。 |

## 🚀 开发建议
- **保持组件化**：新的通用逻辑应优先考虑放入 `Components/`。
- **场景脚本同行**：建议继续保持 `.tscn` 与其主要控制脚本 `.cs` 放在同一个 `Entities/` 子目录下的习惯。
- **资源引用**：`.tres` 文件应统一管理在 `Resources/` 下，避免散落在 `Entities/` 中。
