# Great Nailsage Sly

一个使用 Unity 制作的 2D 动作游戏练习项目，整体方向是类银河恶魔城/平台动作体验：包含角色移动与跳跃、近战连段、敌人行为树 AI、Boss 遭遇战、护符系统、商店经济、暂停菜单与输入重绑定等完整玩法闭环。

这个 README 按开源小游戏项目的方式组织，同时尽量把实现层面的架构和关键机制说明清楚，方便快速理解项目价值、代码结构与可扩展点。

## 项目概览

- 引擎版本：`Unity 2022.3.62f3`
- 渲染管线：`URP 14`
- 输入方案：`Unity Input System`
- 核心插件：`Cinemachine`、`Behavior Designer`
- 主要场景：
  - `Assets/Game/Scenes/Levels/All_Levels.unity`
  - `Assets/Game/Scenes/Levels/Greenpath_Room1.unity`
  - `Assets/Game/Scenes/Levels/Hive_Room1.unity`
  - `Assets/Game/Scenes/Levels/Level_01_jy.unity`

## 玩法内容

- 角色支持移动、起跳、墙滑、蹬墙跳，以及基于护符能力解锁的二段跳。
- 战斗以近战为核心，包含平砍/上挑/下劈空攻击组合，支持连段、输入缓冲和长按连打。
- 敌人包含普通怪、飞行怪和 Boss，AI 由行为树驱动。
- 经济系统以 `Geo` 为货币，敌人死亡掉落拾取物，玩家可在商店购买护符或消耗品。
- 护符系统会直接影响移动、攻击和拾取体验，例如二段跳、伤害增益、攻击间隔缩短、Geo 磁吸、快速治疗。
- 场景内存在遭遇战、Boss 出场演出、隐藏墙密室、危险区域和基础地图切换。
- 暂停菜单支持设置页、护符页、输入重绑定和多端提示切换。

## 我重点实现了什么

如果从客户端/游戏开发岗位视角看，这个项目主要体现的是下面几类能力：

- 组件化角色架构：玩家不是“一个大脚本”，而是 `PlayerRoot + Input + Controller + Movement + Jump + Combat + Facing + AnimatorDriver + CharmRuntime` 的组合式结构。
- 动作时序控制：攻击逻辑和动画播放分离，真正的伤害判定由动画事件在精确帧触发，避免视觉与判定脱节。
- AI 工程化组织：敌人公共能力沉淀在 `EnemyRoot / EnemyBlackboard / EnemyAggroSensor2D / EnemyDeath`，具体敌人行为通过 Behavior Designer Task 组合。
- 统一投射物体系：玩家/敌人远程物体都围绕 `BaseProjectile2D` 与 `ProjectilePoolService` 组织，兼顾复用性和性能。
- 经济与装备闭环：`PlayerCurrency + GeoPickup + CharmVendor + PlayerCharmInventory + PlayerCharmRuntime` 形成可持续扩展的数值系统。
- UI 与输入解耦：`GameInputRouter` 管 Gameplay/UI ActionMap 切换，暂停菜单和商店都能安全接管输入；`InputRebindController` 负责重绑定与持久化。

## 核心架构

### 1. 玩家架构：PlayerRoot 聚合，模块各司其职

玩家相关核心脚本集中在 `Assets/Game/Scripts/Player/`。

- `PlayerRoot`
  - 作为玩家实体的结构入口，统一缓存 Rigidbody、Collider、Animator、AimPoint，以及各功能模块引用。
  - 典型作用是把“实体数据入口”和“行为模块”分开，减少模块间的查找和硬编码依赖。
- `PlayerController`
  - 负责把输入路由给 `Movement / Jump / Combat`。
  - 单独处理朝向仲裁，例如贴墙时朝墙或离墙朝向锁定，避免角色在墙边抖动翻转。
- `PlayerMovement`
  - 使用 Rigidbody2D 速度驱动水平移动。
  - 通过加速度/减速度逼近目标速度，而不是直接写死速度，动作手感更稳定。
  - 直接读取护符运行时倍率，支持数值型装备扩展。
- `PlayerJump`
  - 负责普通跳、墙跳、可变跳高、墙滑和额外跳跃次数管理。
  - 二段跳能力不是写死的，而是通过护符能力检测动态开放。
- `PlayerLock`
  - 提供引用计数式锁定，Boss 出场、商店、暂停菜单等都能安全接管角色控制。

这一套结构的好处是，角色逻辑不是堆在单一状态机里，而是按“输入、运动、战斗、能力修饰、表现层”分层，后续继续加 dash、受击硬直、技能系统时更容易扩展。

### 2. 战斗系统：动画驱动命中，逻辑驱动状态

核心脚本集中在 `Assets/Game/Scripts/Player/Combat/` 与 `Assets/Game/Scripts/Combat/`。

