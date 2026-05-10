# UnifyCountry

一款 Unity 2D 横版策略卡牌肉鸽原型，当前核心方向是“三国题材 + 卡牌上阵 + 阵线对抗 + Roguelike 构筑”。

## 当前状态

按照成熟、可上线的肉鸽游戏产品目标评估，当前综合完成度约为：**22%**。

项目目前已经具备一个可玩的战斗原型，包括卡牌上阵、回合结算、敌方波次、计谋牌、Buff、战斗结算和简单的战后选牌奖励。同时已有主菜单、卡牌收藏场景和第一版固定路线 RunMap。

下一阶段的主要目标，是把当前线性的战斗原型推进为完整肉鸽 Run 循环：

```text
MainMenu -> RunMap -> Battle/Event/Shop/Rest -> Reward -> RunMap -> Boss -> Run Settlement
```

当前 RunMap 已支持：

- 从主菜单进入 `SCN_RunMap`。
- 固定纵向路线地图与滚动条。
- 战役节点进入战斗。
- 分支节点只记录路线选择，不触发战斗。
- 战役内多关连续推进，战役结束后返回 RunMap。
- 战斗页左上角只读地图浮层。

当前最高优先级缺口：

- 遗物系统
- 事件、商店、休整、精英战、Boss 节点
- 金币、删牌、升级、遗物等更多奖励类型
- 当前 Run 存档
- 数据校验和核心战斗测试
- 将运行时代码生成 UI 逐步迁移到 Prefab + Controller 结构

最新完整进度评估见：

- [Docs/ProjectProgress/ProgressReview_2026-05-10.md](Docs/ProjectProgress/ProgressReview_2026-05-10.md)

## 项目目录

```text
Assets/
└─ _Project/
   ├─ Art/
   ├─ Audio/
   ├─ Code/
   │  ├─ Runtime/
   │  │  ├─ Core/
   │  │  ├─ GameLoop/
   │  │  ├─ Cards/
   │  │  ├─ Deck/
   │  │  ├─ Towers/
   │  │  ├─ Enemies/
   │  │  ├─ Combat/
   │  │  ├─ Waves/
   │  │  ├─ Map/
   │  │  ├─ Roguelike/
   │  │  ├─ Economy/
   │  │  ├─ UI/
   │  │  ├─ Save/
   │  │  ├─ Config/
   │  │  └─ Utils/
   │  └─ Editor/
   ├─ Configs/
   ├─ Prefabs/
   ├─ Scenes/
   ├─ UI/
   ├─ ThirdParty/
   └─ Sandbox/
```

## 核心循环

长期目标循环：

```text
MainMenu -> RunMap -> Battle -> Reward -> RunMap
```

当前已实现内容更接近：

```text
MainMenu -> RunMap -> Campaign Battle(s) -> Card Reward -> RunMap
```

下一版可玩里程碑应重点完成：

- 开始一局 Run
- 进入路线地图
- 选择节点
- 进入战斗、事件、商店或休整内容
- 结算胜利和奖励
- 返回 RunMap
- 抵达并结算 Boss 节点

RunMap 技术设计见：

- [Docs/TechDesign/RunMap.md](Docs/TechDesign/RunMap.md)

## 命名前缀

```text
PF_   Prefab
SO_   ScriptableObject
MAT_  Material
TEX_  Texture
SPR_  Sprite
AUD_  Audio
ANIM_ Animation Clip
CTRL_ Animator Controller
SCN_  Scene
```
