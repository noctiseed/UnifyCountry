# Prototype Battle UI

## Usage

在 Unity 中打开任意场景，然后点击：

```text
UnifyCountry / Prototype / Create Battle UI Preview
```

菜单会在当前场景创建一个 `PrototypeBattleUi` 对象，并自动读取：

- `Assets/_Project/Configs/Cards/cards_v001.csv`
- `Assets/_Project/Configs/Cards/player_starting_deck_v001.csv`
- `Assets/_Project/Configs/Waves/battle_001_waves_v001.csv`

## Current Preview

当前 UI 会生成：

- 左侧友方阵地
- 右侧敌方波次
- 底部第一回合手牌 / 初始牌库预览
- 结束回合按钮
- 根据卡牌数据生成的卡通风卡牌块

## Playable Prototype

进入 Unity Play 模式后，当前版本已经支持最小战斗循环：

- 首回合英雄卡自动进入手牌。
- 点击手牌可以将单位上阵。
- 友方单位从右向左承伤，最右侧优先被攻击。
- 点击结束回合后，当前波次敌人出现。
- 敌方先从左到右攻击。
- 友方随后从右到左反击。
- 血量归零的单位会被移除。
- 回合结束后抽 3 张牌。
- 点击重开可以重置本场战斗。

## Next Step

下一步可以接入：

- 卡牌拖拽
- 费用系统
- 手牌弃牌堆
- 攻击动画
- 战斗胜负弹窗
- ScriptableObject 数据资产
