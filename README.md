# A Long Journey

> An 2D indie game under development, it would combine elements of RPG, Rogue-like and strategy. We hope the AI will be the highlight of our project 

---
## Git 使用指南 - tt可以看这（

> 虽然大部分时候可以用GitHub Desktop，但是有时候还是指令更靠谱一点

打开git bash（git的终端， 就是输指令的地方）
* 右键选中文件夹 - Git Bash Here
* 或者在windows搜索Git Bash - 打开后输入
```bash
cd Users/jackwan/alongjouney/ # 这是一个例子，替换成你本地的文件路径
```
### 🔹 每次开始工作前
```bash
# 确保在 dev 分支
git checkout dev

# 获取远程最新进度
git pull origin dev

# 获取到本地文件出了点问题，新建个文件夹到GitHub - Code - Copy URL
git clone https://github.com/jsworks2k25/alongjourney.git
```

---

### 🔹 提交更改
```bash
# 查看修改了哪些文件
git status

# 添加所有更改到暂存区
git add -A

# 提交更改：-m 后面是commit message，记得一定加引号
git commit -m "feat: 添加玩家移动场景"
```

---

### 🔹 推送到远程
```bash
# 推送到 dev 分支
git push origin dev
```

---

### 🔹 创建功能分支（推荐做法）
如果要做新功能或大修改：  
```bash
# 从 dev 创建新分支
git checkout dev
git pull origin dev
git checkout -b feature/jump-mechanic

# 开发并提交后推送
git push origin feature/jump-mechanic
```
然后在 GitHub 上发起 Pull Request 合并回 `dev`。  

---

### 🔹 常见问题
- **忘记切分支就在 main 上开发了**  
  ```bash
  git checkout -b hotfix/move-from-main
  git push origin hotfix/move-from-main
  ```
  然后用 PR 合并。  

- **远程和本地冲突**  
  ```bash
  git pull origin dev
  # 按提示解决冲突，然后：
  git add -A
  git commit
  git push origin dev
  ```

- **彻底覆盖远程分支（需小心！）**  
  如果确认本地版本才是对的：  
  ```bash
  git push origin dev --force
  ```

---

## 链接 / Links
* 云文档链接 [`GOOGLE DRIVE`](https://drive.google.com/drive/folders/1JxQtAn7uOHykelvt85-YNNnM-vz1oXzR)
* 游戏设计文档 / Game Design Document [`docs/GDD.md`](./docs/GDD.md)
* 开发日志 / Development Log [`docs/Devlog.md`](./docs/Devlog.md)

---

## 开发进度 / Development Timeline

### 阶段一：灵感与策划 / Phase 1: Ideation & Planning

* [x] 明确核心概念与世界观
  Define game concept and worldview
* [x] 制作游戏设计文档（GDD）
  Draft Game Design Document (GDD)
* [x] 确立哲学议题与表达方式
  Define core philosophical theme

### 阶段二：原型开发 / Phase 2: Prototype Development

* [ ] 实现基础操作与场景搭建
  Implement basic movement and environment
* [ ] 制作最小可玩版本（MVP）
  Build Minimum Viable Product (MVP)
* [ ] 初步用户测试与反馈
  Conduct initial playtests

### 阶段三：系统开发 / Phase 3: Core Systems

* [ ] 搭建 UI / 存档 / 剧情模块
  Develop UI, Save System, Narrative Tools
* [ ] 创建主要场景与关卡
  Build core scenes and levels
* [ ] 整合美术资源与音效
  Integrate art and audio assets

### 阶段四：优化与测试 / Phase 4: Polish & Testing

* [ ] 多轮内部测试与调优
  Conduct multiple playtest rounds
* [ ] 修复 Bug、优化滑稳性
  Bug fixing and performance optimization
* [ ] 增强游戏引导与手感
  Enhance onboarding and UX

### 阶段五：发布与展示 / Phase 5: Release & Showcase

* [ ] 上传到 Itch.io / Steam（视情况）
  Publish to Itch.io / Steam
* [ ] 发布 Devlog / 项目总结
  Release devlogs and final reflection
* [ ] 准备展示资料与宣传片
  Prepare presentation and trailer

---

## 技术栏 / Tech Stack

| 类型 / Type          | 工具 / Tool                        |
| ------------------ | -------------------------------- |
| 引擎 / Engine        | Unity (C#)                       |
| 图像 / Graphics      | Aseprite / Photoshop / Blender   |
| 音效 / Audio         | Bfxr / Audacity / 自录             |
| 项目管理 / PM          | GitHub Projects / Trello         |
| 协作 / Collaboration | Git + GitHub / Notion / Markdown |

---

## 面向招生官的亮点 / Highlights for Admissions

* **编程能力 / Technical Proficiency**：
  原创机制、模块化结构、数据驱动开发
* **创造表达 / Creative Expression**：
  将复杂主题转化为互动体验
* **项目管理 / Project Management**：
  明确分工、按阶段开发、使用版本控制
* **反思与文档 / Reflection & Documentation**：
  完整记录开发过程，表达开发理念与挑战

---

## 附加资源 / Additional Docs

* [`docs/GDD.md`](./docs/GDD.md)：游戏设计文档 / Game Design Document
* [`docs/Devlog.md`](./docs/Devlog.md)：开发日志 / Development Log
* [`docs/Reflection.md`](./docs/Reflection.md)：项目反思 / Final Reflection

---

## 联系方式 / Contact

### 开发者 / Developers：
* Jack Wan: Script, AI design, musics & sound effects
* Qijia Liu: Mechanics, Gameplay design
* Stan Wan: Scripting assistance, Artworks, UI

### 邮箱 / Email：[stanwan375@gmail.com](mailto:stawnan375@gmail.com)

---

> 本项目时间深思技术与创造表达，欢迎任何意见和反馈！
> This project showcases our technical depth and creative thinking — feedback is welcome!
