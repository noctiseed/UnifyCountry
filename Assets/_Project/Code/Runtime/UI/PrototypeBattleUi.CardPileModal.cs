using System.Collections.Generic;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void BuildHand(Transform parent)
        {
            for (var i = 0; i < Mathf.Min(hand.Count, 5); i++)
            {
                var card = hand[i];
                var cardView = CreateCard(parent, card);
                SetRect(cardView.GetComponent<RectTransform>(), new Vector2(0.025f + i * 0.19f, 0.12f), new Vector2(0.18f + i * 0.19f, 0.78f), Vector2.zero, Vector2.zero);
            }
        }

        private void ShowCardPileModal(Transform parent, string title, IReadOnlyList<CardRecord> cards)
        {
            CloseExistingCardPileModals(parent);
            CreateLargeModal(parent, $"{title}  {cards.Count} 张", out var contentRoot, out var headerRoot);

            var filterIndex = 0;
            var scrollRect = contentRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = CreateImage(contentRoot, "Viewport", Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetRect(viewport.rectTransform, new Vector2(0.02f, 0.04f), new Vector2(0.955f, 0.96f), Vector2.zero, Vector2.zero);

            var contentObject = new GameObject("Cards", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);

            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = contentObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(138f, 196f);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(14, 14, 14, 14);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var emptyText = CreateText(viewport.transform, "暂无卡牌", 28, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.08f));
            SetRect(emptyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var scrollbar = CreateSlimVerticalScrollbar(contentRoot);
            SetRect(scrollbar.GetComponent<RectTransform>(), new Vector2(0.975f, 0.04f), new Vector2(0.985f, 0.96f), Vector2.zero, Vector2.zero);
            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalNormalizedPosition = 1f;

            CreateCardPileFilterDropdown(headerRoot, selectedFilterIndex =>
            {
                filterIndex = selectedFilterIndex;
                PopulateCardPileContent(content, emptyText, cards, filterIndex);
                scrollRect.verticalNormalizedPosition = 1f;
            });
            headerRoot.SetAsLastSibling();

            PopulateCardPileContent(content, emptyText, cards, filterIndex);
        }

        private void CloseExistingCardPileModals(Transform parent)
        {
            if (parent == null)
                return;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null || !child.name.EndsWith(" Modal"))
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private RectTransform CreateCardPileFilterDropdown(Transform parent, System.Action<int> onFilterChanged)
        {
            var root = CreateImage(parent, "Card Pile Filter", new Color(0.82f, 0.66f, 0.42f));
            root.sprite = GetRoundedButtonSprite();
            root.type = Image.Type.Sliced;
            SetRect(root.rectTransform, new Vector2(0f, 0.12f), new Vector2(0.18f, 0.9f), Vector2.zero, Vector2.zero);

            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.16f, 0.1f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root;

            var caption = CreateText(root.transform, "所有", 20, TextAnchor.MiddleLeft, Color.white);
            SetRect(caption.rectTransform, new Vector2(0.1f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);
            caption.resizeTextMinSize = 14;

            var arrow = CreateText(root.transform, "v", 18, TextAnchor.MiddleCenter, Color.white);
            SetRect(arrow.rectTransform, new Vector2(0.78f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);

            var optionNormalColor = new Color(0.86f, 0.69f, 0.44f, 0.96f);
            var menu = CreateImage(parent, "Card Pile Filter Menu", optionNormalColor);
            menu.sprite = GetRoundedButtonSprite();
            menu.type = Image.Type.Sliced;
            SetRect(menu.rectTransform, new Vector2(0f, -1.98f), new Vector2(0.18f, 0.04f), Vector2.zero, Vector2.zero);
            var menuMask = menu.gameObject.AddComponent<Mask>();
            menuMask.showMaskGraphic = true;
            menu.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f, 0.75f);
            menu.transform.SetAsLastSibling();
            menu.gameObject.SetActive(false);

            var labels = new[] { "所有", "单位卡", "计谋卡" };
            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var option = CreateImage(menu.transform, labels[i], new Color(1f, 1f, 1f, 0f));
                var optionButton = option.gameObject.AddComponent<Button>();
                optionButton.targetGraphic = option;
                optionButton.transition = Selectable.Transition.None;
                SetRect(option.rectTransform, new Vector2(0f, 1f - (i + 1) / 3f), new Vector2(1f, 1f - i / 3f), Vector2.zero, Vector2.zero);

                var highlight = CreateImage(option.transform, "Highlight", Color.clear);
                highlight.raycastTarget = false;
                SetRect(highlight.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                var optionText = CreateText(option.transform, labels[i], 18, TextAnchor.MiddleLeft, new Color(0.18f, 0.12f, 0.08f));
                SetRect(optionText.rectTransform, new Vector2(0.16f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

                var highlightHandler = option.gameObject.AddComponent<DropdownOptionHighlightHandler>();
                highlightHandler.Initialize(highlight, new Color(1f, 0.92f, 0.72f, 0.24f), new Color(0.42f, 0.24f, 0.1f, 0.16f));

                optionButton.onClick.AddListener(() =>
                {
                    caption.text = labels[index];
                    menu.gameObject.SetActive(false);
                    onFilterChanged?.Invoke(index);
                });
            }

            button.onClick.AddListener(() =>
            {
                menu.gameObject.SetActive(!menu.gameObject.activeSelf);
                menu.transform.SetAsLastSibling();
            });

            return root.rectTransform;
        }

        private void PopulateCardPileContent(RectTransform content, Text emptyText, IReadOnlyList<CardRecord> cards, int filterIndex)
        {
            ClearChildren(content);

            var visibleCount = 0;
            for (var i = 0; i < cards.Count; i++)
            {
                if (!MatchesCardPileFilter(cards[i], filterIndex))
                    continue;

                CreateReadonlyCard(content, cards[i]);
                visibleCount++;
            }

            emptyText.gameObject.SetActive(visibleCount == 0);
        }

        private static bool MatchesCardPileFilter(CardRecord card, int filterIndex)
        {
            switch (filterIndex)
            {
                case 1:
                    return card.CardType == CardType.Unit;
                case 2:
                    return card.CardType == CardType.Skill;
                default:
                    return true;
            }
        }
    }
}
