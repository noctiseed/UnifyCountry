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
- 当前初始牌库包含刘备、关羽、张飞各 1 张，盾牌兵 x3，弓箭手 x3，以及滚石、疗伤、加固各 1 张。
- 刘备是主公卡，初始牌库有且只有 1 张，奖励池中不会再出现；除刘备外的其他玩家卡牌都可以通过奖励重复获得。
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
- 战斗记录会保留整个 Run 的历史，并在固定字号的滚动区域中显示；重新开始整个 Run 时清空。
- 友方单位按排从右向左承伤，每排最右侧优先被攻击。
- 正式回合开始时，当前回合对应敌方波次先进场，但不触发攻击。
- 正式回合开始时，费用恢复到 3 点，抽 3 张牌，然后触发场上玩家单位的 `OnTurnStart` 效果。
- 玩家点击结束回合后，才进入战斗结算。
- 回合战斗结算按前军、中军、后军顺序推进，也就是从屏幕下方一排开始，逐排结算到屏幕上方。
- 每一排内先结算敌方攻击，再结算友方反击，然后进入下一排。
- 敌方在当前排内按长条队列从前到后攻击。
- 友方在当前排内从右到左反击。
- 敌我攻击只寻找同排目标；敌方同排没有友方单位时攻击玩家大本营，友方同排没有敌方单位时不造成伤害。
- 普通单位攻击已接入 DOTween 动效：单位攻击单位时会临时提到顶层动画层，移动到目标面前攻击后回位；单位攻击大本营时保留短前冲。
- 每次单位攻击前会记录全场血量，攻击结算后对所有实际扣血对象播放受击抖动、闪光和伤害飘字；因此荆棘反伤、额外伤害和其他攻击过程中的连带扣血都会有反馈。
- 攻击动效中的临时 UI tween 会通过 `SetTarget`、`SetLink(..., KillOnDestroy)` 和销毁兜底组件绑定生命周期，避免单位死亡、补位或 UI 刷新时继续访问已销毁的 `CanvasGroup`。
- 血量归零的单位会被移除。
- 死亡结算后，友方每排向右补位；敌方每排保持长条队列顺序，移除死亡单位后后续单位向前补位。
- 进入下一正式回合后费用恢复到 3 点，并抽 3 张牌。
- 关卡胜利后可以进入下一关。
- 点击重开可以重置当前关卡。
- 英雄技能已接入第一版触发机制：支持上阵、攻击、受伤前、受伤后、玩家正式回合开始和玩家回合结束触发。
- 简雍「论客」已接入 `OnTurnStart`：当简雍存活在场上时，每个玩家正式回合开始额外抽 1 张牌。
- 当前计谋牌包括滚石、齐射、疗伤、强化、加固、火攻、火箭、斩杀和草船借箭。
- 复苏 Buff 已接入：回合战斗结算后，存活单位每回合触发一次复苏，按触发前层数恢复生命，并使复苏层数减少 1；回合末 Buff 结算时灼烧优先于复苏，若单位死于灼烧则不会触发复苏。灼烧伤害会先尝试消耗护盾或免疫抵挡整次灼烧，再由护甲按点数抵挡剩余伤害。
- 护甲 Buff 已接入：护甲为常驻点数型 Buff，受到敌方单位攻击时，每点护甲抵挡 1 点伤害并消耗对应点数；护盾优先于护甲消耗。
- 荆棘 Buff 已接入：单位受到敌方单位攻击时，会使攻击者受到等同于当前荆棘层数的反伤；即使本次攻击被护盾、免疫或护甲抵挡，也会触发荆棘；荆棘为常驻型 Buff。
- 当前效果类型支持治疗、攻击加成、最大血量加成、次数型护盾、护甲、复苏、灼烧、荆棘、伤害、击杀返费、抽牌、额外伤害、替代攻击和单次伤害上限。
- 单位 token 会在名称下方、血条上方显示攻击图标和 Buff 图标。护盾、护甲、免疫、复苏、灼烧、荆棘均只通过 Buff 图标与 tooltip 展示；状态短文本仅保留少量非 Buff 型规则，例如 `限4`。

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

`unit_type` 当前支持 `Soldier`、`Hero` 和 `Boss`。Boss 仍属于 `CardType.Unit`，通过 `camp = Enemy` 与玩家版本同名角色区分，并可拥有独立属性、技能和战斗结算逻辑。

`BattleUnit` 在运行时持有独立阵营。普通兵种卡在配置中使用 `Neutral` 阵营作为共享模板：玩家上阵时生成 `Player` 阵营单位，敌方波次生成时生成 `Enemy` 阵营单位；英雄和计谋牌仍以卡牌表中的 `camp` 作为归属限制。

当前支持的触发时机：

- `OnPlay`：单位上阵或敌方波次生成时触发。
- `OnPlayAndTurnStart`：复合时机，单位上阵或敌方波次生成时触发一次，随后每个正式回合开始时继续触发；当前用于张角「太平要术」。
- `OnAttack`：单位攻击时触发。
- `BeforeDamaged`：单位受伤前触发，用于伤害修正。
- `OnDamaged`：单位受伤后触发。普通攻击、英雄技能伤害和计谋牌伤害都会触发；只有实际扣除生命且目标受击后仍存活时才触发，直接死亡不触发。
- `OnTurnStart`：玩家正式回合开始时，对场上存活的玩家单位触发。
- `OnTurnEnd`：玩家回合战斗结算中，我方反击结束后、回合末 Buff 结算前，对场上存活的玩家单位触发。

