using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi : MonoBehaviour
    {
        [System.Serializable]
        private sealed class CardPortraitEntry
        {
            public string cardId;
            public Sprite portrait;
        }

        [Header("Config")]
        [SerializeField] private TextAsset cardsCsv;
        [SerializeField] private TextAsset unitsCsv;
        [SerializeField] private TextAsset effectsCsv;
        [SerializeField] private TextAsset startingDeckCsv;
        [SerializeField] private TextAsset wavesCsv;

        [Header("Style")]
        [SerializeField] private Font uiFont;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1600f, 900f);

        [Header("Card Art")]
        [SerializeField] private List<CardPortraitEntry> cardPortraits = new List<CardPortraitEntry>();

        private readonly Color backgroundColor = new Color(0.94f, 0.89f, 0.73f);
        private readonly Color playerPanelColor = new Color(0.76f, 0.92f, 0.67f);
        private readonly Color enemyPanelColor = new Color(0.96f, 0.68f, 0.58f);
        private readonly Color handPanelColor = new Color(0.99f, 0.94f, 0.72f);
        private readonly Color heroCardColor = new Color(1f, 0.82f, 0.36f);
        private readonly Color soldierCardColor = new Color(0.66f, 0.88f, 1f);
        private readonly Color enemyCardColor = new Color(1f, 0.56f, 0.5f);
        private Sprite roundedButtonSprite;
        private const int MaxFormationSlots = 5;
        private const int FormationRows = 3;
        private const int TotalFormationSlots = MaxFormationSlots * FormationRows;
        private const int MaxEnergy = 3;
        private const int InitialHandSize = 5;
        private const int CardsDrawnPerTurn = 3;
        private const int PlayerBaseMaxHp = 10;
        private const float FormationMoveDuration = 0.45f;
        private const string InitialBattleLog = "拖动手牌到友方阵地上阵，然后点击结束回合。";

        private Dictionary<string, CardRecord> cardMap = new Dictionary<string, CardRecord>();
        private Dictionary<string, Sprite> cardPortraitMap = new Dictionary<string, Sprite>();
        private readonly List<CardRecord> drawPile = new List<CardRecord>();
        private readonly List<CardRecord> discardPile = new List<CardRecord>();
        private readonly List<CardRecord> hand = new List<CardRecord>();
        private readonly List<string> battleLogHistory = new List<string>();
        private readonly List<BattleUnit> playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit> enemyUnits = new List<BattleUnit>();
        private readonly Dictionary<int, RectTransform> unitViews = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, int> animatedSlotOverrides = new Dictionary<int, int>();
        private List<BattleLevelRecord> levels = new List<BattleLevelRecord>();

        private int turnNumber = 1;
        private int currentLevelIndex;
        private int nextWaveIndex;
        private int nextUnitRuntimeId = 1;
        private int currentEnergy = MaxEnergy;
        private int playerBaseHp = PlayerBaseMaxHp;
        private string battleLog = InitialBattleLog;
        private bool initialized;
        private bool isResolvingTurn;
        private bool battleEnded;

        [ContextMenu("Rebuild Preview UI")]
        public void Rebuild()
        {
            if (!initialized)
                InitializeBattle();

            BuildUi();
        }

        public void ResetBattle()
        {
            StopAllCoroutines();
            isResolvingTurn = false;
            battleEnded = false;
            initialized = false;
            InitializeBattle();
            BuildUi();
        }

        public void StartNextLevel()
        {
            if (currentLevelIndex + 1 >= levels.Count)
                return;

            currentLevelIndex++;
            ResetBattle();
        }

        private void Awake()
        {
            ResetBattle();
        }

        private void InitializeBattle()
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cards = PrototypeCsvDatabase.LoadCards(cardsCsv, unitsCsv, effectsCsv);
            cardMap = cards.ToDictionary(card => card.CardId);
            RebuildCardPortraitMap();
            levels = PrototypeCsvDatabase.LoadBattleLevels(wavesCsv);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, Mathf.Max(0, levels.Count - 1));

            drawPile.Clear();
            discardPile.Clear();
            hand.Clear();
            battleLogHistory.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            for (var i = 0; i < TotalFormationSlots; i++)
            {
                playerUnits.Add(null);
                enemyUnits.Add(null);
            }

            nextUnitRuntimeId = 1;
            battleEnded = false;
            playerBaseHp = PlayerBaseMaxHp;

            var startingDeck = PrototypeCsvDatabase.LoadStartingDeck(startingDeckCsv);
            foreach (var entry in startingDeck)
            {
                if (!cardMap.TryGetValue(entry.Key, out var card))
                    continue;

                for (var i = 0; i < entry.Value; i++)
                    drawPile.Add(card);
            }

            turnNumber = 1;
            nextWaveIndex = 0;
            currentEnergy = MaxEnergy;
            Shuffle(drawPile);
            DrawInitialHand();
            AddBattleLogEntry($"准备阶段：英雄卡优先进入初始手牌，补足 {InitialHandSize} 张。");
            initialized = true;
        }

        private void DrawInitialHand()
        {
            var heroCards = drawPile
                .Where(card => card.CardType == CardType.Unit && card.UnitType == UnitType.Hero && card.Camp == CardCamp.Player)
                .ToList();

            Shuffle(heroCards);

            var heroCount = Mathf.Min(InitialHandSize, heroCards.Count);
            for (var i = 0; i < heroCount; i++)
            {
                var hero = heroCards[i];
                if (drawPile.Remove(hero))
                    hand.Add(hero);
            }

            DrawCards(InitialHandSize - hand.Count);
        }

        private void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!DrawOneCard())
                    break;
            }
        }

        private bool DrawOneCard()
        {
            if (drawPile.Count == 0)
                RefillDrawPileFromDiscard();

            if (drawPile.Count == 0)
                return false;

            var card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
            return true;
        }

        private void RefillDrawPileFromDiscard()
        {
            if (discardPile.Count == 0)
                return;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var value = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = value;
            }
        }

        private void PlayCard(CardRecord card)
        {
            PlayCardAt(card, GetFirstEmptyPlayerSlot());
        }

        internal bool CanDragCard(CardRecord card)
        {
            return card != null
                && card.CardType == CardType.Unit
                && card.Unit != null
                && !isResolvingTurn
                && currentEnergy >= card.Cost;
        }

        internal void PlayCardAt(CardRecord card, int insertIndex)
        {
            if (card == null || card.Camp != CardCamp.Player || card.CardType != CardType.Unit || card.Unit == null)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                BuildUi();
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, TotalFormationSlots - 1);
            if (playerUnits[insertIndex] != null)
            {
                AddBattleLogEntry("该阵地位置已有单位。");
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            currentEnergy -= card.Cost;
            playerUnits[insertIndex] = new BattleUnit(card, nextUnitRuntimeId++);
            AddBattleLogEntry($"{card.CardName} 上阵，消耗 {card.Cost} 点费用。");
            BuildUi();
        }

        internal void PlayCardInGap(CardRecord card, int gapIndex)
        {
            if (card == null || card.Camp != CardCamp.Player || card.CardType != CardType.Unit || card.Unit == null)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= TotalFormationSlots)
            {
                AddBattleLogEntry("友方阵地已满，无法继续上阵。");
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            var unit = new BattleUnit(card, nextUnitRuntimeId++);
            if (!TryInsertPlayerUnitAtGap(unit, gapIndex))
            {
                hand.Add(card);
                AddBattleLogEntry("当前同排插入位置不可用。");
                BuildUi();
                return;
            }

            currentEnergy -= card.Cost;
            AddBattleLogEntry($"{card.CardName} 插入阵地，消耗 {card.Cost} 点费用。");
            BuildUi();
        }

        private bool TryInsertPlayerUnitAtGap(BattleUnit unit, int gapIndex)
        {
            var row = DecodeGapRow(gapIndex);
            var afterColumn = DecodeGapAfterColumn(gapIndex);
            if (row < 0 || row >= FormationRows || afterColumn < 0 || afterColumn >= MaxFormationSlots)
                return false;

            var rowSlots = GetOccupiedPlayerSlotsInRow(row);
            if (rowSlots.Count >= MaxFormationSlots)
                return false;

            var afterSlot = GetSlotIndex(row, afterColumn);
            if (playerUnits[afterSlot] == null || playerUnits[afterSlot].IsDead)
                return false;

            var targetColumn = afterColumn + 1;
            if (targetColumn >= MaxFormationSlots)
            {
                var emptyLeft = FindEmptyLeftInRow(row, afterColumn);
                if (emptyLeft < 0)
                    return false;

                ShiftLeftInRow(row, emptyLeft, afterColumn);
                playerUnits[afterSlot] = unit;
                return true;
            }

            var targetSlot = GetSlotIndex(row, targetColumn);
            if (playerUnits[targetSlot] == null)
            {
                playerUnits[targetSlot] = unit;
                return true;
            }

            var emptyRight = FindEmptyRightInRow(row, targetColumn);
            if (emptyRight >= 0)
            {
                ShiftRightInRow(row, targetColumn, emptyRight);
                playerUnits[targetSlot] = unit;
                return true;
            }

            var emptyLeftForGap = FindEmptyLeftInRow(row, afterColumn);
            if (emptyLeftForGap >= 0)
            {
                ShiftLeftInRow(row, emptyLeftForGap, afterColumn);
                playerUnits[afterSlot] = unit;
                return true;
            }

            return false;
        }

        private int FindEmptyLeftInRow(int row, int fromColumn)
        {
            for (var column = fromColumn - 1; column >= 0; column--)
            {
                if (playerUnits[GetSlotIndex(row, column)] == null)
                    return column;
            }

            return -1;
        }

        private int FindEmptyRightInRow(int row, int fromColumn)
        {
            for (var column = fromColumn + 1; column < MaxFormationSlots; column++)
            {
                if (playerUnits[GetSlotIndex(row, column)] == null)
                    return column;
            }

            return -1;
        }

        private void ShiftRightInRow(int row, int fromColumn, int emptyColumn)
        {
            for (var column = emptyColumn; column > fromColumn; column--)
                playerUnits[GetSlotIndex(row, column)] = playerUnits[GetSlotIndex(row, column - 1)];
        }

        private void ShiftLeftInRow(int row, int emptyColumn, int toColumn)
        {
            for (var column = emptyColumn; column < toColumn; column++)
                playerUnits[GetSlotIndex(row, column)] = playerUnits[GetSlotIndex(row, column + 1)];
        }

        private List<int> GetOccupiedPlayerSlotsInRow(int row)
        {
            var slots = new List<int>();
            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var slot = GetSlotIndex(row, column);
                if (playerUnits[slot] != null && !playerUnits[slot].IsDead)
                    slots.Add(slot);
            }

            return slots;
        }

        private int GetFirstEmptyPlayerSlot()
        {
            var preferredRows = new[] { 1, 0, 2 };
            foreach (var row in preferredRows)
            {
                for (var column = MaxFormationSlots - 1; column >= 0; column--)
                {
                    var slotIndex = GetSlotIndex(row, column);
                    if (playerUnits[slotIndex] == null)
                        return slotIndex;
                }
            }

            return GetSlotIndex(1, MaxFormationSlots - 1);
        }

        private int GetFirstEmptyEnemySlotInRow(int row)
        {
            if (row < 0 || row >= FormationRows)
                return -1;

            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var slotIndex = GetSlotIndex(row, column);
                if (enemyUnits[slotIndex] == null)
                    return slotIndex;
            }

            return -1;
        }

        private int CountPlayerUnits()
        {
            var count = 0;
            foreach (var unit in playerUnits)
            {
                if (unit != null && !unit.IsDead)
                    count++;
            }

            return count;
        }

        private int CountEnemyUnits()
        {
            var count = 0;
            foreach (var unit in enemyUnits)
            {
                if (unit != null && !unit.IsDead)
                    count++;
            }

            return count;
        }

        private BattleLevelRecord CurrentLevel => levels.Count == 0 ? null : levels[currentLevelIndex];

        private List<WaveSpawnRecord> CurrentWaves => CurrentLevel == null ? null : CurrentLevel.Waves;

        private bool HasNextLevel => currentLevelIndex + 1 < levels.Count;

        private void EndTurn()
        {
            if (isResolvingTurn)
                return;

            StartCoroutine(ResolveTurnRoutine());
        }

        private IEnumerator ResolveTurnRoutine()
        {
            isResolvingTurn = true;

            var logLines = new List<string>();
            logLines.Add($"第 {turnNumber} 回合结束。");
            DiscardHand(logLines);
            DiscardUnplayedInitialHeroes(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            SpawnCurrentWave(logLines);
            UpdateActiveTurnLog(logLines);
            BuildUi();
            yield return new WaitForSeconds(0.75f);

            ResolveEnemyAttack(logLines);
            UpdateActiveTurnLog(logLines);
            BuildUi();
            if (playerBaseHp <= 0)
            {
                logLines.Add("大本营被攻破，战斗失败。");
                CommitTurnLog(logLines);
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            yield return new WaitForSeconds(0.75f);

            ResolvePlayerAttack(logLines);
            AppendDeathLogs(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            var waves = CurrentWaves;
            if (CountEnemyUnits() == 0 && (waves == null || nextWaveIndex >= waves.Count))
            {
                logLines.Add(HasNextLevel ? $"第 {currentLevelIndex + 1} 关胜利！" : "战斗胜利！");
                CommitTurnLog(logLines);
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            turnNumber++;
            currentEnergy = MaxEnergy;
            var drawCount = DrawCardsWithCount(CardsDrawnPerTurn);
            logLines.Add($"进入第 {turnNumber} 回合，费用恢复到 {MaxEnergy}，从抽牌堆抽 {drawCount} 张牌。");
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            BuildUi();
        }

        private int DrawCardsWithCount(int count)
        {
            var drawn = 0;
            for (var i = 0; i < count; i++)
            {
                if (!DrawOneCard())
                    break;

                drawn++;
            }

            return drawn;
        }

        private void AddBattleLogEntry(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            battleLogHistory.Add(line);
            battleLog = ComposeBattleLog(null);
        }

        private void UpdateActiveTurnLog(List<string> activeLines)
        {
            battleLog = ComposeBattleLog(activeLines);
        }

        private void CommitTurnLog(List<string> activeLines)
        {
            if (activeLines != null)
                battleLogHistory.AddRange(activeLines.Where(line => !string.IsNullOrWhiteSpace(line)));

            battleLog = ComposeBattleLog(null);
        }

        private string ComposeBattleLog(List<string> activeLines)
        {
            var lines = new List<string>();
            if (battleLogHistory.Count > 0)
                lines.AddRange(battleLogHistory);

            if (activeLines != null && activeLines.Count > 0)
            {
                if (lines.Count > 0)
                    lines.Add(string.Empty);

                lines.AddRange(activeLines);
            }

            return lines.Count == 0 ? InitialBattleLog : string.Join("\n", lines);
        }

        private void DiscardHand(List<string> logLines)
        {
            if (hand.Count == 0)
                return;

            var count = hand.Count;
            discardPile.AddRange(hand);
            hand.Clear();
            logLines.Add($"未使用的 {count} 张手牌进入弃牌堆。");
        }

        private void DiscardUnplayedInitialHeroes(List<string> logLines)
        {
            if (turnNumber != 1)
                return;

            var heroes = drawPile
                .Where(card => card.CardType == CardType.Unit && card.UnitType == UnitType.Hero && card.Camp == CardCamp.Player)
                .ToList();

            if (heroes.Count == 0)
                return;

            foreach (var hero in heroes)
            {
                drawPile.Remove(hero);
                discardPile.Add(hero);
            }

            logLines.Add($"抽牌堆中剩余的 {heroes.Count} 张英雄卡进入弃牌堆。");
        }

        private void SpawnCurrentWave(List<string> logLines)
        {
            var waves = CurrentWaves;
            if (waves == null || nextWaveIndex >= waves.Count)
                return;

            var wave = waves[nextWaveIndex];
            nextWaveIndex++;

            var spawnedNames = new List<string>();
            for (var row = 0; row < Mathf.Min(FormationRows, wave.RowCardIds.Length); row++)
            {
                foreach (var cardId in wave.RowCardIds[row])
                {
                    var spawnSlot = GetFirstEmptyEnemySlotInRow(row);
                    if (spawnSlot < 0)
                        break;

                    if (!cardMap.TryGetValue(cardId, out var card))
                        continue;

                    enemyUnits[spawnSlot] = new BattleUnit(card, nextUnitRuntimeId++);
                    spawnedNames.Add($"{card.CardName}(第 {row + 1} 排)");
                }
            }

            if (spawnedNames.Count > 0)
                logLines.Add($"第 {currentLevelIndex + 1} 关第 {nextWaveIndex} 波出现：{string.Join("、", spawnedNames)}。");
        }

        private void ResolveEnemyAttack(List<string> logLines)
        {
            for (var row = 0; row < FormationRows; row++)
            {
                for (var column = 0; column < MaxFormationSlots; column++)
                {
                    var attacker = enemyUnits[GetSlotIndex(row, column)];
                    if (attacker == null || attacker.IsDead)
                        continue;

                    var target = GetPlayerFrontUnit(row);
                    if (target == null)
                    {
                        playerBaseHp = Mathf.Max(0, playerBaseHp - attacker.Attack);
                        logLines.Add($"{attacker.Name} 攻击第 {row + 1} 排大本营，造成 {attacker.Attack} 点伤害。");
                        if (playerBaseHp <= 0)
                            return;

                        continue;
                    }

                    target.TakeDamage(attacker.Attack);
                    logLines.Add($"{attacker.Name} 攻击第 {row + 1} 排 {target.Name}，造成 {attacker.Attack} 点伤害。");
                }
            }
        }

        private void ResolvePlayerAttack(List<string> logLines)
        {
            for (var row = 0; row < FormationRows; row++)
            {
                for (var column = MaxFormationSlots - 1; column >= 0; column--)
                {
                    var attacker = playerUnits[GetSlotIndex(row, column)];
                    if (attacker == null || attacker.IsDead)
                        continue;

                    var target = GetEnemyFrontUnit(row);
                    if (target == null)
                        continue;

                    target.TakeDamage(attacker.Attack);
                    logLines.Add($"{attacker.Name} 反击第 {row + 1} 排 {target.Name}，造成 {attacker.Attack} 点伤害。");
                }
            }
        }

        private BattleUnit GetPlayerFrontUnit(int row)
        {
            for (var column = MaxFormationSlots - 1; column >= 0; column--)
            {
                var unit = playerUnits[GetSlotIndex(row, column)];
                if (unit != null && !unit.IsDead)
                    return unit;
            }

            return null;
        }

        private BattleUnit GetEnemyFrontUnit(int row)
        {
            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var unit = enemyUnits[GetSlotIndex(row, column)];
                if (unit != null && !unit.IsDead)
                    return unit;
            }

            return null;
        }

        private IEnumerator AdvanceFormationsRoutine(List<string> logLines)
        {
            var moves = new List<FormationMove>();
            moves.AddRange(AdvanceFormation(playerUnits, true));
            moves.AddRange(AdvanceFormation(enemyUnits, false));

            if (moves.Count == 0)
                yield break;

            logLines.Add("阵型向前补位。");
            UpdateActiveTurnLog(logLines);

            animatedSlotOverrides.Clear();
            foreach (var move in moves)
                animatedSlotOverrides[move.UnitRuntimeId] = move.FromSlotIndex;

            BuildUi();
            yield return StartCoroutine(AnimateFormationMoves(moves));
            animatedSlotOverrides.Clear();

            BuildUi();
            yield return new WaitForSeconds(0.15f);
        }

        private List<FormationMove> AdvanceFormation(List<BattleUnit> units, bool playerSide)
        {
            var oldSlots = new Dictionary<int, int>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit != null && !unit.IsDead)
                    oldSlots[unit.RuntimeId] = i;
            }

            for (var row = 0; row < FormationRows; row++)
            {
                var aliveUnits = new List<BattleUnit>();
                for (var column = 0; column < MaxFormationSlots; column++)
                {
                    var unit = units[GetSlotIndex(row, column)];
                    if (unit != null && !unit.IsDead)
                        aliveUnits.Add(unit);

                    units[GetSlotIndex(row, column)] = null;
                }

                if (playerSide)
                {
                    var startColumn = MaxFormationSlots - aliveUnits.Count;
                    for (var i = 0; i < aliveUnits.Count; i++)
                        units[GetSlotIndex(row, startColumn + i)] = aliveUnits[i];

                    continue;
                }

                for (var i = 0; i < aliveUnits.Count; i++)
                    units[GetSlotIndex(row, i)] = aliveUnits[i];
            }

            var moves = new List<FormationMove>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null)
                    continue;

                if (oldSlots.TryGetValue(unit.RuntimeId, out var fromSlot) && fromSlot != i)
                    moves.Add(new FormationMove(unit.RuntimeId, fromSlot, i));
            }

            return moves;
        }

        private IEnumerator AnimateFormationMoves(List<FormationMove> moves)
        {
            var rects = new List<RectTransform>();
            var startMins = new List<Vector2>();
            var startMaxes = new List<Vector2>();
            var targetMins = new List<Vector2>();
            var targetMaxes = new List<Vector2>();

            foreach (var move in moves)
            {
                if (!unitViews.TryGetValue(move.UnitRuntimeId, out var rect))
                    continue;

                rects.Add(rect);
                startMins.Add(rect.anchorMin);
                startMaxes.Add(rect.anchorMax);
                targetMins.Add(GetUnitAnchorMin(move.ToSlotIndex));
                targetMaxes.Add(GetUnitAnchorMax(move.ToSlotIndex));
            }

            var elapsed = 0f;
            while (elapsed < FormationMoveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / FormationMoveDuration);
                t = t * t * (3f - 2f * t);

                for (var i = 0; i < rects.Count; i++)
                {
                    rects[i].anchorMin = Vector2.Lerp(startMins[i], targetMins[i], t);
                    rects[i].anchorMax = Vector2.Lerp(startMaxes[i], targetMaxes[i], t);
                    rects[i].offsetMin = Vector2.zero;
                    rects[i].offsetMax = Vector2.zero;
                }

                yield return null;
            }
        }

        private void AppendDeathLogs(List<string> logLines)
        {
            foreach (var unit in enemyUnits)
            {
                if (unit != null && unit.IsDead)
                    logLines.Add($"{unit.Name} 阵亡。");
            }

            foreach (var unit in playerUnits)
            {
                if (unit != null && unit.IsDead)
                    logLines.Add($"{unit.Name} 阵亡。");
            }
        }

        private void BuildUi()
        {
            RebuildCardPortraitMap();
            ClearChildren();
            unitViews.Clear();

            var canvas = CreateCanvas();
            EnsureEventSystem();
            CreateBackground(canvas.transform);

            var waves = CurrentWaves;
            var waveCount = waves == null ? 0 : waves.Count;
            var nextWaveLabel = waveCount == 0 ? "0 / 0" : $"{Mathf.Min(nextWaveIndex + 1, waveCount)} / {waveCount}";
            var status = CreateText(canvas.transform, $"第 {currentLevelIndex + 1} 关  |  第 {turnNumber} 回合  |  下一波 {nextWaveLabel}", 22, TextAnchor.MiddleCenter, new Color(0.22f, 0.16f, 0.1f));
            SetRect(status.rectTransform, new Vector2(0.18f, 0.91f), new Vector2(0.82f, 0.97f), Vector2.zero, Vector2.zero);

            var playerPanel = CreatePanel(canvas.transform, "友方阵地", playerPanelColor);
            SetRect(playerPanel, new Vector2(0.02f, 0.31f), new Vector2(0.49f, 0.88f), Vector2.zero, Vector2.zero);
            BuildPlayerBase(playerPanel.transform);
            BuildBoard(playerPanel.transform, true, playerUnits);

            var enemyPanel = CreatePanel(canvas.transform, "敌方阵地", enemyPanelColor);
            SetRect(enemyPanel, new Vector2(0.51f, 0.31f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            BuildBoard(enemyPanel.transform, false, enemyUnits);
            BuildUpcomingWaveHint(enemyPanel.transform);

            var drawPilePanel = CreateInfoBlock(canvas.transform, "抽牌堆", drawPile.Count.ToString(), new Color(0.72f, 0.84f, 0.95f));
            SetRect(drawPilePanel, new Vector2(0.02f, 0.03f), new Vector2(0.1f, 0.28f), Vector2.zero, Vector2.zero);

            var energyPanel = CreateInfoBlock(canvas.transform, "费用", $"{currentEnergy}/{MaxEnergy}", new Color(0.98f, 0.8f, 0.38f));
            SetRect(energyPanel, new Vector2(0.115f, 0.03f), new Vector2(0.19f, 0.28f), Vector2.zero, Vector2.zero);

            var handPanel = CreatePanel(canvas.transform, "手牌", handPanelColor);
            SetRect(handPanel, new Vector2(0.205f, 0.03f), new Vector2(0.635f, 0.28f), Vector2.zero, Vector2.zero);
            BuildHand(handPanel.transform);

            var discardPilePanel = CreateInfoBlock(canvas.transform, "弃牌堆", discardPile.Count.ToString(), new Color(0.78f, 0.72f, 0.88f));
            SetRect(discardPilePanel, new Vector2(0.65f, 0.03f), new Vector2(0.73f, 0.28f), Vector2.zero, Vector2.zero);

            var logPanel = CreatePanel(canvas.transform, "战斗记录", new Color(0.93f, 0.84f, 0.64f));
            SetRect(logPanel, new Vector2(0.75f, 0.12f), new Vector2(0.98f, 0.28f), Vector2.zero, Vector2.zero);
            BuildBattleLog(logPanel.transform);

            if (!battleEnded)
            {
                var endTurnButton = CreateButton(canvas.transform, "结束回合");
                SetRect(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.035f), new Vector2(0.86f, 0.1f), Vector2.zero, Vector2.zero);
                endTurnButton.interactable = !isResolvingTurn;
                endTurnButton.onClick.AddListener(EndTurn);
            }
            else if (HasNextLevel)
            {
                var nextLevelButton = CreateButton(canvas.transform, "下一关");
                SetRect(nextLevelButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.035f), new Vector2(0.86f, 0.1f), Vector2.zero, Vector2.zero);
                nextLevelButton.interactable = !isResolvingTurn;
                nextLevelButton.onClick.AddListener(StartNextLevel);
            }

            var resetButton = CreateButton(canvas.transform, "重开");
            SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(battleEnded ? 0.87f : 0.88f, 0.035f), new Vector2(0.98f, 0.1f), Vector2.zero, Vector2.zero);
            resetButton.interactable = !isResolvingTurn;
            resetButton.onClick.AddListener(ResetBattle);
        }

        private void BuildBattleLog(Transform parent)
        {
            var scrollRoot = CreateImage(parent, "Battle Log Scroll", new Color(1f, 0.96f, 0.78f, 0.35f));
            SetRect(scrollRoot.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateImage(scrollRoot.transform, "Viewport", Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetRect(viewport.rectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero);

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);

            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var text = contentObject.GetComponent<Text>();
            text.text = battleLog;
            text.font = uiFont;
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.18f, 0.12f, 0.08f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = CreateVerticalScrollbar(scrollRoot.transform);
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            var scrollbarRoot = CreateImage(parent, "Scrollbar", new Color(0.32f, 0.22f, 0.14f, 0.25f));
            SetRect(scrollbarRoot.rectTransform, new Vector2(0.92f, 0.04f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbarRoot.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            SetRect(slidingRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handle = CreateImage(slidingArea.transform, "Handle", new Color(0.54f, 0.36f, 0.2f, 0.85f));
            SetRect(handle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            return scrollbar;
        }

        private void BuildBoard(Transform parent, bool playerSide, List<BattleUnit> units)
        {
            for (var row = 0; row < FormationRows; row++)
            {
                for (var column = 0; column < MaxFormationSlots; column++)
                {
                    var slotIndex = GetSlotIndex(row, column);
                    var slot = CreateImage(parent, $"Slot R{row + 1} C{column + 1}", GetSlotColor(row));
                    SetRect(slot.rectTransform, GetSlotAnchorMin(slotIndex), GetSlotAnchorMax(slotIndex), Vector2.zero, Vector2.zero);

                    var outline = slot.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.35f, 0.25f, 0.16f, 0.7f);
                    outline.effectDistance = new Vector2(2f, -2f);

                    var labelText = playerSide ? (MaxFormationSlots - column).ToString() : (column + 1).ToString();
                    var label = CreateText(slot.transform, labelText, 14, TextAnchor.LowerCenter, new Color(0.23f, 0.16f, 0.1f, 0.72f));
                    SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
            }

            for (var i = 0; i < units.Count; i++)
            {
                var battleUnit = units[i];
                if (battleUnit == null)
                    continue;

                var index = i;
                if (animatedSlotOverrides.TryGetValue(battleUnit.RuntimeId, out var overrideSlot))
                    index = overrideSlot;

                var unit = CreateUnitToken(parent, battleUnit, false);
                SetRect(unit, GetUnitAnchorMin(index), GetUnitAnchorMax(index), Vector2.zero, Vector2.zero);
                unit.localScale = Vector3.one * GetDepthScale(index);
                unitViews[battleUnit.RuntimeId] = unit;
            }

            if (playerSide && !isResolvingTurn && CountPlayerUnits() < TotalFormationSlots)
            {
                for (var i = 0; i < TotalFormationSlots; i++)
                {
                    if (units[i] == null)
                        CreatePlayerInsertDropZone(parent, i);
                }

                for (var row = 0; row < FormationRows; row++)
                    CreatePlayerGapDropZonesForRow(parent, row);
            }
        }

        private void BuildPlayerBase(Transform parent)
        {
            var root = CreateImage(parent, "大本营", new Color(0.88f, 0.72f, 0.42f));
            SetRect(root.rectTransform, new Vector2(0.34f, 0.75f), new Vector2(0.66f, 0.86f), Vector2.zero, Vector2.zero);

            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.18f, 0.08f);
            outline.effectDistance = new Vector2(2f, -2f);

            var label = CreateText(root.transform, $"大本营  {playerBaseHp}/{PlayerBaseMaxHp}", 18, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.04f));
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), Vector2.one, Vector2.zero, Vector2.zero);

            CreateHealthBar(root.transform, playerBaseHp, PlayerBaseMaxHp);
        }

        private void CreatePlayerInsertDropZone(Transform parent, int insertIndex)
        {
            var zone = CreateImage(parent, $"Insert {insertIndex}", new Color(0.2f, 0.7f, 0.95f, 0.08f));
            SetRect(zone.rectTransform, GetSlotAnchorMin(insertIndex), GetSlotAnchorMax(insertIndex), Vector2.zero, Vector2.zero);

            var outline = zone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.45f, 0.85f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            var dropZone = zone.gameObject.AddComponent<BoardInsertDropZone>();
            dropZone.Initialize(this, insertIndex, false, zone);
        }

        private void CreatePlayerGapDropZonesForRow(Transform parent, int row)
        {
            var occupiedSlots = GetOccupiedPlayerSlotsInRow(row);
            if (occupiedSlots.Count == 0 || occupiedSlots.Count >= MaxFormationSlots)
                return;

            for (var i = 0; i < occupiedSlots.Count - 1; i++)
            {
                var leftColumn = GetSlotColumn(occupiedSlots[i]);
                CreatePlayerGapDropZone(parent, row, leftColumn, occupiedSlots);
            }

            var rightmostColumn = GetSlotColumn(occupiedSlots[occupiedSlots.Count - 1]);
            CreatePlayerGapDropZone(parent, row, rightmostColumn, occupiedSlots);
        }

        private void CreatePlayerGapDropZone(Transform parent, int row, int afterColumn, List<int> occupiedSlots)
        {
            var gapIndex = EncodeGapIndex(row, afterColumn);
            var anchorMin = GetGapZoneAnchorMin(gapIndex, occupiedSlots);
            var anchorMax = GetGapZoneAnchorMax(gapIndex, occupiedSlots);
            var zone = CreateImage(parent, $"Gap R{row + 1} C{afterColumn + 1}", Color.clear);
            SetRect(zone.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            zone.transform.SetAsLastSibling();

            var marker = CreateImage(zone.transform, "Insert Marker", new Color(0.02f, 0.42f, 0.18f, 0.95f));
            SetRect(marker.rectTransform, new Vector2(0.46f, 0.08f), new Vector2(0.54f, 0.92f), Vector2.zero, Vector2.zero);
            marker.raycastTarget = false;
            marker.enabled = false;

            var dropZone = zone.gameObject.AddComponent<BoardInsertDropZone>();
            dropZone.Initialize(this, gapIndex, true, zone, marker);
        }

        private static Vector2 GetGapZoneAnchorMin(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var center = GetGapZoneCenter(gapIndex, occupiedSlots);
            const float width = 0.055f;
            return new Vector2(Mathf.Clamp(center.x - width * 0.5f, 0.02f, 0.94f), GetSlotAnchorMin(GetSlotIndex(row, 0)).y);
        }

        private static Vector2 GetGapZoneAnchorMax(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var center = GetGapZoneCenter(gapIndex, occupiedSlots);
            const float width = 0.055f;
            return new Vector2(Mathf.Clamp(center.x + width * 0.5f, 0.06f, 0.98f), GetSlotAnchorMax(GetSlotIndex(row, 0)).y);
        }

        private static Vector2 GetGapZoneCenter(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var afterColumn = DecodeGapAfterColumn(gapIndex);
            var afterSlot = GetSlotIndex(row, afterColumn);
            var afterMax = GetSlotAnchorMax(afterSlot);
            var nextSlot = -1;
            foreach (var slot in occupiedSlots)
            {
                if (GetSlotColumn(slot) > afterColumn)
                {
                    nextSlot = slot;
                    break;
                }
            }

            if (nextSlot >= 0)
                return new Vector2((afterMax.x + GetSlotAnchorMin(nextSlot).x) * 0.5f, GetSlotCenter(afterSlot).y);

            return new Vector2(afterMax.x + 0.025f, GetSlotCenter(afterSlot).y);
        }

        private static int EncodeGapIndex(int row, int afterColumn)
        {
            return row * MaxFormationSlots + afterColumn;
        }

        private static int DecodeGapRow(int gapIndex)
        {
            return Mathf.Clamp(gapIndex / MaxFormationSlots, 0, FormationRows - 1);
        }

        private static int DecodeGapAfterColumn(int gapIndex)
        {
            return Mathf.Clamp(gapIndex % MaxFormationSlots, 0, MaxFormationSlots - 1);
        }

        private static int GetSlotIndex(int row, int column)
        {
            return row * MaxFormationSlots + column;
        }

        private static int GetSlotRow(int slotIndex)
        {
            return Mathf.Clamp(slotIndex / MaxFormationSlots, 0, FormationRows - 1);
        }

        private static int GetSlotColumn(int slotIndex)
        {
            return Mathf.Clamp(slotIndex % MaxFormationSlots, 0, MaxFormationSlots - 1);
        }

        private static Color GetSlotColor(int row)
        {
            var alpha = 0.34f + row * 0.08f;
            return new Color(1f, 1f, 1f, alpha);
        }

        private static Vector2 GetSlotCenter(int slotIndex)
        {
            var row = GetSlotRow(slotIndex);
            var column = GetSlotColumn(slotIndex);
            var rowOffset = row - 1;
            return new Vector2(0.16f + column * 0.165f + rowOffset * 0.055f, 0.60f - row * 0.20f);
        }

        private static Vector2 GetSlotAnchorMin(int slotIndex)
        {
            var center = GetSlotCenter(slotIndex);
            const float width = 0.074f;
            const float height = 0.068f;
            return new Vector2(center.x - width, center.y - height);
        }

        private static Vector2 GetSlotAnchorMax(int slotIndex)
        {
            var center = GetSlotCenter(slotIndex);
            const float width = 0.074f;
            const float height = 0.068f;
            return new Vector2(center.x + width, center.y + height);
        }

        private static Vector2 GetUnitAnchorMin(int slotIndex)
        {
            var center = GetSlotCenter(slotIndex);
            const float width = 0.058f;
            const float height = 0.15f;
            return new Vector2(center.x - width, center.y - height * 0.55f);
        }

        private static Vector2 GetUnitAnchorMax(int slotIndex)
        {
            var center = GetSlotCenter(slotIndex);
            const float width = 0.058f;
            const float height = 0.15f;
            return new Vector2(center.x + width, center.y + height);
        }

        private static float GetDepthScale(int slotIndex)
        {
            return 1f;
        }

        private void BuildUpcomingWaveHint(Transform parent)
        {
            var waves = CurrentWaves;
            var hint = waves != null && nextWaveIndex < waves.Count ? $"下波：{DescribeWave(waves[nextWaveIndex])}" : "已无后续波次";
            var text = CreateText(parent, hint, 20, TextAnchor.MiddleCenter, new Color(0.22f, 0.12f, 0.1f));
            SetRect(text.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
        }

        private string DescribeWave(WaveSpawnRecord wave)
        {
            var names = new List<string>();
            for (var row = 0; row < Mathf.Min(FormationRows, wave.RowCardIds.Length); row++)
            {
                foreach (var cardId in wave.RowCardIds[row])
                {
                    if (cardMap.TryGetValue(cardId, out var card))
                        names.Add($"{card.CardName}(第 {row + 1} 排)");
                }
            }

            return names.Count == 0 ? "-" : string.Join("、", names);
        }

        private void BuildHand(Transform parent)
        {
            for (var i = 0; i < Mathf.Min(hand.Count, 5); i++)
            {
                var card = hand[i];
                var cardView = CreateCard(parent, card);
                SetRect(cardView.GetComponent<RectTransform>(), new Vector2(0.025f + i * 0.19f, 0.12f), new Vector2(0.18f + i * 0.19f, 0.78f), Vector2.zero, Vector2.zero);
            }
        }

        private void RebuildCardPortraitMap()
        {
            if (cardPortraits == null)
            {
                cardPortraitMap = new Dictionary<string, Sprite>();
                return;
            }

            cardPortraitMap = cardPortraits
                .Where(entry => entry != null
                    && !string.IsNullOrWhiteSpace(entry.cardId)
                    && entry.portrait != null)
                .GroupBy(entry => entry.cardId)
                .ToDictionary(group => group.Key, group => group.First().portrait);
        }

        private bool TryGetCardPortrait(CardRecord card, out Sprite portrait)
        {
            if (cardPortraitMap != null && cardPortraitMap.TryGetValue(card.CardId, out portrait))
                return true;

#if UNITY_EDITOR
            portrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_Project/Art/Cards/Portraits/{(string.IsNullOrWhiteSpace(card.ArtId) ? card.CardId : card.ArtId)}.png");
            return portrait != null;
#else
            portrait = null;
            return false;
#endif
        }

        private bool TryGetUnitSprite(BattleUnit unit, out Sprite sprite)
        {
#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_Project/Art/Units/Sprites/{unit.UnitId}.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }

        private Button CreateCard(Transform parent, CardRecord card)
        {
            var color = card.CardType == CardType.Unit && card.UnitType == UnitType.Hero ? heroCardColor : soldierCardColor;
            var image = CreateImage(parent, card.CardName, color);

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => PlayCard(card));
            button.interactable = !isResolvingTurn && currentEnergy >= card.Cost;
            var canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            var dragHandler = image.gameObject.AddComponent<CardDragHandler>();
            dragHandler.Initialize(this, card, image.rectTransform, canvasGroup);

            var cost = CreateBadge(image.transform, card.Cost.ToString(), new Color(0.25f, 0.6f, 0.95f));
            SetRect(cost, new Vector2(0.03f, 0.72f), new Vector2(0.25f, 0.96f), Vector2.zero, Vector2.zero);

            var name = CreateText(image.transform, card.CardName, 22, TextAnchor.MiddleCenter, new Color(0.15f, 0.09f, 0.05f));
            SetRect(name.rectTransform, new Vector2(0.2f, 0.72f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            var portrait = CreateImage(image.transform, "Portrait", new Color(1f, 0.96f, 0.78f));
            SetRect(portrait.rectTransform, new Vector2(0.14f, 0.39f), new Vector2(0.86f, 0.69f), Vector2.zero, Vector2.zero);

            if (TryGetCardPortrait(card, out var portraitSprite))
            {
                portrait.color = Color.white;
                portrait.sprite = portraitSprite;
                portrait.preserveAspect = true;
            }
            else
            {
                var face = CreateText(
                    portrait.transform,
                    string.IsNullOrEmpty(card.CardName) ? "?" : card.CardName.Substring(0, 1),
                    34,
                    TextAnchor.MiddleCenter,
                    Color.white);
                SetRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var statsText = card.CardType == CardType.Unit ? $"攻 {card.Attack}   血 {card.Hp}" : card.CardType.ToString();
            var stats = CreateText(image.transform, statsText, 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.08f));
            SetRect(stats.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.25f), Vector2.zero, Vector2.zero);

            if (card.Effects.Count > 0)
            {
                var effect = CreateText(image.transform, card.Effects[0].EffectName, 14, TextAnchor.MiddleCenter, new Color(0.22f, 0.12f, 0.08f));
                SetRect(effect.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
            }

            var typeText = card.CardType == CardType.Unit && card.UnitType == UnitType.Hero ? "唯一" : "普通";
            var type = CreateText(image.transform, typeText, 16, TextAnchor.MiddleCenter, Color.white);
            SetRect(type.rectTransform, new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);

            CreateBorder(image.transform, new Color(0.22f, 0.16f, 0.1f), 3f);
            return button;
        }

        private RectTransform CreateUnitToken(Transform parent, BattleUnit unit, bool compact)
        {
            var hasSprite = TryGetUnitSprite(unit, out var unitSprite);
            var root = CreateImage(parent, unit.Name, hasSprite ? new Color(1f, 1f, 1f, 0f) : unit.Camp == CardCamp.Enemy ? enemyCardColor : heroCardColor);
            if (!hasSprite)
                root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.45f);
            shadow.effectDistance = new Vector2(6f, -8f);

            if (hasSprite)
            {
                var spriteImage = CreateImage(root.transform, "Unit Sprite", Color.white);
                spriteImage.sprite = unitSprite;
                spriteImage.preserveAspect = true;
                SetRect(spriteImage.rectTransform, new Vector2(-0.12f, 0.08f), new Vector2(1.12f, 1.14f), Vector2.zero, Vector2.zero);
                spriteImage.raycastTarget = false;
            }

            var name = CreateText(root.transform, unit.Name, compact ? 15 : 16, TextAnchor.MiddleCenter, hasSprite ? Color.white : new Color(0.12f, 0.08f, 0.05f));
            SetRect(name.rectTransform, hasSprite ? new Vector2(-0.08f, -0.08f) : new Vector2(0f, 0.56f), hasSprite ? new Vector2(1.08f, 0.12f) : Vector2.one, Vector2.zero, Vector2.zero);
            if (hasSprite)
                name.gameObject.AddComponent<Outline>().effectColor = new Color(0.12f, 0.06f, 0.04f, 0.9f);

            var attack = CreateText(root.transform, $"攻 {unit.Attack}", compact ? 14 : 15, TextAnchor.MiddleCenter, hasSprite ? Color.white : new Color(0.18f, 0.1f, 0.06f));
            SetRect(attack.rectTransform, hasSprite ? new Vector2(-0.08f, 0.1f) : new Vector2(0f, 0.32f), hasSprite ? new Vector2(1.08f, 0.28f) : new Vector2(1f, 0.52f), Vector2.zero, Vector2.zero);
            if (hasSprite)
                attack.gameObject.AddComponent<Outline>().effectColor = new Color(0.12f, 0.06f, 0.04f, 0.9f);

            CreateHealthBar(root.transform, unit.CurrentHp, unit.MaxHp);

            return root.rectTransform;
        }

        private void CreateHealthBar(Transform parent, int currentHp, int maxHp)
        {
            var frame = CreateImage(parent, "Health Bar", new Color(0.22f, 0.04f, 0.035f));
            SetRect(frame.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);
            frame.gameObject.AddComponent<Outline>().effectColor = new Color(0.08f, 0.02f, 0.015f);

            var fill = CreateImage(frame.transform, "Health Fill", new Color(0.83f, 0.08f, 0.06f));
            var hpRatio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);
            SetRect(fill.rectTransform, Vector2.zero, new Vector2(hpRatio, 1f), Vector2.zero, Vector2.zero);

            var text = CreateText(frame.transform, $"{currentHp}/{maxHp}", 15, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

    }
}
