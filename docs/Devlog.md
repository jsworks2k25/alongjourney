# 开发日志 Development Log

## 时间线 / Development Timeline

### 灵感与策划：
#### 2025-06-09
Document drafts created.
#### 2025-06-16
整理了所玩过的游戏的机制，讨论了游戏玩法设计灵感，搭建项目制作环境。
Organized the mechanics of the games played, discussed gameplay design inspirations, and built the project production environment.
#### 2025-07-03
敲定了游戏的雏形，制定游戏最初机制与目标。更新了游戏设计文档，设定了游戏类型，操作方式以及反馈循环机制。
The prototype of the game was finalized to develop the initial mechanics and objectives of the game. Updated the game design document to set up the game type, how it works, and the feedback loop mechanism.
### 原型阶段：
#### 12/19/2025
物体碰撞，前后遮挡关系，玩家从后方经过障碍物时透明

### 测试与迭代阶段：

#### 2026-05-13
动画朝向与 move/idle 选片改为跟随黑板上的移动意图（`MoveDirection` / `InputVector`），不再用实际 `Velocity`，击退时位移可与贴图朝向解耦。  
Locomotion facing and move vs idle clips now follow blackboard move intent (`MoveDirection` / `InputVector`), not `Velocity`, so knockback motion can diverge from sprite facing.

---
