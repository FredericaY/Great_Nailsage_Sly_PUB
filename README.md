# Great Nailsage Sly

这是我用 Unity 做的一个 2D 动作游戏项目。

整体方向偏平台动作 / 类银河恶魔城，目前已经把角色移动、战斗、敌人 AI、Boss 遭遇、护符、商店、暂停菜单这些核心部分搭起来了，后面会继续补关卡内容、数值打磨和更多角色能力。

## 当前内容

目前项目里已经有这些内容：

- 基础移动：移动、跳跃、墙滑、蹬墙跳
- 近战战斗：平砍 / 上挑 / 下劈空中攻击
- 连段与输入缓冲
- 普通敌人和 Boss
- 基于行为树的敌人 AI
- Geo 货币掉落与拾取
- 护符系统
- 商店与消耗品
- 暂停菜单、设置页、护符页
- 输入重绑定
- 隐藏墙 / 隐藏房间

## 开发环境

- Unity：`2022.3.62f3`
- Render Pipeline：`URP`
- Input：`Unity Input System`
- 插件：`Cinemachine`、`Behavior Designer`

## 运行方式

用 `Unity 2022.3.62f3` 打开项目后，可以直接从这些场景开始：

- `Assets/Game/Scenes/Levels/All_Levels.unity`
- `Assets/Game/Scenes/Levels/Greenpath_Room1.unity`
- `Assets/Game/Scenes/Levels/Hive_Room1.unity`
- `Assets/Game/Scenes/Levels/Level_01_jy.unity`

## 操作

项目现在同时支持键鼠和手柄，键位也可以在暂停菜单里重绑。

默认主要操作大致是：

- 移动：`A / D` 或方向键
- 跳跃：`Space`
- 攻击：`J` / 输入资源里的 Attack 键位
- 交互：`E`
- 暂停：`Esc`

如果后面我改了 Input Actions，这里不一定会第一时间同步，最终以 `Assets/Game/Input/GameInput.inputactions` 和暂停菜单里的实际绑定为准。

## 项目结构

我现在主要按功能把代码放在 `Assets/Game/Scripts/` 下面：

```text
Assets/Game/Scripts/
├─ Audio/          音频服务、场景音频
├─ Combat/         血量、伤害、投射物、对象池
├─ Core/Input/     输入路由、输入重绑定
├─ Enemies/        敌人公共逻辑、行为树任务、遭遇战
├─ Pickups/        Geo 掉落与拾取
├─ Player/         玩家控制、移动、跳跃、战斗
├─ Systems/        护符、环境机关、场景切换、渲染辅助
├─ UI/             HUD、Boss 血条、暂停菜单
└─ Utils/          传感器、相机震动、协程工具等
```

资源基本都放在 `Assets/Game/` 下，第三方内容单独放在 `Assets/Behavior Designer` 和其他对应目录里。

## 目前代码里几个比较核心的部分

这里简单记一下我现在项目里几个比较关键的实现，后面自己回来看也方便。

### 1. 玩家结构

玩家这边我没有把所有逻辑塞进一个大脚本里，而是拆成了几块：

- `PlayerRoot`：统一拿引用，作为玩家实体入口
- `PlayerController`：负责输入路由
- `PlayerMovement`：水平移动
- `PlayerJump`：跳跃、墙跳、墙滑、额外跳跃
- `PlayerCombat`：攻击逻辑
- `PlayerLock`：商店 / 演出 / 菜单期间锁玩家
- `PlayerCharmInventory` / `PlayerCharmRuntime`：护符持有和运行时效果

现在这样拆下来以后，加新能力会比直接堆状态机更舒服一些。

### 2. 战斗时序

战斗这块我比较在意手感和时序，所以攻击不是单纯按键就立刻生成判定。

现在的做法是：

- `PlayerCombat` 先决定这次攻击是什么
- 然后向动画层发请求
- 命中帧用动画事件生成 Hitbox
- 动画结束时再开放下一次输入

另外还补了几件事：

- 连段窗口
- 输入缓冲
- 长按重复攻击
- 动画事件丢失时的超时保护

这样至少在逻辑上，攻击状态和动画播放不会太容易打架。

### 3. 敌人 AI

敌人 AI 现在是基于 `Behavior Designer` 做的。

公共部分主要是：

- `EnemyRoot`
- `EnemyBlackboard`
- `EnemyAggroSensor2D`
- `EnemyDeath`

然后不同敌人和 Boss 的行为再拆成各自的 BT Task。

例如 False Knight 这边，跳扑、后撤、普通攻击、波攻击这些都是拆开的。Boss 的一些行为也会记在黑板里，避免连续重复同一种动作。

### 4. 投射物和对象池

远程攻击这块我单独做了一层基础设施：

- `BaseProjectile2D`：投射物公共生命周期
- `EnemyProjectileBase2D`：敌方投射物扩展
- `ProjectilePoolService`：场景级对象池

现在 Boss 的冲击波已经走对象池了，后面如果继续加远程敌人或者玩家技能，这套可以继续复用。

### 5. 护符、商店和经济

这部分基本已经串起来了。

- `PlayerCurrency`：玩家货币
- `GeoPickup` / `GeoPickupSpawner`：敌人死亡掉落 Geo
- `CharmDefinition`：护符定义，走 ScriptableObject
- `PlayerCharmInventory`：持有 / 装备
- `PlayerCharmRuntime`：把护符效果转成运行时能力和数值
- `CharmVendor`：商店购买逻辑
- `PlayerConsumables`：当前接了 Quick Heal 这类消耗品

护符现在不只是收藏，它会直接影响移动、攻击和拾取体验，比如二段跳、攻击倍率、攻击间隔、Geo 磁吸这些。

### 6. UI 和输入

UI 这边我现在比较注意“菜单接管输入”和“玩法输入”之间不要互相干扰。

- `GameInputRouter`：切换 Gameplay / UI 两套 ActionMap
- `InputRebindController`：处理重绑定和保存
- `PauseMenuController`：暂停菜单总控
- `PauseMenuSettingsPage`：设置和键位页
- `PauseMenuCharmPage`：护符页

商店和暂停菜单打开时，都会切到 UI 输入模式，避免角色还在响应玩法输入。

## 现在还想继续补的内容

接下来大概率会继续做这些方向：

- 更多关卡内容和引导
- 更完整的场景切换和出生点
- 存档 / 读档
- 更多角色能力
- 更多敌人类型
- 更细一点的数值和平衡调整
- 更完整的反馈表现，比如受击、镜头、音效和特效

## 当前状态

这个项目现在还不是一个完整成品，更接近“核心系统已经成型、内容还在继续补”的阶段。

对我来说现阶段最重要的不是继续堆功能，而是把已有这套东西继续做稳，尤其是战斗手感、关卡节奏和系统之间的衔接。