- `PlayerCombat`
  - 负责连段状态、输入缓冲、长按重复攻击、攻击超时保护。
  - 攻击流程是：
    1. 逻辑层决定本次攻击类型
    2. 向动画层发起请求
    3. 缓存待生成 Hitbox
    4. 动画事件在命中帧调用 `AnimEvent_SpawnAttackHitbox`
    5. 动画尾帧通过 `AnimEvent_AttackEnd` 重新开放输入
  - 这样做可以把“状态判断”和“判定生成”解耦，属于比较典型的动作游戏时序控制方式。
- `AttackHitbox`
  - 近战伤害实体按攻击帧生成，不常驻，不需要在 Update 中一直维护。
- `HeartsHealth`
  - 玩家使用“心数”制血量系统，实现了受击无敌帧、伤害映射和恢复逻辑。
- `HpHealth`
  - 敌人/Boss 使用连续数值 HP，方便配合不同伤害来源、Boss 血条和危险区域规则。

这里最值得聊的点不是“能打到敌人”，而是：

- 连段窗口和输入缓冲的实现
- 动画事件与逻辑态同步
- 丢失动画事件时的超时保护
- 装备系统如何影响攻击伤害与攻击间隔

### 3. 敌人与 AI：行为树 + 黑板 + 公共底座

敌人脚本主要在 `Assets/Game/Scripts/Enemies/`。

- `EnemyRoot`
  - 是敌人公共入口，聚合黑板、血量、死亡、Animator、GroundSensor、MoveRange、BehaviorTree。
- `EnemyBlackboard`
  - 存玩家引用、与玩家距离、朝向、攻击状态、冷却、行为记忆等运行时数据。
  - Boss 的“不要连续两次后撤/跳向玩家”这类限制，就是通过黑板中的行为记忆实现。
- `EnemyAggroSensor2D`
  - 负责索敌，把玩家引用写入黑板。
- `EnemyDeath`
  - 统一处理死亡事件、行为树关闭、碰撞关闭、死亡动画和 Geo 掉落。
- `Behavior Designer Task`
  - 普通怪和 Boss 的行为拆成一组组小 Task，例如巡逻、转向、追击、攻击条件判断、跳扑、后撤、波形攻击。
  - 例如 `BT_FK_JumpAttack` 会在起跳前锁定玩家目标位置，结合重力和期望上抛速度计算横向速度，保证 Boss 跳扑更像“有设计感的攻击”而不是纯粹随机位移。

整体上，这套 AI 不是简单 if/else 堆逻辑，而是做成了：

- 公共感知层
- 黑板状态层
- 行为树决策层
- 攻击发射器执行层

这在多人协作或者后期持续加怪物类型时更容易维护。

### 4. 对象池与投射物体系

这部分对应简历里比较典型的“对象池 / 战斗基础设施”能力。

- `ProjectilePoolService`
  - 提供场景级投射物对象池。
  - 支持按 Prefab 维度缓存、预热、延迟回收。
  - 回收前会统一清理 Rigidbody2D 的速度和角速度，减少残留状态。
- `BaseProjectile2D`
  - 抽象了投射物生命周期、命中判定、世界碰撞、自身寿命、越界销毁、只命中一次集合等通用逻辑。
- `EnemyProjectileBase2D`
  - 在基础投射物之上封装了敌方投射物数据读取、目标指向、追踪更新、MoveRange 边界约束等机制。
- `FKAttackEmitter`
  - Boss 攻击执行器中已经接入对象池来发射冲击波，而不是每次直接实例化。

从项目代码能看出，这里的目标不是只做一个特例子弹，而是先搭“可复用的投射物基座”，再让不同怪物按数据和特化逻辑接入。

### 5. 经济、商店与“背包/装备”机制

项目里没有传统 RPG 式大背包，但已经实现了比较完整的“护符背包 + 装备 + 商店”闭环。

- `PlayerCurrency`
  - 玩家货币容器，负责增减、消费能力判断和事件通知。
- `GeoPickup` / `GeoPickupSpawner`
  - 敌人死亡后按面额拆分掉落 Geo。
  - 掉落物有散射、弹跳、延迟拾取、Geo Magnet 磁吸。
- `CharmDefinition`
  - 使用 ScriptableObject 存护符定义：ID、名称、描述、图标、价格、属性倍率、授予能力。
- `PlayerCharmInventory`
  - 管理拥有列表与当前装备护符。
- `PlayerCharmRuntime`
  - 负责把“当前装备护符”转成游戏逻辑可消费的能力与数值接口，例如：
  - `HasDoubleJumpAbility()`
  - `GetMoveSpeedMultiplier()`
  - `GetAttackDamageMultiplier()`
  - `GetAttackCooldownMultiplier()`
- `CharmVendor`
  - 实现场景触发式商店。
  - 支持进入交互区后接管输入、暂停时间、运行时动态生成商店 UI、判断货币是否足够、购买护符或消耗品。
- `PlayerConsumables`
  - 当前接入了 Quick Heal 消耗型道具，与商店和输入系统联动。

