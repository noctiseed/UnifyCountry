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
        private CardHoverAnimator hoverAnimator;
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
            hoverAnimator = GetComponent<CardHoverAnimator>();
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
            if (hoverAnimator != null)
                hoverAnimator.SetSuppressed(true);

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

            if (hoverAnimator != null)
                hoverAnimator.SetSuppressed(false);

            if (dropped || rectTransform == null)
                return;

            if (startParent != null)
                rectTransform.SetParent(startParent, false);

            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }

    public sealed class CardHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverLift = 34f;
        private const float HoverScale = 1.12f;
        private const float AnimationSpeed = 14f;

        private static CardHoverAnimator activeHover;

        private RectTransform rectTransform;
        private Vector2 baseAnchoredPosition;
        private Vector3 baseScale;
        private bool initialized;
        private bool hovered;
        private bool suppressed;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (suppressed)
                return;

            CaptureBaseTransform();
            SetActiveHover(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearActiveHover(this);
        }

        public void SetSuppressed(bool value)
        {
            suppressed = value;
            ClearActiveHover(this);
            hovered = false;
            if (value)
                SnapToBase();
        }

        private void Update()
        {
            if (rectTransform == null || suppressed)
                return;

            CaptureBaseTransform();

            var targetPosition = hovered ? baseAnchoredPosition + new Vector2(0f, HoverLift) : baseAnchoredPosition;
            var targetScale = hovered ? baseScale * HoverScale : baseScale;
            var t = 1f - Mathf.Exp(-AnimationSpeed * Time.deltaTime);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, t);
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, t);

            if (hovered)
            {
                transform.SetAsLastSibling();
            }
        }

        private void CaptureBaseTransform()
        {
            if (initialized || rectTransform == null)
                return;

            baseAnchoredPosition = rectTransform.anchoredPosition;
            baseScale = rectTransform.localScale;
            initialized = true;
        }

        private static void SetActiveHover(CardHoverAnimator next)
        {
            if (activeHover == next)
            {
                next.hovered = true;
                next.transform.SetAsLastSibling();
                return;
            }

            if (activeHover != null)
                activeHover.hovered = false;

            activeHover = next;
            activeHover.hovered = true;
            activeHover.transform.SetAsLastSibling();
        }

        private static void ClearActiveHover(CardHoverAnimator target)
        {
            if (activeHover != target)
                return;

            target.hovered = false;
            activeHover = null;
        }

        private void SnapToBase()
        {
            if (!initialized || rectTransform == null)
                return;

            rectTransform.anchoredPosition = baseAnchoredPosition;
            rectTransform.localScale = baseScale;
        }

        private void OnDisable()
        {
            ClearActiveHover(this);
        }
    }

    public sealed class DropdownOptionHighlightHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image highlight;
        private Color hoverColor;
        private Color pressedColor;

        public void Initialize(Image highlight, Color hoverColor, Color pressedColor)
        {
            this.highlight = highlight;
            this.hoverColor = hoverColor;
            this.pressedColor = pressedColor;
            Clear();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetColor(hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetColor(pressedColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetColor(hoverColor);
        }

        private void Clear()
        {
            SetColor(Color.clear);
        }

        private void SetColor(Color color)
        {
            if (highlight != null)
                highlight.color = color;
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

    public sealed class PrototypeTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private string message;
        private Font font;
        private RectTransform tooltipRect;
        private Canvas canvas;

        public void Initialize(string message, Font font)
        {
            this.message = message;
            this.font = font;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRect != null)
                return;

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var panelObject = new GameObject("Tooltip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvas.transform, false);
            panelObject.transform.SetAsLastSibling();

            tooltipRect = panelObject.GetComponent<RectTransform>();
            tooltipRect.anchorMin = new Vector2(0f, 0f);
            tooltipRect.anchorMax = new Vector2(0f, 0f);
            tooltipRect.pivot = new Vector2(0f, 0f);
            tooltipRect.sizeDelta = new Vector2(170f, 42f);

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.075f, 0.04f, 0.94f);
            image.raycastTarget = false;

            var outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.98f, 0.82f, 0.42f, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 5f);
            textRect.offsetMax = new Vector2(-10f, -5f);

            var text = textObject.GetComponent<Text>();
            text.text = message;
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 18;

            MoveTooltip(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            MoveTooltip(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipRect == null)
                return;

            Destroy(tooltipRect.gameObject);
            tooltipRect = null;
        }

        private void MoveTooltip(PointerEventData eventData)
        {
            if (tooltipRect == null || canvas == null)
                return;

            var scaleFactor = Mathf.Max(0.01f, canvas.scaleFactor);
            var position = eventData.position / scaleFactor + new Vector2(14f, 16f);
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                var maxX = canvasRect.rect.width - tooltipRect.sizeDelta.x - 8f;
                var maxY = canvasRect.rect.height - tooltipRect.sizeDelta.y - 8f;
                position.x = Mathf.Clamp(position.x, 8f, Mathf.Max(8f, maxX));
                position.y = Mathf.Clamp(position.y, 8f, Mathf.Max(8f, maxY));
            }

            tooltipRect.anchoredPosition = position;
        }

        private void OnDisable()
        {
            if (tooltipRect != null)
                Destroy(tooltipRect.gameObject);
        }
    }
}