当前支持的效果类型：

- `Heal`
- `BuffAttack`
- `GainShield`：增加护盾层数，每层护盾可抵挡一次敌方攻击。
- `GainArmor`：增加护甲点数，每点护甲可抵挡 1 点敌方攻击伤害，抵挡后消耗。
- `FortifySameRowAndSelf`：复合护甲效果，按目标规则给一组单位增加护甲，并给施放者额外增加护甲；当前用于张梁「人公固阵」。
- `ScorchEnemyRowAndShieldSelf`：复合灼烧效果，按目标规则给一组敌对单位施加灼烧，并给施放者护盾；当前用于张宝「地公妖火」。
- `TaipingDoctrine`：复合 Boss 效果，登场时给同阵营存活单位复苏并给施放者护盾，之后每回合按目标规则施加灼烧；当前用于张角「太平要术」。
- `Damage`
- `BonusDamage`
- `ReplaceAttack`
- `DamageCap`
- `DrawCards`
- `HealAndGainRevival`
- `BuffAttackAndMaxHp`
- `GainMaxHpAndRevival`
- `GainBurn`
- `DamageAndGainBurn`
- `DamageGainEnergyOnKill`
- `GainThorns`：增加荆棘层数，受到敌方单位攻击时反伤攻击者。

伤害触发规则：

- `DealDamage` 会先结算护盾 / 免疫攻击次数，再结算 `BeforeDamaged` 伤害上限，然后消耗护甲抵挡剩余伤害，最后实际扣血。
- 当 `triggerDamaged` 为 true、实际扣血大于 0 且目标未死亡时，触发目标的 `OnDamaged` 效果。
- 普通攻击、英雄技能伤害、替代攻击伤害和计谋牌伤害都会以 `triggerDamaged = true` 结算。
- 计谋牌伤害当前以无来源伤害结算，因此不会消耗护盾 / 免疫攻击次数 / 护甲，但仍会触发 `BeforeDamaged` 和符合条件的 `OnDamaged`。
- 夏侯惇「刚烈」依赖 `OnDamaged`：受到普通攻击、英雄技能伤害或计谋牌伤害后若仍存活，攻击力 +1；若该次伤害直接死亡，则不加攻。

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
- `PLAN_006` 火攻：`DamageAndGainBurn` + `EnemySingle`，造成 3 点伤害并施加 2 层灼烧，费用 2。
- `PLAN_007` 火箭：`DamageAndGainBurn` + `EnemyRow`，对敌方整排所有单位造成 1 点伤害并施加 2 层灼烧，费用 2。
- `PLAN_008` 斩杀：`DamageGainEnergyOnKill` + `EnemySingle`，造成 1 点伤害，若直接击杀目标则获得 1 点可用费用。
- `PLAN_009` 草船借箭：`DrawCards` + `NoTarget`，抽 2 张牌。

## Next Step

下一步可以接入：

- 技能牌释放特效与行目标特效
- 攻击动效继续打磨，例如并行受击反馈、镜头轻震和更完整的特效资源
- 战斗胜负弹窗
- 更完整的 BuffInstance 数据结构与持续回合机制，例如将复苏、护盾、护甲、免疫、临时攻击等统一到可配置持续时间
- ScriptableObject 数据资产

## UI Implementation Notes

当前原型 UI 主要使用运行时代码生成，相关脚本集中在 `Assets/_Project/Code/Runtime/UI`：

- `MainMenuUi.cs`：首页。
- `CardCollectionUi.cs`：卧虎藏龙卡牌汇总页。
- `PrototypeBattleUi*.cs`：战斗页面及其 partial 拆分，包括卡牌、单位、牌堆、结算、拖拽和施法等模块。

这种方式适合早期快速验证规则和交互，但随着页面增多，`UI` 目录会同时承载页面、组件、布局工具和交互脚本，维护成本会逐渐上升。

后续更推荐逐步迁移到 **Prefab + Controller** 的方式：

```text
Assets/_Project/Prefabs/UI/
  Pages/
    MainMenuPage.prefab
    CardCollectionPage.prefab
    BattlePage.prefab
  Components/
    CardView.prefab
    UnitView.prefab
    BuffIcon.prefab

Assets/_Project/Code/Runtime/UI/
  Common/
    CardView.cs
    UnitView.cs
  MainMenu/
    MainMenuController.cs
  CardCollection/
    CardCollectionController.cs
  Battle/
    BattleUiController.cs
    BattleDragHandlers.cs
    SkillCastingComponents.cs
```

推荐迁移节奏：

1. 新页面优先用 Prefab 搭静态结构，代码只负责绑定数据和按钮事件。
2. 先抽重复组件，例如 `CardView.prefab`，让战斗手牌、奖励牌和卧虎藏龙卡牌共用同一套表现。
3. 再把页面级脚本按功能拆目录，避免所有页面继续堆在 `Runtime/UI` 根目录。
4. 战斗 UI 可以最后迁移，因为它包含拖拽、施法、动画和状态刷新，风险最高。

短期内保留现有代码生成 UI 是可以接受的；中期目标是让美术布局、字体、间距、按钮状态等尽量在 Prefab 中调整，代码只保留数据驱动和交互逻辑。