这部分是非常适合面试展开的，因为它同时覆盖了：

- 数据驱动
- 运行时状态管理
- UI 生成
- 游戏数值接口抽象
- 商店交易与资源回滚

### 6. UI、暂停菜单与输入系统

UI 相关代码主要在 `Assets/Game/Scripts/UI/` 和 `Assets/Game/Scripts/Core/Input/`。

- `GameInputRouter`
  - 管理 Gameplay / UI 两套 ActionMap 的启停。
  - 统一暴露菜单导航、确认、取消、翻页、暂停、使用消耗品等输入状态。
  - 记录最近输入设备来源，用于动态切换键鼠/手柄提示。
- `InputRebindController`
  - 支持交互式重绑定，并通过 JSON 存到 `PlayerPrefs`。
- `PauseMenuController`
  - 打开菜单时暂停时间、切换输入模式、关闭场景内其他 Canvas，并驱动页面滑动。
- `PauseMenuSettingsPage`
  - 支持音量调节和键位重绑。
- `PauseMenuCharmPage`
  - 负责护符列表构建、网格选择、装备与详情展示。
- `BossHealthBar`
  - 可直接绑定 Boss 血量，也可跟随遭遇战动态绑定生成的 Boss。

这一部分体现的是“UI 不是孤立脚本”，而是和输入、时间缩放、玩家锁定、装备系统做了联动。

### 7. 场景机制与特殊内容

- `EnemyEncounter`
  - 负责遭遇战激活、Boss 生成、玩家输入锁定、标题演出和 Boss 死亡通知。
- `EncounterGateController`
  - 根据遭遇战状态开闭场景门。
- `HiddenWallReveal`
  - 实现了一个挺有意思的隐藏房间机制：
  - 玩家靠近或进入隐藏墙后，临时禁用周围碰撞体
  - 运行时生成独立密室边界与出口
  - 传送玩家进入密室
  - 随机生成消耗品或 Geo 奖励
  - 出口返回原场景指定点
- `MapTransition`
  - 提供基础的跨场景传送。
- `LevelResetter`
  - 调试期快速重开当前场景。

## 项目目录

实际可关注的主体目录如下：

```text
Assets/
├─ Game/
│  ├─ Audio/                 音频资源、SoundBank、SceneAudioProfile
│  ├─ Input/                 Input Actions 资源
│  ├─ Prefabs/               角色、敌人、特效、场景物件预制体
│  ├─ Scenes/                可运行场景
│  ├─ Scripts/
│  │  ├─ Audio/              音频服务与场景音频
│  │  ├─ Combat/             血量、伤害、投射物池
│  │  ├─ Core/Input/         输入路由与重绑定
│  │  ├─ Enemies/            敌人与行为树任务
│  │  ├─ Pickups/            Geo 掉落与拾取
│  │  ├─ Player/             玩家主逻辑
│  │  ├─ Systems/            护符、环境、场景、渲染等系统
│  │  ├─ UI/                 HUD、暂停菜单、Boss UI
│  │  └─ Utils/              相机震动、协程运行器、物理传感器等
│  └─ Tiles/                 Tile 资源
├─ Behavior Designer/        行为树插件
└─ ThirdParty/               其他第三方资源
```

## 如何运行

1. 使用 `Unity 2022.3.62f3` 打开项目。
2. 等待 Package 导入完成。
3. 可直接打开以下场景进行游玩或查看功能：
   - `Assets/Game/Scenes/Levels/All_Levels.unity`
   - `Assets/Game/Scenes/Levels/Greenpath_Room1.unity`
   - `Assets/Game/Scenes/Levels/Hive_Room1.unity`
4. 运行后可通过暂停菜单查看设置、护符页和输入重绑定。

## 项目亮点

- 不是单一 Demo 场景，而是包含玩家、敌人、Boss、商店、护符、UI、场景机制在内的一套完整玩法样板。
- 代码组织偏工程化，公共系统和特化玩法有明显边界。
- 已经有比较明确的数据驱动意识，尤其体现在护符定义、敌方攻击数据、音频 cue/bank 等内容上。
- 有意识地处理了动作游戏常见问题，例如输入缓冲、攻击丢事件保护、玩家锁定、UI 抢输入、投射物复用等。

## 可以继续扩展的方向

- 完整存档系统：货币、护符拥有状态、已开启的密室/商店购买记录等。
- 更完整的场景流转：过场、出生点、区域切换缓存。
- 更多角色能力：Dash、法术、蓄力攻击、空中冲刺。
- 更细的数值与内容生产工具：敌人配置表、掉落表、护符组合、关卡配置资产。
- 自动化测试与调试工具：核心数值回归、行为树验证、输入录制回放。

## 当前项目状态

这是一个完成度已经不错的个人动作游戏练习项目，但它仍然更偏“可玩的系统化原型”而不是商业完整产品。已经具备展示玩法、代码架构和客户端实现能力的价值，也保留了继续做大做深的空间。

