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
        private const int MaxFormationSlots = 5;
        private const float FormationMoveDuration = 0.45f;

        private Dictionary<string, CardRecord> cardMap = new Dictionary<string, CardRecord>();
        private readonly List<CardRecord> drawPile = new List<CardRecord>();
        private readonly List<CardRecord> hand = new List<CardRecord>();
        private readonly List<BattleUnit> playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit> enemyUnits = new List<BattleUnit>();
        private readonly Dictionary<int, RectTransform> unitViews = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, int> animatedSlotOverrides = new Dictionary<int, int>();
        private List<List<string>> waveSlots = new List<List<string>>();

        private int turnNumber = 1;
        private int nextWaveIndex;
        private int nextUnitRuntimeId = 1;
        private string battleLog = "\u62d6\u52a8\u624b\u724c\u5230\u53cb\u65b9\u9635\u5730\u4e0a\u9635\uff0c\u7136\u540e\u70b9\u51fb\u7ed3\u675f\u56de\u5408\u3002";
        private bool initialized;
        private bool isResolvingTurn;

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
            hand.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            nextUnitRuntimeId = 1;

            var startingDeck = PrototypeCsvDatabase.LoadStartingDeck(startingDeckCsv);
            foreach (var entry in startingDeck)
            {
                if (!cardMap.TryGetValue(entry.Key, out var card))
                    continue;

                for (var i = 0; i < entry.Value; i++)
                    drawPile.Add(card);
            }

            DrawGuaranteedFirstHand();
            turnNumber = 1;
            nextWaveIndex = 0;
            battleLog = "\u51c6\u5907\u9636\u6bb5\uff1a\u82f1\u96c4\u5361\u5df2\u8fdb\u5165\u9996\u56de\u5408\u624b\u724c\u3002";
            initialized = true;
        }

        private void DrawGuaranteedFirstHand()
        {
            var heroes = drawPile.Where(card => card.UnitType == UnitType.Hero && card.Camp == CardCamp.Player).ToList();
            foreach (var hero in heroes)
            {
                hand.Add(hero);
                drawPile.Remove(hero);
            }

            while (hand.Count < 3 && drawPile.Count > 0)
                DrawOneCard();
        }

        private void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
                DrawOneCard();
        }

        private void DrawOneCard()
        {
            if (drawPile.Count == 0)
                return;

            var index = Random.Range(0, drawPile.Count);
            var card = drawPile[index];
            drawPile.RemoveAt(index);
            hand.Add(card);
        }

        private void PlayCard(CardRecord card)
        {
            PlayCardAt(card, playerUnits.Count);
        }

        private void PlayCardAt(CardRecord card, int insertIndex)
        {
            if (card == null || card.Camp != CardCamp.Player)
                return;

            if (isResolvingTurn)
                return;

            if (playerUnits.Count >= MaxFormationSlots)
            {
                battleLog = "\u53cb\u65b9\u9635\u5730\u5df2\u6ee1\uff0c\u65e0\u6cd5\u7ee7\u7eed\u4e0a\u9635\u3002";
                BuildUi();
                return;
            }

            hand.Remove(card);
            insertIndex = Mathf.Clamp(insertIndex, 0, playerUnits.Count);
            playerUnits.Insert(insertIndex, new BattleUnit(card, nextUnitRuntimeId++));
            battleLog = $"{card.CardName} \u4e0a\u9635\u3002\u53cb\u65b9\u6700\u53f3\u4fa7\u627f\u4f24 1 \u4f18\u5148\u627f\u4f24\u3002";
            BuildUi();
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
            logLines.Add($"\u7b2c {turnNumber} \u56de\u5408\u7ed3\u675f\u3002");

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            SpawnCurrentWave(logLines);
            battleLog = string.Join("\n", logLines);
            BuildUi();
            yield return new WaitForSeconds(0.75f);

            ResolveEnemyAttack(logLines);
            battleLog = string.Join("\n", logLines);
            BuildUi();
            yield return new WaitForSeconds(0.75f);

            ResolvePlayerAttack(logLines);
            AppendDeathLogs(logLines);

            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));

            if (enemyUnits.Count == 0 && nextWaveIndex >= waveSlots.Count)
            {
                battleLog = string.Join("\n", logLines) + "\n\u6218\u6597\u80dc\u5229\uff01";
                isResolvingTurn = false;
                BuildUi();
                yield break;
            }

            turnNumber++;
            DrawCards(3);
            logLines.Add($"\u8fdb\u5165\u7b2c {turnNumber} \u56de\u5408\uff0c\u62bd 3 \u5f20\u724c\u3002");
            battleLog = string.Join("\n", logLines);
            isResolvingTurn = false;
            BuildUi();
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
                logLines.Add($"\u7b2c {nextWaveIndex} \u6ce2\u51fa\u73b0\uff1a{string.Join("\u3001", spawnedNames)}\u3002");
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
                    logLines.Add("\u53cb\u65b9\u9635\u5730\u65e0\u5355\u4f4d\uff0c\u654c\u65b9\u6682\u65e0\u76ee\u6807\u3002");
                    return;
                }

                target.TakeDamage(attacker.Attack);
                logLines.Add($"{attacker.Name} \u653b\u51fb {target.Name}\uff0c\u9020\u6210 {attacker.Attack} \u70b9\u4f24\u5bb3\u3002");
            }
        }

        private void ResolvePlayerAttack(List<string> logLines)
        {
            for (var i = playerUnits.Count - 1; i >= 0; i--)
            {
                var attacker = playerUnits[i];
                if (attacker.IsDead)
                    continue;

                var target = GetEnemyFrontUnit();
                if (target == null)
                    return;

                target.TakeDamage(attacker.Attack);
                logLines.Add($"{attacker.Name} \u53cd\u51fb {target.Name}\uff0c\u9020\u6210 {attacker.Attack} \u70b9\u4f24\u5bb3\u3002");
            }
        }

        private BattleUnit GetPlayerFrontUnit()
        {
            for (var i = playerUnits.Count - 1; i >= 0; i--)
            {
                if (!playerUnits[i].IsDead)
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

            logLines.Add("\u9635\u578b\u5411\u524d\u8865\u4f4d\u3002");
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
                if (!unit.IsDead)
                    oldSlots[unit.RuntimeId] = GetVisualSlotIndex(playerSide, i, units.Count);
            }

            units.RemoveAll(unit => unit.IsDead);

            var moves = new List<FormationMove>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
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
                    logLines.Add($"{unit.Name} \u9635\u4ea1\u3002");
            }

            foreach (var unit in playerUnits)
            {
                if (unit.IsDead)
                    logLines.Add($"{unit.Name} \u9635\u4ea1\u3002");
            }
        }

        private void BuildUi()
        {
            ClearChildren();
            unitViews.Clear();

            var canvas = CreateCanvas();
            EnsureEventSystem();
            CreateBackground(canvas.transform);

            var title = CreateText(canvas.transform, "\u4e09\u56fd\u5361\u724c\u6218\u7ebf - \u53ef\u73a9\u539f\u578b", 38, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(760f, 56f));

            var status = CreateText(canvas.transform, $"\u7b2c {turnNumber} \u56de\u5408  |  \u724c\u5e93 {drawPile.Count}  |  \u624b\u724c {hand.Count}  |  \u4e0b\u4e00\u6ce2 {Mathf.Min(nextWaveIndex + 1, waveSlots.Count)} / {waveSlots.Count}", 22, TextAnchor.MiddleCenter, new Color(0.22f, 0.16f, 0.1f));
            SetRect(status.rectTransform, new Vector2(0.18f, 0.895f), new Vector2(0.82f, 0.945f), Vector2.zero, Vector2.zero);

            var playerPanel = CreatePanel(canvas.transform, "\u53cb\u65b9\u9635\u5730", playerPanelColor);
            SetRect(playerPanel, new Vector2(0.02f, 0.31f), new Vector2(0.49f, 0.88f), Vector2.zero, Vector2.zero);
            BuildBoard(playerPanel.transform, true, playerUnits);

            var enemyPanel = CreatePanel(canvas.transform, "\u654c\u65b9\u9635\u5730", enemyPanelColor);
            SetRect(enemyPanel, new Vector2(0.51f, 0.31f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            BuildBoard(enemyPanel.transform, false, enemyUnits);
            BuildUpcomingWaveHint(enemyPanel.transform);

            var handPanel = CreatePanel(canvas.transform, "\u624b\u724c\uff08\u62d6\u5165\u53cb\u65b9\u9635\u5730\u4e0a\u9635\uff09", handPanelColor);
            SetRect(handPanel, new Vector2(0.02f, 0.03f), new Vector2(0.73f, 0.28f), Vector2.zero, Vector2.zero);
            BuildHand(handPanel.transform);

            var logPanel = CreatePanel(canvas.transform, "\u6218\u6597\u8bb0\u5f55", new Color(0.93f, 0.84f, 0.64f));
            SetRect(logPanel, new Vector2(0.75f, 0.03f), new Vector2(0.98f, 0.28f), Vector2.zero, Vector2.zero);
            var logText = CreateText(logPanel.transform, battleLog, 18, TextAnchor.UpperLeft, new Color(0.18f, 0.12f, 0.08f));
            SetRect(logText.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);

            var endTurnButton = CreateButton(canvas.transform, "\u7ed3\u675f\u56de\u5408");
            SetRect(endTurnButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.295f), new Vector2(0.87f, 0.385f), Vector2.zero, Vector2.zero);
            endTurnButton.interactable = !isResolvingTurn;
            endTurnButton.onClick.AddListener(EndTurn);

            var resetButton = CreateButton(canvas.transform, "\u91cd\u5f00");
            SetRect(resetButton.GetComponent<RectTransform>(), new Vector2(0.89f, 0.295f), new Vector2(0.98f, 0.385f), Vector2.zero, Vector2.zero);
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
                var label = CreateText(slot.transform, playerSide ? $"\u627f\u4f24 {order}" : $"\u654c\u4f4d {order}", 18, TextAnchor.LowerCenter, new Color(0.23f, 0.16f, 0.1f));
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            for (var i = 0; i < units.Count; i++)
            {
                var index = GetVisualSlotIndex(playerSide, i, units.Count);
                if (animatedSlotOverrides.TryGetValue(units[i].RuntimeId, out var overrideSlot))
                    index = overrideSlot;

                var unit = CreateUnitToken(parent, units[i], false);
                SetRect(unit, GetUnitAnchorMin(index), GetUnitAnchorMax(index), Vector2.zero, Vector2.zero);
                unitViews[units[i].RuntimeId] = unit;
            }

            if (playerSide && !isResolvingTurn && units.Count < MaxFormationSlots)
            {
                for (var i = 0; i <= units.Count; i++)
                    CreatePlayerInsertDropZone(parent, i, units.Count);
            }
        }

        private void CreatePlayerInsertDropZone(Transform parent, int insertIndex, int unitCount)
        {
            var targetSlot = MaxFormationSlots - (unitCount + 1) + insertIndex;
            var zone = CreateImage(parent, $"Insert {insertIndex}", new Color(0.2f, 0.7f, 0.95f, 0.08f));
            SetRect(zone.rectTransform, GetSlotAnchorMin(targetSlot), GetSlotAnchorMax(targetSlot), Vector2.zero, Vector2.zero);

            var outline = zone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.45f, 0.85f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            var dropZone = zone.gameObject.AddComponent<BoardInsertDropZone>();
            dropZone.Initialize(this, insertIndex, zone);
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
            var hint = nextWaveIndex < waveSlots.Count ? $"\u4e0b\u6ce2\uff1a{DescribeWave(waveSlots[nextWaveIndex])}" : "\u5df2\u65e0\u540e\u7eed\u6ce2\u6b21";
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

            return names.Count == 0 ? "-" : string.Join("\u3001", names);
        }

        private void BuildHand(Transform parent)
        {
            for (var i = 0; i < Mathf.Min(hand.Count, 7); i++)
            {
                var card = hand[i];
                var cardView = CreateCard(parent, card);
                SetRect(cardView.GetComponent<RectTransform>(), new Vector2(0.02f + i * 0.135f, 0.12f), new Vector2(0.13f + i * 0.135f, 0.78f), Vector2.zero, Vector2.zero);
            }
        }

        private Button CreateCard(Transform parent, CardRecord card)
        {
            var color = card.UnitType == UnitType.Hero ? heroCardColor : soldierCardColor;
            var image = CreateImage(parent, card.CardName, color);
            image.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => PlayCard(card));
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

            var stats = CreateText(image.transform, $"\u653b {card.Attack}   \u8840 {card.Hp}", 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.08f));
            SetRect(stats.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.31f), Vector2.zero, Vector2.zero);

            var typeText = card.UnitType == UnitType.Hero ? "\u552f\u4e00" : "\u666e\u901a";
            var type = CreateText(image.transform, typeText, 16, TextAnchor.MiddleCenter, Color.white);
            SetRect(type.rectTransform, new Vector2(0.68f, 0.02f), new Vector2(0.96f, 0.16f), Vector2.zero, Vector2.zero);

            return button;
        }

        private RectTransform CreateUnitToken(Transform parent, BattleUnit unit, bool compact)
        {
            var root = CreateImage(parent, unit.Name, unit.Camp == CardCamp.Enemy ? enemyCardColor : heroCardColor);
            root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var name = CreateText(root.transform, unit.Name, compact ? 17 : 18, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.05f));
            SetRect(name.rectTransform, new Vector2(0f, 0.58f), Vector2.one, Vector2.zero, Vector2.zero);

            var stats = CreateText(root.transform, $"\u653b{unit.Attack} / \u8840{unit.CurrentHp}", compact ? 15 : 16, TextAnchor.MiddleCenter, Color.white);
            SetRect(stats.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero);

            return root.rectTransform;
        }

        private RectTransform CreateBadge(Transform parent, string value, Color color)
        {
            var badge = CreateImage(parent, "Badge", color);
            badge.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.1f, 0.07f);

            var text = CreateText(badge.transform, value, 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return badge.rectTransform;
        }

        private Button CreateButton(Transform parent, string label)
        {
            var image = CreateImage(parent, label, new Color(0.9f, 0.28f, 0.21f));
            image.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.12f, 0.08f);

            var button = image.gameObject.AddComponent<Button>();
            var text = CreateText(image.transform, label, 28, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
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
                if (owner == null || owner.isResolvingTurn)
                    return;

                dropped = false;
                startParent = rectTransform.parent;
                startAnchoredPosition = rectTransform.anchoredPosition;
                rectTransform.SetAsLastSibling();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.85f;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (owner == null || owner.isResolvingTurn)
                    return;

                var canvas = GetComponentInParent<Canvas>();
                var scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
                rectTransform.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scaleFactor);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
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
            private Image image;
            private Color normalColor;

            public void Initialize(PrototypeBattleUi owner, int insertIndex, Image image)
            {
                this.owner = owner;
                this.insertIndex = insertIndex;
                this.image = image;
                normalColor = image.color;
            }

            public void OnDrop(PointerEventData eventData)
            {
                var dragHandler = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CardDragHandler>();
                if (dragHandler == null || owner == null)
                    return;

                dragHandler.MarkDropped();
                owner.PlayCardAt(dragHandler.Card, insertIndex);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (image != null)
                    image.color = new Color(0.2f, 0.7f, 0.95f, 0.28f);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (image != null)
                    image.color = normalColor;
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
