# Great Nailsage Sly (Open-Source Showcase)

> CN: 这是 **GreatNailsageSly** 的开源展示版本，用于展示原型开发能力、系统设计思路与技术实现深度。  
> EN: This is the open-source showcase edition of **GreatNailsageSly**, focused on prototype execution, system design, and technical depth.

## 1) 项目标题 | Project Title

**Great Nailsage Sly (Open-Source Showcase)**

## 2) 简短介绍 | Brief Introduction

**CN**  
一款 2D 动作平台/Boss 战原型游戏，使用 Unity（URP）开发。项目重点不在“堆功能”，而在有限周期内完成可扩展的角色控制、敌人行为系统、战斗反馈链路与渲染深度表现。技术栈包含 C#、Unity 2D、URP、Behavior Designer、Shader Graph。  

**EN**  
A 2D action-platformer / boss-fight prototype built with Unity (URP). The focus is not feature quantity, but building scalable player control, enemy behavior systems, combat feedback loops, and rendering depth within a tight schedule. Tech stack includes C#, Unity 2D, URP, Behavior Designer, and Shader Graph.

## 3) Demo 展示 | Demo

**GIF 预览 | GIF Preview**  
![Gameplay Demo 01](Demo/Demo01.gif)
![Gameplay Demo 02](Demo/Demo02.gif)

