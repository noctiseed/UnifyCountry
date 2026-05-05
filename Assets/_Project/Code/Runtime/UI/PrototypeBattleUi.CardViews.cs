using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private RectTransform CreateReadonlyCard(Transform parent, CardRecord card)
        {
            return CreateCardBase(parent, card).rectTransform;
        }

        private Button CreateCard(Transform parent, CardRecord card)
        {
            var image = CreateCardBase(parent, card);
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(() => PlayCardFromHand(card, image.rectTransform));
            button.interactable = !isResolvingTurn && currentEnergy >= card.Cost;
            var canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            image.gameObject.AddComponent<CardHoverAnimator>();
            var dragHandler = image.gameObject.AddComponent<CardDragHandler>();
            dragHandler.Initialize(this, card, image.rectTransform, canvasGroup);

            return button;
        }

        private Image CreateCardBase(Transform parent, CardRecord card)
        {
            var color = GetCardColor(card);
            var image = CreateImage(parent, card.CardName, color);

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

            var statsText = card.CardType == CardType.Unit ? $"攻 {card.Attack}   血 {card.Hp}" : GetCardTypeLabel(card.CardType);
            var stats = CreateText(image.transform, statsText, 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.12f, 0.08f));
            SetRect(stats.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.25f), Vector2.zero, Vector2.zero);

            if (card.Effects.Count > 0)
            {
                var effect = CreateText(image.transform, card.Effects[0].EffectName, 14, TextAnchor.MiddleCenter, new Color(0.22f, 0.12f, 0.08f));
                SetRect(effect.rectTransform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
            }

            CreateBorder(image.transform, new Color(0.22f, 0.16f, 0.1f), 3f);
            return image;
        }

        private static string GetCardTypeLabel(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Unit:
                    return "单位卡";
                case CardType.Skill:
                    return "计谋卡";
                case CardType.Equipment:
                    return "装备卡";
                case CardType.Power:
                    return "能力卡";
                case CardType.Event:
                    return "事件卡";
                default:
                    return "卡牌";
            }
        }
    }
}
