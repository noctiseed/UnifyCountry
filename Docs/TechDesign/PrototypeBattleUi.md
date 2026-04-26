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

## Next Step

下一步可以接入：

- 卡牌拖拽
- 卡牌上阵
- 结束回合后刷怪
- 敌方先攻与友方反击
- 血量扣减和死亡移除