**可运行版本 | Playable Build**  
- Itch.io: [Play Great Nailsage Sly](https://fr3derica.itch.io/great-nailsage-sly?secret=vM4aLKURztTBJMaxLRwbVUbsJw)

## 4) 技术亮点（系统设计） | Technical Highlights (System Design)

**CN**
- 模块化角色系统：`PlayerRoot` 作为结构入口，聚合 `Input / Movement / Jump / Combat / Facing`，`PlayerController` 负责输入路由与模块编排。
- 敌人统一实体入口：`EnemyRoot` 自动装配黑板、生命、受击、死亡、行为树等组件，降低预制体配置耦合。
- 事件驱动战斗链路：以 `IDamageable + DamageInfo + HpHealth` 为核心，统一玩家、敌人、陷阱伤害处理语义。
- 遭遇战触发系统：`EnemyEncounter + EncounterTriggerZone2D` 支持场景触发激活与 Boss 动态生成。
- 可控移动边界：`EnemyMoveRange2D` 提供 `ClampX / ContainsX`，保证 AI 位移与跳跃落点可控、可调试。

**EN**
- Modular player architecture: `PlayerRoot` aggregates `Input / Movement / Jump / Combat / Facing`, while `PlayerController` orchestrates input routing.
- Unified enemy entity entry: `EnemyRoot` auto-wires blackboard, health, hurt/death handlers, and behavior tree components.
- Event-driven combat pipeline: `IDamageable + DamageInfo + HpHealth` standardizes combat semantics across player/enemy/hazard.
- Encounter activation flow: `EnemyEncounter + EncounterTriggerZone2D` supports trigger-based activation and runtime boss spawning.
- Bounded AI movement: `EnemyMoveRange2D` (`ClampX / ContainsX`) constrains locomotion and jump landing behavior for stability.

## 5) 架构说明 | Architecture

**CN**  
本项目没有套用现成开源“通用游戏框架”，而是围绕原型开发效率自建了一套轻量架构：
- `Root + Module`：实体根节点负责依赖聚合，功能模块专注单一职责。
- `Orchestrator`：控制器只做“调度/路由”，不吞并具体业务逻辑。
- `Blackboard as Runtime State`：敌人共享运行态集中到 `EnemyBlackboard`，避免行为任务之间隐式耦合。
- `Event-first Combat`：生命组件通过事件向表现层广播（受击闪白、死亡动画、销毁等），降低跨模块直接引用。

**EN**  
Instead of adopting a generic open-source framework, this project uses a lightweight custom architecture optimized for prototype speed:
- `Root + Module`: entity roots aggregate dependencies; modules keep single responsibilities.
- `Orchestrator`: controllers route and schedule, without swallowing domain logic.
- `Blackboard as Runtime State`: enemy shared runtime facts are centralized in `EnemyBlackboard`.
- `Event-first Combat`: health components emit events to presentation/gameplay responders, reducing hard coupling.

## 6) AI 设计说明 | AI Design

**CN**
- 使用 **Behavior Designer** 构建敌人行为树，Boss（False Knight）的核心行为以自定义 Task 实现（如 `BT_FK_JumpToPlayer`、`BT_FK_BackstepToFarthestSide`、`BT_FK_NormalAttack`）。
- 行为选择依赖 `EnemyBlackboard` 作为“事实源（source of truth）”：
  - 目标信息：`player`、`distanceToPlayer`
  - 状态信息：`isDead`、`isHurtLocked`、`isAttacking`
  - 动作记忆：`lastMoveWasBackstep`、`lastMoveWasJumpToPlayer`（防止重复动作）
- 跳跃类行为使用弹道参数计算（上跳初速度 + 重力 + 目标锁定），并结合 `EnemyMoveRange2D` 对落点与空中越界进行约束，提升战斗可读性与稳定性。
- 攻击触发与动画事件解耦：行为层只发起请求，命中盒生成与攻击窗口由战斗/动画链路处理。

**EN**
- Enemy behaviors are built with **Behavior Designer**, with custom boss tasks such as `BT_FK_JumpToPlayer`, `BT_FK_BackstepToFarthestSide`, and `BT_FK_NormalAttack`.
- `EnemyBlackboard` works as the runtime source of truth:
  - Target facts: `player`, `distanceToPlayer`
  - State facts: `isDead`, `isHurtLocked`, `isAttacking`
  - Action memory: `lastMoveWasBackstep`, `lastMoveWasJumpToPlayer` (prevents repetitive patterns)
- Jump actions use ballistic-style calculations (initial vertical speed + gravity + locked target), then clamp motion via `EnemyMoveRange2D` for robustness.
- Behavior logic requests attacks, while hitbox windows and animation timing are handled by combat/animation layers.

## 7) 渲染与视觉系统 | Rendering & Visuals

**CN**
- 渲染管线采用 **URP**（Universal Render Pipeline）。
- 为 2D Sprite 引入深度表达：使用 `SG_SpriteDepthClip.shadergraph` 与材质 `M_Sprite_DOFDepth`，让 2D 元素参与更真实的深度关系。
- 配套渲染工具脚本：
  - `ApplySpriteMaterial`：批量将深度材质应用到 SpriteRenderer。
  - `StaggerSpriteZ`：按微小步进错开局部 Z，避免 z-fighting。
  - `EnvSpriteBatch`：统一场景环境精灵的 Sorting Layer / Order / 局部 Z。
- 目标是用 2D 资源构建更清晰的空间层次，同时保持可控的性能与可编辑性。

**EN**
- Rendering is built on **URP**.
- 2D sprites are given depth-aware behavior using `SG_SpriteDepthClip.shadergraph` and `M_Sprite_DOFDepth`.
- Supporting rendering utilities:
  - `ApplySpriteMaterial` for batch material assignment.
  - `StaggerSpriteZ` for tiny local Z offsets to avoid z-fighting.
  - `EnvSpriteBatch` for consistent sorting layer/order and local Z control.
- The goal is stronger spatial readability from 2D assets while keeping the workflow performant and editable.

## 8) 项目结构 | Project Structure

```text
Assets/
├─ Game/
│  ├─ Scripts/
│  │  ├─ Combat/              # Damage, health, combat contracts
│  │  ├─ Player/              # PlayerRoot + modules + controller
│  │  ├─ Enemies/
│  │  │  ├─ Common/           # EnemyRoot, Blackboard, hurt/death, projectile
│  │  │  ├─ Boss/FalseKnight/ # Boss combat + BT custom tasks
│  │  │  └─ Encounter/        # Encounter trigger and move range systems
│  │  ├─ Systems/Rendering/   # Sprite material, z-stagger, sorting tools
│  │  ├─ UI/
│  │  └─ Utils/
│  ├─ Rendering/
│  │  ├─ Shaders/             # SG_SpriteDepthClip.shadergraph
│  │  ├─ Materials/           # M_Sprite_DOFDepth, etc.
│  │  └─ Pipeline/            # URP assets
│  └─ Scenes/
├─ Behavior Designer/         # Behavior Designer plugin assets
└─ ThirdParty/
```

## 9) 技术反思与开发周期 | Technical Reflection & Timeline

**CN**  
开发周期为 **2026-01-19 到 2026-02-19**（约 3 周）。这个周期内完成了可玩原型、核心战斗回路、Boss AI、渲染深度方案和基础架构搭建。  
项目已验证“快速原型 + 可扩展架构”的可行性，但在内容丰富度、关卡细节、数值平衡与打击反馈精修方面仍有较大打磨空间。

**EN**  
Development ran from **2026-01-19 to 2026-02-19** (about 3 weeks). Within this window, I shipped a playable prototype with core combat loop, boss AI, depth-oriented rendering setup, and a custom extensible architecture.  
The project validates a fast-prototype-plus-scalable-structure approach, while still requiring further polish in content depth, level detail, balancing, and combat feel.
