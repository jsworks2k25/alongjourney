# Game Design Document

## 1\. Game Overview 

- **Game Name**:   
- **Genre**: Strategy, Battle, Rogue-like, Tower Defense, 2.5D  
- **Target Platform**: Mainly pc, mobile (optional);  multiplayer collaboration  
- **Core Idea / Philosophy Theme**: provokes players to think about the relationship between humans and AI.

## 2\. Gameplay Mechanics / Core Mechanics 

- **Controls**: Arrow keys, click   
- **Basic Operation**: Control character movement, 2.5D (probable), click/drag to place, control defense facilities to fight, click to shoot.  
- **Main Game Loop**:   
  Inter-games:   
- *Positive Feedback*: 

  Winning \- gain initial bonuses/build skill trees \- enhance in-game experience; Losing \- gain game experience/skills \- want to win more 

- *Negative Feedback*: 
  
  Reach goal \- create harder maps \- more challenging route design and combat 

  Intra-games: 

- *Positive Feedback*: 



- *Negative feedback*: 


- **Innovative mechanic**: An exceptionally intelligent AI with logical reasoning capabilities based on DEL and other frameworks, enabling realistic simulated interactions.

## 3\. Story & World 

- **World setting**: Post-apocalyptic, fighting against chaotic environment,   
- **Main characters and settings**:   
- **Storyline / direction**: 

## 4\. Level Design

- **Main level list and description**: Different maps, special terrains: resource-poor / high enemy-strength, optional   
- **Difficulty and pace design**:   
- **Interactive elements design**: 

## 5\. Art & Audio Visual style: 

- **Visual style**: different from traditional tower defense, combining meat pigeon and combat. Art & Audio   
- **Visual style and color palette**:   
- **Audio style / music reference**:   
- **Animation and transitions design (if any)**: 

## 6\. User Interface 

- **UI structure sketch**:   
- **HUD elements description**:   
- **Beginner’s guide flow**: 

## 7\. Technical Implementation

- **Project Architecture**:   
- **Modules and system division**:  
- **Plug-ins / External Resources Used**:

---

# 游戏设计文档

## 中文备份

## 1\. 游戏概述 / Game Overview

- 游戏名称：  
- 类型（Genre）：策略，战斗，肉鸽，塔防。  
- 目标平台：主要pc， 不排除移动端、多人  
- 核心理念 / 哲学主题：引发对人类与AI的思考。


## 2\. 玩法机制 / Core Mechanics

- 控制方式：方向键，点击  
- 基本操作：控制角色移动，2.5D（大概率），放置，建造，对话  
- 游戏主循环：  
  局外：  
  正反馈：胜利-获得初始加成/建造技能树-增强局内体验；失败-获得游戏经验/技巧-更想赢  
  负反馈：达成目标-生成更难的地图-对路线设计及战斗产生更高挑战  
  局内：  
  正反馈： 
  负反馈： 
- 创新机制：极为聪明的AI，拥有基于DEL等逻辑逻辑推理能力，将真实模拟互动

## 3\. 故事与世界观 / Story & World

- 世界背景设定：后启示录，与混乱的环境对抗，  
- 主要角色与设定：  
- 故事情节 / 走向：

## 4\. 关卡设计 / Level Design

- 主要关卡列表与描述：不同地图，特殊地形：资源贫瘠/敌人强度，可供选择，融合类箱庭的开放世界地图或地牢类房间拼接式
- 难度与节奏设计：  
- 互动要素设计：

## 5\. 美术与音效 / Art & Audio

- 视觉风格与色调：  
- 音效风格 / 音乐参考：  
- 动画与过场设计（如有）：

## 6\. 用户界面 / User Interface

- UI 结构草图：  
- HUD 元素说明：  
- 新手引导流程：

## 7\. 技术实现 / Technical Architecture

- 项目架构：  
- 模块与系统划分：  
- 使用的插件 / 外部资源：



#  新计划
地图设计类似超级放大版泰拉瑞亚，各种区域分化明显，有足够资源需求，并且难以每次从初始据点过去，需要建立新据点（以及可能修路设防），据点可以作为重生点，也可以造传送装置
不同区域大致按到出生点距离排难度，后期科技逐渐需要铁、石油之类的后期资源，从而产生探索需求
做一些生产元素，种田炼铁之类的，可以让npc帮忙，实现一定的自动化，这样可以产生一些中心据点的需求
npc可以是落单的或是其他阵营，落单的显然容易拉拢，其他阵营可以尝试合并、结盟、交易、或者屠了
可以藏一点小小的主线，有点碎片剧情，让故事更有趣合理，以及稍微推动玩家发展（但是最好别过头，留些选择空间）

