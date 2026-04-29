using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
            if (owner == null || !owner.CanDragCard(card))
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
            if (!isDragging || owner == null || !owner.CanDragCard(card))
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

    public sealed class BoardInsertDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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
}
