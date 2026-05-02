# Prototype Battle UI

## Usage

在 Unity 中打开任意场景，然后点击：

```text
UnifyCountry / Prototype / Create Battle UI Preview
```

菜单会在当前场景创建一个 `PrototypeBattleUi` 对象，并自动读取：

- `Assets/_Project/Configs/Cards/cards_v001.csv`
- `Assets/_Project/Configs/Cards/units_v001.csv`
- `Assets/_Project/Configs/Cards/effects_v001.csv`
- `Assets/_Project/Configs/Cards/player_starting_deck_v001.csv`
- `Assets/_Project/Configs/Waves/battle_001_waves_v001.csv`

## Current Preview

当前 UI 会生成：

- 左侧友方阵地
- 右侧敌方波次
- 3 排 x 5 列的伪 2.5D 横向错位棋盘站位
- 各排格子与单位保持相同尺寸，通过排距、错位和阴影表现深度
- 友方大本营显示在阵地上方居中位置，并同时显示生命值文本与血条
- 当前关卡、回合数和下一波进度
- 底部手牌、抽牌堆、弃牌堆与费用
- 结束回合按钮
- 根据卡牌数据生成的卡通风卡牌块
- 英雄卡会显示从 `effects_v001.csv` 读取到的技能名

## Playable Prototype

进入 Unity Play 模式后，当前版本已经支持最小战斗循环：

- 首回合英雄卡自动进入手牌。
- 初始回合手牌为 5 张，若英雄卡超过 5 张，则随机选择 5 张英雄卡进入手牌。
- 初始回合结束时，手牌中未使用的牌进入弃牌堆，抽牌堆中剩余的英雄卡也进入弃牌堆。
- 后续回合每回合抽 3 张牌，不再触发英雄卡保底。
- 初始准备回合拥有 5 点费用，后续回合统一恢复到 3 点费用。
- 玩家英雄费用从 `cards_v001.csv` 读取，当前刘备为 1，关羽、张飞、赵云、马超为 2。
- 波次配置支持按关卡分组，并能指定敌人出生在哪一排。
- 当前包含 2 个关卡；第 1 关沿用原有三波，第 2 关包含多排出兵与曹操后排跟随。
- 点击手牌可以将单位上阵。
- 可以拖动手牌到友方阵地空格上阵。
- 可以在同排两个单位之间，或该排最右侧单位右边插入新单位；插入只在同一排内挤位。
- 上阵会消耗费用，费用不足时无法出牌。
- 战斗记录会保留每回合历史，并在固定字号的滚动区域中显示。
- 友方单位按排从右向左承伤，每排最右侧优先被攻击。
- 点击结束回合后，当前波次敌人出现。
- 敌方先从左到右攻击。
- 友方随后从右到左反击。
- 敌我攻击只寻找同排目标；敌方同排没有友方单位时攻击玩家大本营，友方同排没有敌方单位时不造成伤害。
- 血量归零的单位会被移除。
- 死亡结算后，友方每排向右补位，敌方每排向左补位。
- 回合结束后抽 3 张牌。
- 关卡胜利后可以进入下一关。
- 点击重开可以重置当前关卡。
- 英雄技能已接入第一版触发机制：支持上阵、攻击、受伤前和受伤后触发。
- 当前效果类型支持治疗、攻击加成、护盾、伤害、额外伤害、替代攻击和单次伤害上限。
- 单位 token 会在名称下方、血条上方显示状态短文本，例如 `攻+1`、`盾2`、`免1`、`限4`。

## Data Files

当前原型使用 CSV 驱动卡牌、单位和技能：

```text
cards_v001.csv
  card_id, card_name, card_type, cost, camp, faction, rarity,
  max_copies_in_deck, art_id, effect_id, description_key

units_v001.csv
  card_id, unit_id, unit_name, unit_type, hp, attack, role, tags,
  skill_effect_ids

effects_v001.csv
  effect_id, effect_name, timing, effect_type, target_rule, value,
  secondary_value, tags, description
```

`CardRecord` 会读取卡牌通用信息，并通过 `card_id` 合并 `UnitRecord` 和 `EffectRecord`。

当前支持的触发时机：

- `OnPlay`：单位上阵或敌方波次生成时触发。
- `OnAttack`：单位攻击时触发。
- `BeforeDamaged`：单位受伤前触发，用于伤害修正。
- `OnDamaged`：单位受伤后触发。

当前支持的效果类型：

- `Heal`
- `BuffAttack`
- `GainShield`
- `Damage`
- `BonusDamage`
- `ReplaceAttack`
- `DamageCap`

当前支持的目标规则：

- `Self`
- `CurrentTarget`
- `Attacker`
- `AllyFrontSameRow`
- `AllyAllSameRow`
- `EnemyAllSameRow`

## Next Step

下一步可以接入：

- 攻击动画
- 战斗胜负弹窗
- 更完整的 BuffInstance 数据结构与持续回合机制
- ScriptableObject 数据资产