**“领袖视角”的生存建设RPG**

传统的《环世界》更像是“上帝视角”，你下令，小人执行；而《星露谷》是“个人视角”，你亲自耕作。你们的切入点介于两者之间：**玩家是基建的参与者，也是NPC的组织者。**

要实现这种“轻量化、重代入感”的设计，可以尝试从以下几个维度构建游戏循环：

### 1. 核心循环：从“亲力亲为”到“分工协作”

因为不想走重工业自动化路线，你们可以将驱动力从“传送带”改为**“人力/角色的社会分工”**。

* **探索/采集（玩家）：** 玩家亲自去危险区域或资源点获取核心素材（类似星露谷下矿）。
* **规划/建造（玩家）：** 玩家决定基地的布局。
* **日常维持（NPC）：** NPC 负责那些枯燥的、重复性的体力活（如种地、砍柴、搬运）。
* **反馈与扩张：** 随着基地变好，吸引更多有特殊能力的 NPC，解锁玩家更高级的装备或建筑。

**循环公式：** `玩家获取稀有资源` -> `建设基础设施` -> `吸引/解救NPC` -> `NPC自动化日常任务` -> `释放玩家精力去更远的地方探索`。

---

### 2. 引导机制：以“需求”代替“科技树”

既然规模靠近《星露谷》，可以把《环世界》繁琐的数值简化为**视觉化的“需求满足”**。

* **NPC 的入住门槛：** 每一个 NPC 都是一个“小目标”。比如，想让医生加入，你必须先盖一间带床和药架的屋子。这比单纯点开科技树更有成就感。
* **建筑的功能性联动：** 引导玩家建设的不是“效率”，而是“生活”。比如：
* 盖了**厨房**，NPC 吃的饭能加 BUFF。
* 盖了**工坊**，玩家可以升级采集工具。


* **代入感强化：** 玩家必须在基地内走动，亲自把任务“委托”给 NPC（或者在告示板发布），而不是远程指挥。这种**物理上的交互**是增强代入感的关键。

---

### 3. 如何简化《环世界》的数值系统？

《环世界》的复杂在于每一个身体部位、每一种情绪都有数值。你们可以进行“降维打击”：

| 模块 | 《环世界》做法（复杂） | 你们的简化方案（星露谷化） |
| --- | --- | --- |
| **健康** | 全身器官损伤、感染率、流血速度 | 传统的 HP 条 + 简单的“状态异常”（如：骨折、感冒）。 |
| **情绪** | 几十种正负面 Buff 叠加计算 | 三种状态：满意、一般、消极。影响 NPC 的工作速度或是否离开。 |
| **战斗** | 掩体计算、子弹轨迹、器官击穿 | 动作 RPG 元素。玩家亲自参与战斗，NPC 提供远程辅助或防御。 |
| **资源** | 数十种材料（不同木头、金属） | 简化为几类核心资源（木材、石材、矿石、食物）。 |

---

### 4. 增强“代入感”的特色设计

既然玩家控制一个具体角色，可以加入一些**“领袖行为”**：

* **光环效应：** 当玩家在 NPC 附近工作时，NPC 的效率提升；或者在战斗中，玩家的存在能提高大家的士气。
* **共建感：** 某些大型建筑（如防御墙）需要玩家和 NPC 共同敲打才能完工。这种“大家一起干活”的画面感非常治愈。
* **有限的自动化：** 不要用传送带，而是用“职能站”。比如你放一个“砍树旗标”，NPC 就会去附近砍树并堆在指定地点。这种**基于视觉逻辑**的自动化比工业流水线更适合 2D 异视角。

---

### 建议的下一步实验

既然你们已经在用 Godot 开发，可以先尝试做一个 **"Follower System" (追随者系统)**：

1. 写一个简单的脚本，让一个 NPC 会跟随玩家。
2. 给 NPC 一个“收集木头”的任务。
3. 当 NPC 收集到一定数量，能触发一段对话或解锁一个新的配方。

这种以**“人”**为核心的基建感，可能会比单纯的工厂更有温度。你们目前的进度在哪个阶段？是已经在写 NPC 的 AI 行为树，还是在处理等距视角的地图构建？