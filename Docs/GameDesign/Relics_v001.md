# Relics v001

## Current Scope

第一批遗物已实现战斗功能，并已接入“卧虎藏龙”图鉴页展示。暂未接入玩家获得方式。

当前遗物通过 `RunSession.Current.RelicIds` 持有。后续奖励、事件、商店或调试入口只需要向当前 Run 添加对应遗物 ID，即可在之后的战斗中生效。

代码入口：

```text
Assets/_Project/Code/Runtime/Roguelike/RunRelics.cs
Assets/_Project/Code/Runtime/Map/RunSession.cs
Assets/_Project/Code/Runtime/UI/CardCollectionUi.cs
```

“卧虎藏龙”页面当前使用左侧一级页签切换：

```text
卡牌
遗物
```

卡牌页保留原有卡牌筛选；遗物页展示当前 5 个遗物的静态定义。

## Relic List

| ID | 名称 | 效果 |
|---|---|---|
| `RELIC_001` | 传国玉玺 | 正式回合的费用上限 +1。初始准备回合仍为 5 费。 |
| `RELIC_002` | 铁甲军令 | 每回合第一次打出单位牌后，抽 1 张牌。 |
| `RELIC_003` | 太平符箓 | 每层复苏额外回复 1 点生命。 |
| `RELIC_004` | 铁蒺藜 | 每层荆棘额外造成 1 点反伤。 |
| `RELIC_005` | 火牛阵图 | 每波敌人入场时，对该波所有敌人造成 1 点伤害，并施加 1 层灼烧。 |

## Implementation Notes

### 传国玉玺

只影响正式回合。

```text
初始准备回合最大费用 = 5
正式回合最大费用 = 3 + FormalTurnMaxEnergyBonus
```

当前 `RELIC_001` 提供：

```text
FormalTurnMaxEnergyBonus += 1
```

### 铁甲军令

当前实现为每个正式回合第一次成功打出单位牌后抽 1 张牌，初始准备阶段不触发。

触发点：

```text
PrototypeBattleUi.CardsFormation
  PlayCardAt
  PlayCardInGap
```

每个玩家回合开始时重置触发标记。

### 太平符箓

不是“最终回复量翻倍”，而是每层复苏的回复量加算。

```text
复苏回复量 = 复苏层数 * (1 + RevivalHealBonusPerStack)
```

当前 `RELIC_003` 提供：

```text
RevivalHealBonusPerStack += 1
```

示例：

```text
5 层复苏，无遗物：5 * (1) = 5
5 层复苏，有太平符箓：5 * (1 + 1) = 10
5 层复苏，后续有另一个同类加成：5 * (1 + 1 + 1) = 15
```

### 铁蒺藜

不是“最终反伤翻倍”，而是每层荆棘的反伤加算。

```text
荆棘反伤 = 荆棘层数 * (1 + ThornsDamageBonusPerStack)
```

当前 `RELIC_004` 提供：

```text
ThornsDamageBonusPerStack += 1
```

### 火牛阵图

触发点在敌方波次生成后，只作用于本波新入场的敌人。

```text
WaveEntryDamage += 1
WaveEntryBurn += 1
```

当前效果会在波次生成后：

1. 对新生成敌人造成 1 点伤害。
2. 对仍存活的新生成敌人施加 1 层灼烧。
3. 如果有敌人因此死亡，直接从阵型中移除。

## Future Work

- 接入遗物获得入口。
- 将遗物定义数据化到 CSV 或 ScriptableObject。
- 为遗物触发增加更明确的战斗表现，例如飘字、图标闪烁、特效。
- 增加遗物数据校验。
