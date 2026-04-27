using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed class PrototypeBattleUi : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TextAsset cardsCsv;
        [SerializeField] private TextAsset startingDeckCsv;
        [SerializeField] private TextAsset wavesCsv;

        [Header("Style")]
        [SerializeField] private Font uiFont;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1600f, 900f);

        private readonly Color backgroundColor = new Color(0.94f, 0.89f, 0.73f);
        private readonly Color playerPanelColor = new Color(0.76f, 0.92f, 0.67f);
        private readonly Color enemyPanelColor = new Color(0.96f, 0.68f, 0.58f);
        private readonly Color handPanelColor = new Color(0.99f, 0.94f, 0.72f);
        private readonly Color heroCardColor = new Color(1f, 0.82f, 0.36f);
        private readonly Color soldierCardColor = new Color(0.66f, 0.88f, 1f);
        private readonly Color enemyCardColor = new Color(1f, 0.56f, 0.5f);
        private Sprite roundedButtonSprite;
        private const int MaxFormationSlots = 5;
        private const int MaxEnergy = 3;
        private const int PlayerBaseMaxHp = 10;
        private const float FormationMoveDuration = 0.45f;

        private Dictionary<string, CardRecord> cardMap = new Dictionary<string, CardRecord>();
        private readonly List<CardRecord> drawPile = new List<CardRecord>();
        private readonly List<CardRecord> discardPile = new List<CardRecord>();
        private readonly List<CardRecord> hand = new List<CardRecord>();
        private readonly List<BattleUnit> playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit> enemyUnits = new List<BattleUnit>();
        private readonly Dictionary<int, RectTransform> unitViews = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, int> animatedSlotOverrides = new Dictionary<int, int>();
        private List<List<string>> waveSlots = new List<List<string>>();

        private int turnNumber = 1;
        private int nextWaveIndex;
        private int nextUnitRuntimeId = 1;
        private int currentEnergy = MaxEnergy;
        private int playerBaseHp = PlayerBaseMaxHp;
        private string battleLog = "拖动手牌到友方阵地上阵，然后点击结束回合。";
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

        private void Awake()
        {
            ResetBattle();
        }

        private void InitializeBattle()
        {
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cards = PrototypeCsvDatabase.LoadCards(cardsCsv);
            cardMap = cards.ToDictionary(card => card.CardId);
            waveSlots = PrototypeCsvDatabase.LoadWaveSlots(wavesCsv);

            drawPile.Clear();
            discardPile.Clear();
            hand.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            for (var i = 0; i < MaxFormationSlots; i++)
                playerUnits.Add(null);

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
            DrawCards(5);
            battleLog = "准备阶段：抽牌堆已洗牌，抽 5 张牌进入手牌。";
            initialized = true;
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

        private void PlayCardAt(CardRecord card, int insertIndex)
        {
            if (card == null || card.Camp != CardCamp.Player)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= MaxFormationSlots)
            {
                battleLog = "友方阵地已满，无法继续上阵。";
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                battleLog = $"费用不足：{card.CardName} 需要 {card.Cost} 点费用。";
                BuildUi();
                return;
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, MaxFormationSlots - 1);
            if (playerUnits[insertIndex] != null)
            {
                battleLog = "该阵地位置已有单位。";
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            currentEnergy -= card.Cost;
            playerUnits[insertIndex] = new BattleUnit(card, nextUnitRuntimeId++);
            battleLog = $"{card.CardName} 上阵，消耗 {card.Cost} 点费用。";
            BuildUi();
        }

        private void PlayCardInGap(CardRecord card, int gapIndex)
        {
            if (card == null || card.Camp != CardCamp.Player)
                return;

            if (isResolvingTurn)
                return;

            if (CountPlayerUnits() >= MaxFormationSlots)
            {
                battleLog = "友方阵地已满，无法继续上阵。";
                BuildUi();
                return;
            }

            if (currentEnergy < card.Cost)
            {
                battleLog = $"费用不足：{card.CardName} 需要 {card.Cost} 点费用。";
                BuildUi();
                return;
            }

            if (!hand.Remove(card))
                return;

            var unit = new BattleUnit(card, nextUnitRuntimeId++);
            if (!TryInsertPlayerUnitAtGap(unit, gapIndex))
            {
                hand.Add(card);
                battleLog = "当前插入位置不可用。";
                BuildUi();
                return;
            }

            currentEnergy -= card.Cost;
            battleLog = $"{card.CardName} 插入阵地，消耗 {card.Cost} 点费用。";
            BuildUi();
        }

        private bool TryInsertPlayerUnitAtGap(BattleUnit unit, int gapIndex)
        {
            var occupiedSlots = GetOccupiedPlayerSlots();
            if (occupiedSlots.Count >= MaxFormationSlots)
                return false;

            if (occupiedSlots.Count == 0)
            {
                playerUnits[GetFirstEmptyPlayerSlot()] = unit;
                return true;
            }

            gapIndex = Mathf.Clamp(gapIndex, 0, occupiedSlots.Count);
            if (gapIndex == 0)
                return InsertBeforeSlot(unit, occupiedSlots[0]);

            if (gapIndex == occupiedSlots.Count)
                return InsertAfterSlot(unit, occupiedSlots[occupiedSlots.Count - 1]);

            return InsertBetweenSlots(unit, occupiedSlots[gapIndex - 1], occupiedSlots[gapIndex]);
        }

        private bool InsertBeforeSlot(BattleUnit unit, int slot)
        {
            for (var i = slot - 1; i >= 0; i--)
            {
                if (playerUnits[i] == null)
                {
                    playerUnits[i] = unit;
                    return true;
                }
            }

            var emptyRight = FindEmptyRight(slot);
            if (emptyRight < 0)
                return false;

            ShiftRight(slot, emptyRight);
            playerUnits[slot] = unit;
            return true;
        }

        private bool InsertAfterSlot(BattleUnit unit, int slot)
        {
            for (var i = slot + 1; i < MaxFormationSlots; i++)
            {
                if (playerUnits[i] == null)
                {
                    playerUnits[i] = unit;
                    return true;
                }
            }

            var emptyLeft = FindEmptyLeft(slot);
            if (emptyLeft < 0)
                return false;

            ShiftLeft(emptyLeft, slot);
            playerUnits[slot] = unit;
            return true;
        }

        private bool InsertBetweenSlots(BattleUnit unit, int leftSlot, int rightSlot)
        {
            for (var i = leftSlot + 1; i < rightSlot; i++)
            {
                if (playerUnits[i] == null)
                {
                    playerUnits[i] = unit;
                    return true;
                }
            }

            var emptyLeft = FindEmptyLeft(leftSlot);
            var emptyRight = FindEmptyRight(rightSlot);

            if (emptyRight >= 0 && (emptyLeft < 0 || emptyRight - rightSlot <= leftSlot - emptyLeft))
            {
                ShiftRight(rightSlot, emptyRight);
                playerUnits[rightSlot] = unit;
                return true;
            }

            if (emptyLeft >= 0)
            {
                ShiftLeft(emptyLeft, leftSlot);
                playerUnits[leftSlot] = unit;
                return true;
            }

            return false;
        }

        private int FindEmptyLeft(int fromSlot)
        {
            for (var i = fromSlot - 1; i >= 0; i--)
            {
                if (playerUnits[i] == null)
                    return i;
            }

            return -1;
        }

        private int FindEmptyRight(int fromSlot)
        {
            for (var i = fromSlot + 1; i < MaxFormationSlots; i++)
            {
                if (playerUnits[i] == null)
                    return i;
            }

            return -1;
        }

        private void ShiftRight(int fromSlot, int emptySlot)
        {
            for (var i = emptySlot; i > fromSlot; i--)
                playerUnits[i] = playerUnits[i - 1];
        }

        private void ShiftLeft(int emptySlot, int toSlot)
        {
            for (var i = emptySlot; i < toSlot; i++)
                playerUnits[i] = playerUnits[i + 1];
        }

        private List<int> GetOccupiedPlayerSlots()
        {
            var slots = new List<int>();
            for (var i = 0; i < MaxFormationSlots; i++)
            {
                if (playerUnits[i] != null && !playerUnits[i].IsDead)
                    slots.Add(i);
            }

            return slots;
        }

        private int GetFirstEmptyPlayerSlot()
        {
            for (var i = MaxFormationSlots - 1; i >= 0; i--)
            {
                if (playerUnits[i] == null)
                    return i;
            }

            return MaxFormationSlots - 1;
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

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            SpawnCurrentWave(logLines);
            battleLog = string.Join("\n", logLines);
            BuildUi();
            yield return new WaitForSeconds(0.75f);

            ResolveEnemyAttack(logLines);
            battleLog = string.Join("\n", logLines);
            BuildUi();
            if (playerBaseHp <= 0)
            {
                battleLog = string.Join("\n", logLines) + "\n大本营被攻破，战斗失败。";
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            yield return new WaitForSeconds(0.75f);

            ResolvePlayerAttack(logLines);
            AppendDeathLogs(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            if (enemyUnits.Count == 0 && nextWaveIndex >= waveSlots.Count)
            {
                battleLog = string.Join("\n", logLines) + "\n战斗胜利！";
                battleEnded = true;
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            turnNumber++;
            currentEnergy = MaxEnergy;
            var drawCount = DrawCardsWithCount(5);
            logLines.Add($"进入第 {turnNumber} 回合，费用恢复到 {MaxEnergy}，从抽牌堆抽 {drawCount} 张牌。");
            battleLog = string.Join("\n", logLines);
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

        private void DiscardHand(List<string> logLines)
        {
            if (hand.Count == 0)
                return;

            var count = hand.Count;
            discardPile.AddRange(hand);
            hand.Clear();
            logLines.Add($"未使用的 {count} 张手牌进入弃牌堆。");
        }

        private void SpawnCurrentWave(List<string> logLines)
        {
            if (nextWaveIndex >= waveSlots.Count)
                return;

            var wave = waveSlots[nextWaveIndex];
            nextWaveIndex++;

            var spawnedNames = new List<string>();
            foreach (var cardId in wave)
            {
                if (enemyUnits.Count >= MaxFormationSlots)
                    break;

                if (!cardMap.TryGetValue(cardId, out var card))
                    continue;

                enemyUnits.Add(new BattleUnit(card, nextUnitRuntimeId++));
                spawnedNames.Add(card.CardName);
            }

            if (spawnedNames.Count > 0)
                logLines.Add($"第 {nextWaveIndex} 波出现：{string.Join("、", spawnedNames)}。");
        }

        private void ResolveEnemyAttack(List<string> logLines)
        {
            foreach (var attacker in enemyUnits.ToList())
            {
                if (attacker.IsDead)
                    continue;

                var target = GetPlayerFrontUnit();
                if (target == null)
                {
                    playerBaseHp = Mathf.Max(0, playerBaseHp - attacker.Attack);
                    logLines.Add($"{attacker.Name} 攻击大本营，造成 {attacker.Attack} 点伤害。");
                    if (playerBaseHp <= 0)
                        return;

                    continue;
                }

                target.TakeDamage(attacker.Attack);
                logLines.Add($"{attacker.Name} 攻击 {target.Name}，造成 {attacker.Attack} 点伤害。");
            }
        }

        private void ResolvePlayerAttack(List<string> logLines)
        {
            for (var i = playerUnits.Count - 1; i >= 0; i--)
            {
                var attacker = playerUnits[i];
                if (attacker == null || attacker.IsDead)
                    continue;

                var target = GetEnemyFrontUnit();
                if (target == null)
                    return;

                target.TakeDamage(attacker.Attack);
                logLines.Add($"{attacker.Name} 反击 {target.Name}，造成 {attacker.Attack} 点伤害。");
            }
        }

        private BattleUnit GetPlayerFrontUnit()
        {
            for (var i = playerUnits.Count - 1; i >= 0; i--)
            {
                if (playerUnits[i] != null && !playerUnits[i].IsDead)
                    return playerUnits[i];
            }

            return null;
        }

        private BattleUnit GetEnemyFrontUnit()
        {
            for (var i = 0; i < enemyUnits.Count; i++)
            {
                if (!enemyUnits[i].IsDead)
                    return enemyUnits[i];
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
            battleLog = string.Join("\n", logLines);

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
                    oldSlots[unit.RuntimeId] = GetVisualSlotIndex(playerSide, i, units.Count);
            }

            if (playerSide)
            {
                var aliveUnits = units.Where(unit => unit != null && !unit.IsDead).ToList();
                units.Clear();
                for (var i = 0; i < MaxFormationSlots - aliveUnits.Count; i++)
                    units.Add(null);

                units.AddRange(aliveUnits);
            }
            else
            {
                units.RemoveAll(unit => unit == null || unit.IsDead);
            }

            var moves = new List<FormationMove>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null)
                    continue;

                var toSlot = GetVisualSlotIndex(playerSide, i, units.Count);
                if (oldSlots.TryGetValue(unit.RuntimeId, out var fromSlot) && fromSlot != toSlot)
                    moves.Add(new FormationMove(unit.RuntimeId, fromSlot, toSlot));
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
                if (unit.IsDead)
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
            ClearChildren();
            unitViews.Clear();

            var canvas = CreateCanvas();
            EnsureEventSystem();
            CreateBackground(canvas.transform);

            var status = CreateText(canvas.transform, $"第 {turnNumber} 回合  |  下一波 {Mathf.Min(nextWaveIndex + 1, waveSlots.Count)} / {waveSlots.Count}", 22, TextAnchor.MiddleCenter, new Color(0.22f, 0.16f, 0.1f));
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
            var logText = CreateText(logPanel.transform, battleLog, 18, TextAnchor.UpperLeft, new Color(0.18f, 0.12f, 0.08f));
            SetRect(logText.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.7f), Vector2.zero, Vector2.zero);

            if (!battleEnded)
            {
                var endTurnButton = CreateButton(canvas.transform, "结束回合");
                SetRect(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.75f, 0.035f), new Vector2(0.86f, 0.1f), Vector2.zero, Vector2.zero);
                endTurnButton.interactable = !isResolvingTurn;
                endTurnButton.onClick.AddListener(EndTurn);
            }

            var resetButton = CreateButton(canvas.transform, "重开");
            SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(battleEnded ? 0.87f : 0.88f, 0.035f), new Vector2(0.98f, 0.1f), Vector2.zero, Vector2.zero);
            resetButton.interactable = !isResolvingTurn;
            resetButton.onClick.AddListener(ResetBattle);
        }

        private void BuildBoard(Transform parent, bool playerSide, List<BattleUnit> units)
        {
            for (var i = 0; i < MaxFormationSlots; i++)
            {
                var slot = CreateImage(parent, $"Slot {i + 1}", new Color(1f, 1f, 1f, 0.38f));
                SetRect(slot.rectTransform, GetSlotAnchorMin(i), GetSlotAnchorMax(i), Vector2.zero, Vector2.zero);
                slot.gameObject.AddComponent<Outline>().effectColor = new Color(0.35f, 0.25f, 0.16f);

                var order = playerSide ? MaxFormationSlots - i : i + 1;
                var label = CreateText(slot.transform, order.ToString(), 18, TextAnchor.LowerCenter, new Color(0.23f, 0.16f, 0.1f));
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            for (var i = 0; i < units.Count; i++)
            {
                var index = GetVisualSlotIndex(playerSide, i, units.Count);
                var battleUnit = units[i];
                if (battleUnit == null)
                    continue;

                if (animatedSlotOverrides.TryGetValue(battleUnit.RuntimeId, out var overrideSlot))
                    index = overrideSlot;

                var unit = CreateUnitToken(parent, battleUnit, false);
                SetRect(unit, GetUnitAnchorMin(index), GetUnitAnchorMax(index), Vector2.zero, Vector2.zero);
                unitViews[battleUnit.RuntimeId] = unit;
            }

            if (playerSide && !isResolvingTurn && CountPlayerUnits() < MaxFormationSlots)
            {
                for (var i = 0; i < MaxFormationSlots; i++)
                {
                    if (units[i] == null)
                        CreatePlayerInsertDropZone(parent, i);
                }

                var occupiedSlots = GetOccupiedPlayerSlots();
                for (var i = 0; i <= occupiedSlots.Count; i++)
                    CreatePlayerGapDropZone(parent, i, occupiedSlots);
            }
        }

        private void BuildPlayerBase(Transform parent)
        {
            var root = CreateImage(parent, "大本营", new Color(0.88f, 0.72f, 0.42f));
            SetRect(root.rectTransform, new Vector2(0.34f, 0.73f), new Vector2(0.66f, 0.84f), Vector2.zero, Vector2.zero);

            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.18f, 0.08f);
            outline.effectDistance = new Vector2(2f, -2f);

            var label = CreateText(root.transform, "大本营", 18, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.04f));
            SetRect(label.rectTransform, new Vector2(0f, 0.48f), Vector2.one, Vector2.zero, Vector2.zero);

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

        private void CreatePlayerGapDropZone(Transform parent, int gapIndex, List<int> occupiedSlots)
        {
            if (occupiedSlots.Count == 0)
                return;

            var anchorMin = GetGapZoneAnchorMin(gapIndex, occupiedSlots);
            var anchorMax = GetGapZoneAnchorMax(gapIndex, occupiedSlots);
            var zone = CreateImage(parent, $"Gap {gapIndex}", Color.clear);
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
            var center = GetGapZoneCenterX(gapIndex, occupiedSlots);
            const float width = 0.07f;
            return new Vector2(Mathf.Clamp(center - width * 0.5f, 0.02f, 0.94f), 0.18f);
        }

        private static Vector2 GetGapZoneAnchorMax(int gapIndex, List<int> occupiedSlots)
        {
            var center = GetGapZoneCenterX(gapIndex, occupiedSlots);
            const float width = 0.07f;
            return new Vector2(Mathf.Clamp(center + width * 0.5f, 0.06f, 0.98f), 0.72f);
        }

        private static float GetGapZoneCenterX(int gapIndex, List<int> occupiedSlots)
        {
            if (gapIndex <= 0)
                return GetSlotAnchorMin(occupiedSlots[0]).x;

            if (gapIndex >= occupiedSlots.Count)
                return GetSlotAnchorMax(occupiedSlots[occupiedSlots.Count - 1]).x;

            var left = occupiedSlots[gapIndex - 1];
            var right = occupiedSlots[gapIndex];
            return (GetSlotAnchorMax(left).x + GetSlotAnchorMin(right).x) * 0.5f;
        }

        private static int GetVisualSlotIndex(bool playerSide, int unitIndex, int unitCount)
        {
            return playerSide ? MaxFormationSlots - unitCount + unitIndex : unitIndex;
        }

        private static Vector2 GetSlotAnchorMin(int slotIndex)
        {
            return new Vector2(0.05f + slotIndex * 0.18f, 0.18f);
        }

        private static Vector2 GetSlotAnchorMax(int slotIndex)
        {
            return new Vector2(0.19f + slotIndex * 0.18f, 0.72f);
        }

        private static Vector2 GetUnitAnchorMin(int slotIndex)
        {
            return new Vector2(0.065f + slotIndex * 0.18f, 0.3f);
        }

        private static Vector2 GetUnitAnchorMax(int slotIndex)
        {
            return new Vector2(0.175f + slotIndex * 0.18f, 0.65f);
        }

        private void BuildUpcomingWaveHint(Transform parent)
        {
            var hint = nextWaveIndex < waveSlots.Count ? $"下波：{DescribeWave(waveSlots[nextWaveIndex])}" : "已无后续波次";
            var text = CreateText(parent, hint, 20, TextAnchor.MiddleCenter, new Color(0.22f, 0.12f, 0.1f));
            SetRect(text.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
        }

        private string DescribeWave(List<string> cardIds)
        {
            var names = new List<string>();
            foreach (var cardId in cardIds)
            {
                if (cardMap.TryGetValue(cardId, out var card))
                    names.Add(card.CardName);
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

        private Button CreateCard(Transform parent, CardRecord card)
        {
            var color = card.UnitType == UnitType.Hero ? heroCardColor : soldierCardColor;
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
            SetRect(portrait.rectTransform, new Vector2(0.14f, 0.34f), new Vector2(0.86f, 0.69f), Vector2.zero, Vector2.zero);

            var face = CreateText(portrait.transform, string.IsNullOrEmpty(card.CardName) ? "?" : card.CardName.Substring(0, 1), 34, TextAnchor.MiddleCenter, Color.white);
            SetRect(face.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var stats = CreateText(image.transform, $"攻 {card.Attack}   血 {card.Hp}", 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.08f));
            SetRect(stats.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.31f), Vector2.zero, Vector2.zero);

            var typeText = card.UnitType == UnitType.Hero ? "唯一" : "普通";
            var type = CreateText(image.transform, typeText, 16, TextAnchor.MiddleCenter, Color.white);
            SetRect(type.rectTransform, new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);

            CreateBorder(image.transform, new Color(0.22f, 0.16f, 0.1f), 3f);
            return button;
        }

        private void CreateBorder(Transform parent, Color color, float thickness)
        {
            CreateBorderPart(parent, "Border Top", color, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -thickness), Vector2.zero);
            CreateBorderPart(parent, "Border Bottom", color, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            CreateBorderPart(parent, "Border Left", color, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            CreateBorderPart(parent, "Border Right", color, new Vector2(1f, 0f), Vector2.one, new Vector2(-thickness, 0f), Vector2.zero);
        }

        private void CreateBorderPart(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var border = CreateImage(parent, name, color);
            border.raycastTarget = false;
            border.rectTransform.anchorMin = anchorMin;
            border.rectTransform.anchorMax = anchorMax;
            border.rectTransform.offsetMin = offsetMin;
            border.rectTransform.offsetMax = offsetMax;
            border.transform.SetAsLastSibling();
        }

        private RectTransform CreateUnitToken(Transform parent, BattleUnit unit, bool compact)
        {
            var root = CreateImage(parent, unit.Name, unit.Camp == CardCamp.Enemy ? enemyCardColor : heroCardColor);
            root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var name = CreateText(root.transform, unit.Name, compact ? 17 : 18, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.05f));
            SetRect(name.rectTransform, new Vector2(0f, 0.56f), Vector2.one, Vector2.zero, Vector2.zero);

            var attack = CreateText(root.transform, $"攻 {unit.Attack}", compact ? 15 : 16, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.06f));
            SetRect(attack.rectTransform, new Vector2(0f, 0.32f), new Vector2(1f, 0.52f), Vector2.zero, Vector2.zero);

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

        private RectTransform CreateBadge(Transform parent, string value, Color color)
        {
            var badge = CreateImage(parent, "Badge", color);
            badge.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.1f, 0.07f);

            var text = CreateText(badge.transform, value, 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return badge.rectTransform;
        }

        private RectTransform CreateInfoBlock(Transform parent, string title, string value, Color color)
        {
            var block = CreatePanel(parent, title, color);

            var valueText = CreateText(block.transform, value, 34, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.05f));
            SetRect(valueText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.68f), Vector2.zero, Vector2.zero);

            return block;
        }

        private Button CreateButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.84f, 0.24f, 0.18f));
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.84f, 0.24f, 0.18f),
                highlightedColor = new Color(0.96f, 0.36f, 0.26f),
                pressedColor = new Color(0.64f, 0.15f, 0.12f),
                selectedColor = new Color(0.84f, 0.24f, 0.18f),
                disabledColor = new Color(0.48f, 0.44f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.16f, 0.08f, 0.05f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);

            var text = CreateText(image.transform, label, 21, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }

        private Sprite GetRoundedButtonSprite()
        {
            if (roundedButtonSprite != null)
                return roundedButtonSprite;

            const int size = 32;
            const int radius = 9;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0;
                    var dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0;
                    var inside = dx * dx + dy * dy <= radius * radius;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedButtonSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedButtonSprite;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            var image = gameObject.GetComponent<Image>();
            image.color = color;

            return image;
        }

        private Text CreateText(Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);

            var text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 8);
            text.resizeTextMaxSize = size;

            return text;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Prototype Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        private void CreateBackground(Transform parent)
        {
            var background = CreateImage(parent, "Background", backgroundColor);
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var skyBand = CreateImage(parent, "Top Color Band", new Color(0.25f, 0.55f, 0.72f));
            SetRect(skyBand.rectTransform, new Vector2(0f, 0.88f), Vector2.one, Vector2.zero, Vector2.zero);
        }

        private RectTransform CreatePanel(Transform parent, string title, Color color)
        {
            var panel = CreateImage(parent, title, color);
            panel.raycastTarget = false;

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f);
            outline.effectDistance = new Vector2(4f, -4f);

            var label = CreateText(panel.transform, title, 26, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(label.rectTransform, new Vector2(0f, 0.86f), Vector2.one, Vector2.zero, Vector2.zero);

            return panel.rectTransform;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            if (anchorMin == anchorMax)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private readonly struct FormationMove
        {
            public FormationMove(int unitRuntimeId, int fromSlotIndex, int toSlotIndex)
            {
                UnitRuntimeId = unitRuntimeId;
                FromSlotIndex = fromSlotIndex;
                ToSlotIndex = toSlotIndex;
            }

            public int UnitRuntimeId { get; }
            public int FromSlotIndex { get; }
            public int ToSlotIndex { get; }
        }

        private sealed class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private PrototypeBattleUi owner;
            private CardRecord card;
            private RectTransform rectTransform;
            private CanvasGroup canvasGroup;
            private Vector2 startAnchoredPosition;
            private Transform startParent;
            private bool dropped;
            private bool isDragging;

            public CardRecord Card => card;

            public void Initialize(PrototypeBattleUi owner, CardRecord card, RectTransform rectTransform, CanvasGroup canvasGroup)
            {
                this.owner = owner;
                this.card = card;
                this.rectTransform = rectTransform;
                this.canvasGroup = canvasGroup;
            }

            public void MarkDropped()
            {
                dropped = true;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                isDragging = false;
                if (owner == null || owner.isResolvingTurn || owner.currentEnergy < card.Cost)
                    return;

                isDragging = true;
                dropped = false;
                startParent = rectTransform.parent;
                startAnchoredPosition = rectTransform.anchoredPosition;
                rectTransform.SetAsLastSibling();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.85f;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (!isDragging || owner == null || owner.isResolvingTurn)
                    return;

                var canvas = GetComponentInParent<Canvas>();
                var scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
                rectTransform.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scaleFactor);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (!isDragging)
                    return;

                isDragging = false;
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.alpha = 1f;
                }

                if (dropped || rectTransform == null)
                    return;

                if (startParent != null)
                    rectTransform.SetParent(startParent, false);

                rectTransform.anchoredPosition = startAnchoredPosition;
            }
        }

        private sealed class BoardInsertDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
        {
            private PrototypeBattleUi owner;
            private int insertIndex;
            private bool insertAsGap;
            private Image image;
            private Image marker;
            private Color normalColor;

            public void Initialize(PrototypeBattleUi owner, int insertIndex, bool insertAsGap, Image image, Image marker = null)
            {
                this.owner = owner;
                this.insertIndex = insertIndex;
                this.insertAsGap = insertAsGap;
                this.image = image;
                this.marker = marker;
                normalColor = image.color;
            }

            public void OnDrop(PointerEventData eventData)
            {
                var dragHandler = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardDragHandler>();
                if (dragHandler == null || owner == null)
                    return;

                dragHandler.MarkDropped();
                if (insertAsGap)
                    owner.PlayCardInGap(dragHandler.Card, insertIndex);
                else
                    owner.PlayCardAt(dragHandler.Card, insertIndex);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (insertAsGap)
                {
                    if (image != null)
                        image.color = new Color(0.1f, 0.85f, 0.38f, 0.08f);

                    if (marker != null)
                        marker.enabled = true;

                    return;
                }

                if (image != null)
                    image.color = new Color(0.2f, 0.7f, 0.95f, 0.28f);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (image != null)
                    image.color = normalColor;

                if (marker != null)
                    marker.enabled = false;
            }
        }

        private sealed class BattleUnit
        {
            private readonly CardRecord card;

            public BattleUnit(CardRecord card, int runtimeId)
            {
                this.card = card;
                RuntimeId = runtimeId;
                CurrentHp = card.Hp;
            }

            public int RuntimeId { get; }
            public string Name => card.CardName;
            public int Attack => card.Attack;
            public int MaxHp => card.Hp;
            public CardCamp Camp => card.Camp;
            public int CurrentHp { get; private set; }
            public bool IsDead => CurrentHp <= 0;

            public void TakeDamage(int amount)
            {
                CurrentHp = Mathf.Max(0, CurrentHp - amount);
            }
        }
    }
}

