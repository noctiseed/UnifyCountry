using System.Collections.Generic;
using UnifyCountry.Combat;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void DrawInitialHand()
        {
            battleDeck.DrawInitialHand(InitialHandSize);
        }

        private void DrawCards(int count)
        {
            battleDeck.DrawCards(count);
        }

        private bool DrawOneCard()
        {
            return battleDeck.DrawOneCard();
        }

        private void RefillDrawPileFromDiscard()
        {
            battleDeck.RefillDrawPileFromDiscard();
        }

        private static void Shuffle<T>(IList<T> list)
        {
            BattleDeck.Shuffle(list);
        }
    }
}
