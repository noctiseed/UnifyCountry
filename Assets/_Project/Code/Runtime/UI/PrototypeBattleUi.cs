using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Combat;
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
        [SerializeField] private Sprite attackIconSprite;
        [SerializeField] private Sprite shieldIconSprite;
        [SerializeField] private Sprite regenerationIconSprite;

        private readonly Color backgroundColor = new Color(0.94f, 0.89f, 0.73f);
        private readonly Color playerPanelColor = new Color(0.76f, 0.92f, 0.67f);
        private readonly Color enemyPanelColor = new Color(0.96f, 0.68f, 0.58f);
        private readonly Color handPanelColor = new Color(0.99f, 0.94f, 0.72f);
        private readonly Color heroCardColor = new Color(1f, 0.82f, 0.36f);
        private readonly Color soldierCardColor = new Color(0.66f, 0.88f, 1f);
        private readonly Color enemyCardColor = new Color(1f, 0.56f, 0.5f);
        private Sprite roundedButtonSprite;
        private const int MaxFormationSlots = BattleFormation.MaxFormationSlots;
        private const int FormationRows = BattleFormation.FormationRows;
        private const int TotalFormationSlots = BattleFormation.TotalFormationSlots;
        private const int InitialPrepareEnergy = 5;
        private const int MaxEnergy = 3;
        private const int InitialHandSize = 5;
        private const int CardsDrawnPerTurn = 3;
        private const int PlayerBaseMaxHp = 10;
        private const float FormationMoveDuration = 0.45f;
        private const string InitialBattleLog = "拖动手牌到友方阵地上阵，然后点击结束回合。";

        private readonly BattleState battleState = new BattleState();
        private BattleDeck battleDeck;
        private BattleFormation battleFormation;
        private BattleEffectResolver battleEffectResolver;
        private Dictionary<string, Sprite> cardPortraitMap = new Dictionary<string, Sprite>();
        private readonly List<string> battleLogHistory = new List<string>();
        private readonly Dictionary<int, RectTransform> unitViews = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, int> animatedSlotOverrides = new Dictionary<int, int>();

        private Dictionary<string, CardRecord> cardMap { get => battleState.CardMap; set => battleState.CardMap = value; }
        private List<BattleLevelRecord> levels { get => battleState.Levels; set => battleState.Levels = value; }
        private List<CardRecord> library => battleState.Library;
        private List<CardRecord> drawPile => battleState.DrawPile;
        private List<CardRecord> discardPile => battleState.DiscardPile;
        private List<CardRecord> hand => battleState.Hand;
        private List<BattleUnit> playerUnits => battleState.PlayerUnits;
        private List<BattleUnit> enemyUnits => battleState.EnemyUnits;
        private int turnNumber { get => battleState.TurnNumber; set => battleState.TurnNumber = value; }
        private int currentLevelIndex { get => battleState.CurrentLevelIndex; set => battleState.CurrentLevelIndex = value; }
        private int nextWaveIndex { get => battleState.NextWaveIndex; set => battleState.NextWaveIndex = value; }
        private int currentEnergy { get => battleState.CurrentEnergy; set => battleState.CurrentEnergy = value; }
        private int playerBaseHp { get => battleState.PlayerBaseHp; set => battleState.PlayerBaseHp = value; }
        private BattlePhase battlePhase { get => battleState.BattlePhase; set => battleState.BattlePhase = value; }

        private string battleLog = InitialBattleLog;
        private Text battleLogText;
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
            battleLogText = null;
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

        private void EnsureBattleSystems()
        {
            if (battleDeck != null && battleFormation != null && battleEffectResolver != null)
                return;

            battleDeck = new BattleDeck(battleState);
            battleFormation = new BattleFormation(battleState);
            battleEffectResolver = new BattleEffectResolver(battleState, battleDeck, battleFormation);
        }

        private void InitializeBattle()
        {
            EnsureBattleSystems();

            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cards = PrototypeCsvDatabase.LoadCards(cardsCsv, unitsCsv, effectsCsv);
            cardMap = cards.ToDictionary(card => card.CardId);
            RebuildCardPortraitMap();
            levels = PrototypeCsvDatabase.LoadBattleLevels(wavesCsv);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, Mathf.Max(0, levels.Count - 1));

            battleState.ClearBattleCollections();
            battleLogHistory.Clear();
            battleState.EnsureFormationSlots();

            battleState.NextUnitRuntimeId = 1;
            battleEnded = false;
            playerBaseHp = PlayerBaseMaxHp;

            var startingDeck = PrototypeCsvDatabase.LoadStartingDeck(startingDeckCsv);
            foreach (var entry in startingDeck)
            {
                if (!cardMap.TryGetValue(entry.Key, out var card))
                    continue;

                for (var i = 0; i < entry.Value; i++)
                {
                    library.Add(card);
                    drawPile.Add(card);
                }
            }

            turnNumber = 0;
            nextWaveIndex = 0;
            currentEnergy = InitialPrepareEnergy;
            battlePhase = BattlePhase.InitialPrepare;
            Shuffle(drawPile);
            DrawInitialHand();
            AddBattleLogEntry($"准备阶段：获得 {InitialPrepareEnergy} 点费用，英雄卡优先进入初始手牌，补足 {InitialHandSize} 张。");
            initialized = true;
        }
    }
}
