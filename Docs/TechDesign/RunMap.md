# RunMap

## Current Scope

RunMap 已接入第一版固定路线原型，用于把线性战斗流程推进为肉鸽 Run 循环。

当前版本重点是路线与战役推进，不包含分支战斗内容：

- 主节点：战役节点，点击后进入战斗。
- 分支节点：非战斗节点，只记录路线选择，并解锁汇总后的下一个战役。
- 战斗内地图按钮：只打开只读地图浮层，不切换场景，不暂停或重置当前战斗。

## Scene And Entry

场景：

```text
Assets/_Project/Scenes/RunMap/SCN_RunMap.unity
```

主菜单入口：

```text
MainMenuUi.StartGame()
  -> RunSession.BeginNewRun()
  -> SceneManager.LoadScene("SCN_RunMap")
```

正式地图页面由 `RunMapUi` 运行时代码生成：

```text
Assets/_Project/Code/Runtime/UI/RunMapUi.cs
```

## Runtime State

RunMap 当前使用静态会话保存本局 Run 状态：

```text
Assets/_Project/Code/Runtime/Map/RunSession.cs
Assets/_Project/Code/Runtime/Map/RunMapTypes.cs
```

核心状态：

- `CurrentNodeId`：当前所在地图节点。
- `ActiveCampaignId`：当前正在进行的战役。
- `ActiveCampaignLevelIndex`：当前战役内第几关。
- `IsBattleActive`：是否处于战役战斗流程中。
- `IsRunComplete`：是否已完成最终 Boss。
- `CompletedNodeIds`：已完成节点。
- `AvailableNodeIds`：当前可选择节点。
- `DeckCounts`：当前 Run 牌库数量。
- `RouteHistory`：路线历史，用于底部路线展示。

当前状态仅驻留内存，尚未接入存档。

## Route Layout

地图为固定剧本路线，从上往下展开，并使用滚动条查看完整路线。

结构：

```text
黄巾之乱
  ├─ 安抚流民 ─┐
  └─ 收编义军 ─┘
虎牢关之战
  ├─ 联军粮道 ─┐
  └─ 夜袭敌营 ─┘
徐州攻防战
  ├─ 固守城池 ─┐
  └─ 追击吕布 ─┘
大战吕布
```

分支节点点击后不会进入战斗，只会：

1. 标记该分支完成。
2. 移除同组另一条分支的可选状态。
3. 解锁汇总后的下一个战役节点。
4. 将分支名称写入 `RouteHistory`。

## Campaigns

当前战役配置仍为代码内固定定义，关卡内容使用现有战斗关卡占位。

```text
CAMPAIGN_YELLOW_TURBAN  黄巾之乱    LEVEL_001 | LEVEL_002 | LEVEL_003
CAMPAIGN_HULAOGUAN      虎牢关之战  LEVEL_001 | LEVEL_002 | LEVEL_003
CAMPAIGN_XUZHOU         徐州攻防战  LEVEL_001 | LEVEL_002 | LEVEL_003
CAMPAIGN_LVBU           大战吕布    LEVEL_003
```

后续正式内容到位后，可以把这些占位关卡替换为：

```text
黄巾之乱：3 关
虎牢关之战：3 关
徐州攻防战：3 关
大战吕布：1 关，最终 Boss
```

## Battle Integration

战斗场景仍复用：

```text
Assets/Scenes/Battle/SCN_BattlePrototype.unity
```

当 `RunSession.HasActiveBattle` 为 true 时，`PrototypeBattleUi` 会：

1. 从 `RunSession.GetActiveBattleLevelIds()` 读取当前战役关卡列表。
2. 按 `ActiveCampaignLevelIndex` 进入战役内当前关。
3. 使用 `RunSession.Current.DeckCounts` 恢复当前 Run 牌库。
4. 战斗胜利后，如果战役还有下一关，则进入下一关。
5. 战役全部完成后，保存牌库，调用 `RunSession.CompleteActiveCampaign()`，返回 `SCN_RunMap`。

战后选牌奖励仍沿用现有战斗结算界面，并在领取奖励后同步写入 `RunSession.DeckCounts`。

## Battle Map Preview

战斗页面左上角有“地图”按钮。

该按钮只打开战斗内只读浮层：

- 不加载 `SCN_RunMap`。
- 不调用 `StopAllCoroutines()`。
- 不修改节点状态。
- 不允许重开 Run。
- 不允许选择地图节点。
- 点击“返回”只关闭浮层。

浮层使用与正式 RunMap 一致的纵向路线布局和滚动条，但节点全部不可交互。

## Current UI Notes

正式 RunMap 页面：

- 标题为“行军路线”。
- 节点从上往下排列。
- 地图区域可纵向滚动。
- 可用节点只显示节点名称，不显示“普通战役 3 关”等副标题。
- 底部只显示路线历史。

战斗内查看地图浮层：

- 标题为“战线”，居中显示。
- 地图区域可纵向滚动。
- 右下角按钮为“返回”。

## Known Limitations

- 地图与战役配置目前硬编码在 `RunSession`，后续应迁移到 CSV 或 ScriptableObject。
- 分支节点当前只记录选择，没有实际奖励、事件、商店、休整或遗物。
- RunState 只在内存中存在，切出运行或重启游戏后不会保存。
- 正式 RunMap 页面仍是运行时代码生成 UI，后续可迁移到 Prefab + Controller。
- 战斗内地图浮层与正式 RunMap 页面存在部分重复绘制逻辑，后续可抽共享组件。

## Suggested Next Steps

1. 将路线和战役配置数据化。
2. 为分支节点接入非战斗收益，例如恢复、删牌、商店、遗物、金币。
3. 接入 Run 存档。
4. 将 RunMap 节点组件抽成可复用 UI。
5. 用正式的黄巾之乱、虎牢关之战、徐州攻防战和大战吕布关卡替换当前占位关卡。
