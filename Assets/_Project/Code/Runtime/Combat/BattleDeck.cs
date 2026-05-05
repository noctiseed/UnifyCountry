using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.Combat
{
    internal sealed class BattleDeck
    {
        public const int MaxHandSize = 10;

        private readonly BattleState state;

        public BattleDeck(BattleState state)
        {
            this.state = state;
        }

        public void DrawInitialHand(int initialHandSize)
        {
            var heroCards = state.DrawPile
                .Where(card => card.CardType == CardType.Unit && card.UnitType == UnitType.Hero && card.Camp == CardCamp.Player)
                .ToList();

            Shuffle(heroCards);

            var heroCount = Mathf.Min(initialHandSize, heroCards.Count);
            for (var i = 0; i < heroCount; i++)
            {
                var hero = heroCards[i];
                if (state.DrawPile.Remove(hero))
                    state.Hand.Add(hero);
            }

            DrawCards(initialHandSize - state.Hand.Count);
        }

        public void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!DrawOneCard())
                    break;
            }
        }

        public int DrawCardsWithCount(int count)
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

        public bool DrawOneCard()
        {
            if (state.Hand.Count >= MaxHandSize)
                return false;

            if (state.DrawPile.Count == 0)
                RefillDrawPileFromDiscard();

            if (state.DrawPile.Count == 0)
                return false;

            var card = state.DrawPile[0];
            state.DrawPile.RemoveAt(0);
            state.Hand.Add(card);
            return true;
        }

        public void DiscardHand(List<string> logLines)
        {
            if (state.Hand.Count == 0)
                return;

            var count = state.Hand.Count;
            state.DiscardPile.AddRange(state.Hand);
            state.Hand.Clear();
            logLines.Add($"未使用的 {count} 张手牌进入弃牌堆。");
        }

        public void RefillDrawPileFromDiscard()
        {
            if (state.DiscardPile.Count == 0)
                return;

            state.DrawPile.AddRange(state.DiscardPile);
            state.DiscardPile.Clear();
            Shuffle(state.DrawPile);
        }

        public static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var value = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = value;
            }
        }
    }
}
