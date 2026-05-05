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
- 右侧敌方阵地与下一波提示
- 友方阵地为 3 排 x 5 列的伪 2.5D 横向错位棋盘站位
- 敌方阵地为 3 排长条队列；同排单位可以超过 5 个，默认按正常间距展开，数量过多时自动压缩间距并产生层叠遮挡
- 各排单位通过排距、错位、遮挡层级和阴影表现深度
- 友方大本营显示在阵地上方居中位置，并同时显示生命值文本与血条
- 当前关卡、回合数和下一波进度
- 底部手牌、抽牌堆、弃牌堆与费用；手牌区域不显示标题
- 结束回合按钮
- 根据卡牌数据生成的卡通风卡牌块
- 英雄卡和计谋牌会显示从 `effects_v001.csv` 读取到的效果名
- 计谋牌施法时会显示选中高亮、目标高亮和虚线指向箭头

## Playable Prototype

进入 Unity Play 模式后，当前版本已经支持最小战斗循环：

- 首回合英雄卡自动进入手牌。
- 初始回合手牌为 5 张，若英雄卡超过 5 张，则随机选择 5 张英雄卡进入手牌。
- 初始回合结束时，手牌中未使用的牌进入弃牌堆，抽牌堆中剩余的英雄卡也进入弃牌堆。
- 后续回合每回合抽 3 张牌，不再触发英雄卡保底。
- 初始准备回合拥有 5 点费用，后续回合统一恢复到 3 点费用。
- 玩家英雄费用从 `cards_v001.csv` 读取，当前刘备、简雍为 1，关羽、张飞、赵云、马超为 2。
- 当前初始牌库包含 6 张玩家英雄、盾牌兵 x5、弓箭手 x5，以及 7 张计谋牌。
- 波次配置支持按关卡分组，并能指定敌人出生在哪一排。
- 敌方波次生成时直接追加到对应排队列末尾；同排已有残留敌军时，不再因为 5 格上限而跳过后续敌人。
- 当前包含 3 个关卡；第 1 关为前军单线教学，第 2 关包含多排出兵与曹仁 Boss，第 3 关包含前中后三军递进、休整回合和曹操 Boss。
- 点击手牌可以将单位上阵。
- 可以拖动手牌到友方阵地空格上阵。
- 可以在同排两个单位之间，或该排最右侧单位右边插入新单位；插入只在同一排内挤位。
- 上阵会消耗费用，费用不足时无法出牌。
- 点击计谋牌会进入施法状态，右键或 `Esc` 可以取消施法。
- 计谋牌支持友方单体、敌方单体、敌方整排和无目标立即释放。
- 计谋牌释放后会消耗费用并进入弃牌堆。
- 战斗记录会保留每回合历史，并在固定字号的滚动区域中显示。
- 友方单位按排从右向左承伤，每排最右侧优先被攻击。
- 正式回合开始时，当前回合对应敌方波次先进场，但不触发攻击。
- 正式回合开始时，费用恢复到 3 点，抽 3 张牌，然后触发场上玩家单位的 `OnTurnStart` 效果。
- 玩家点击结束回合后，才进入战斗结算。
- 敌方按排内队列从前到后攻击。
- 友方随后从右到左反击。
- 敌我攻击只寻找同排目标；敌方同排没有友方单位时攻击玩家大本营，友方同排没有敌方单位时不造成伤害。
- 血量归零的单位会被移除。
- 死亡结算后，友方每排向右补位；敌方每排保持长条队列顺序，移除死亡单位后后续单位向前补位。
- 进入下一正式回合后费用恢复到 3 点，并抽 3 张牌。
- 关卡胜利后可以进入下一关。
- 点击重开可以重置当前关卡。
- 英雄技能已接入第一版触发机制：支持上阵、攻击、受伤前、受伤后和玩家正式回合开始触发。
- 简雍「论客」已接入 `OnTurnStart`：当简雍存活在场上时，每个玩家正式回合开始额外抽 1 张牌。
- 当前计谋牌包括滚石、齐射、疗伤、强化、加固、斩杀和草船借箭。
- 复苏 Buff 已接入：回合战斗结算后，存活单位每回合触发一次复苏，按触发前层数恢复生命，并使复苏层数减少 1。
- 当前效果类型支持治疗、攻击加成、最大血量加成、次数型护盾、复苏、伤害、击杀返费、抽牌、额外伤害、替代攻击和单次伤害上限。
- 单位 token 会在名称下方、血条上方显示状态短文本，例如 `攻+1`、`盾2`、`免1`、`复2`、`限4`。

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

`BattleUnit` 在运行时持有独立阵营。普通兵种卡在配置中使用 `Neutral` 阵营作为共享模板：玩家上阵时生成 `Player` 阵营单位，敌方波次生成时生成 `Enemy` 阵营单位；英雄和计谋牌仍以卡牌表中的 `camp` 作为归属限制。

当前支持的触发时机：

- `OnPlay`：单位上阵或敌方波次生成时触发。
- `OnAttack`：单位攻击时触发。
- `BeforeDamaged`：单位受伤前触发，用于伤害修正。
- `OnDamaged`：单位受伤后触发。
- `OnTurnStart`：玩家正式回合开始时，对场上存活的玩家单位触发。

当前支持的效果类型：

- `Heal`
- `BuffAttack`
- `GainShield`：增加护盾层数，每层护盾可抵挡一次敌方攻击。
- `Damage`
- `BonusDamage`
- `ReplaceAttack`
- `DamageCap`
- `DrawCards`
- `HealAndGainRevival`
- `BuffAttackAndMaxHp`
- `DamageGainEnergyOnKill`

当前支持的目标规则：

- `Self`
- `CurrentTarget`
- `Attacker`
- `AllyFrontSameRow`
- `AllyAllSameRow`
- `EnemyAllSameRow`
- `AllySingle`
- `EnemySingle`
- `EnemyRow`
- `NoTarget`

其中 `Self`、`CurrentTarget`、`Attacker`、`AllyFrontSameRow`、`AllyAllSameRow`、`EnemyAllSameRow` 用于单位技能目标解析；`AllySingle`、`EnemySingle`、`EnemyRow`、`NoTarget` 用于计谋牌施法目标需求判定。

当前计谋牌效果：

- `PLAN_001` 滚石：`Damage` + `EnemySingle`，对敌方单体造成 3 点伤害。
- `PLAN_002` 齐射：`Damage` + `EnemyRow`，对敌方整排所有单位造成 1 点伤害。
- `PLAN_003` 疗伤：`HealAndGainRevival` + `AllySingle`，治疗 2 点并获得 2 层复苏。
- `PLAN_004` 强化：`BuffAttackAndMaxHp` + `AllySingle`，攻击 +2，最大血量 +2，并恢复 2 点生命。
- `PLAN_005` 加固：`GainShield` + `AllySingle`，获得 1 层护盾。
- `PLAN_008` 斩杀：`DamageGainEnergyOnKill` + `EnemySingle`，造成 1 点伤害，若直接击杀目标则获得 1 点可用费用。
- `PLAN_009` 草船借箭：`DrawCards` + `NoTarget`，抽 2 张牌。

## Next Step

下一步可以接入：

- 攻击动画
- 战斗胜负弹窗
- 更完整的 BuffInstance 数据结构与持续回合机制，例如将复苏、护盾、免疫、临时攻击等统一到可配置持续时间
- ScriptableObject 数据资产
