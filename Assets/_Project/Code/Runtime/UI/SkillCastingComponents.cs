using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    internal enum SkillTargetKind
    {
        None,
        Unit,
        Row
    }

    internal readonly struct SkillTarget
    {
        public SkillTarget(SkillTargetKind kind, bool playerSide, int row, int slotIndex, BattleUnit unit)
        {
            Kind = kind;
            PlayerSide = playerSide;
            Row = row;
            SlotIndex = slotIndex;
            Unit = unit;
        }

        public SkillTargetKind Kind { get; }
        public bool PlayerSide { get; }
        public int Row { get; }
        public int SlotIndex { get; }
        public BattleUnit Unit { get; }

        public static SkillTarget ForUnit(bool playerSide, int row, int slotIndex, BattleUnit unit)
        {
            return new SkillTarget(SkillTargetKind.Unit, playerSide, row, slotIndex, unit);
        }

        public static SkillTarget ForRow(bool playerSide, int row)
        {
            return new SkillTarget(SkillTargetKind.Row, playerSide, row, -1, null);
        }
    }

    internal sealed class SkillTargetHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private PrototypeBattleUi owner;
        private SkillTarget target;
        private Image highlight;
        private Outline highlightOutline;

        public SkillTarget Target => target;

        public void Initialize(PrototypeBattleUi owner, SkillTarget target)
        {
            this.owner = owner;
            this.target = target;
            owner.RegisterSkillTargetHandler(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.PreviewSkillTarget(target);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.ClearSkillTargetPreview(target);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                owner?.TryConfirmSkillTarget(target);
        }

        public void SetSkillHighlight(bool active, bool valid)
        {
            if (!active)
            {
                if (highlight != null)
                    highlight.enabled = false;
                return;
            }

            EnsureHighlight();
            highlight.enabled = true;
            highlight.color = valid
                ? new Color(0.18f, 1f, 0.38f, target.Kind == SkillTargetKind.Row ? 0.24f : 0.32f)
                : new Color(0.95f, 0.16f, 0.12f, target.Kind == SkillTargetKind.Row ? 0.22f : 0.3f);
            highlightOutline.effectColor = valid
                ? new Color(0.45f, 1f, 0.55f, 0.72f)
                : new Color(1f, 0.18f, 0.12f, 0.9f);
        }

        private void EnsureHighlight()
        {
            if (highlight != null)
                return;

            var highlightObject = new GameObject("Skill Target Highlight", typeof(RectTransform), typeof(Image), typeof(Outline));
            highlightObject.transform.SetParent(transform, false);
            highlightObject.transform.SetAsLastSibling();

            var rect = highlightObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            highlight = highlightObject.GetComponent<Image>();
            highlight.raycastTarget = false;

            highlightOutline = highlightObject.GetComponent<Outline>();
            highlightOutline.effectDistance = target.Kind == SkillTargetKind.Row
                ? new Vector2(3f, -3f)
                : new Vector2(2f, -2f);
        }

        private void OnDestroy()
        {
            owner?.UnregisterSkillTargetHandler(this);
        }
    }

    internal sealed class DashedArrowGraphic : MonoBehaviour
    {
        private const float SegmentLength = 18f;
        private const float GapLength = 10f;
        private const float LineWidth = 5f;
        private const float HeadLength = 22f;
        private const float HeadWidth = 17f;

        private readonly List<Image> segments = new List<Image>();
        private Vector2 start;
        private Vector2 end;
        private Color arrowColor = new Color(1f, 0.78f, 0.22f, 0.95f);

        public bool raycastTarget { get; set; }

        public Color color
        {
            get => arrowColor;
            set
            {
                arrowColor = value;
                for (var i = 0; i < segments.Count; i++)
                {
                    if (segments[i] != null)
                        segments[i].color = arrowColor;
                }
            }
        }

        public void SetPoints(Vector2 startPoint, Vector2 endPoint)
        {
            start = startPoint;
            end = endPoint;
            RebuildSegments();
        }

        private void RebuildSegments()
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length < 8f)
            {
                SetVisibleCount(0);
                return;
            }

            var direction = delta / length;
            var normal = new Vector2(-direction.y, direction.x);
            var lineEndDistance = Mathf.Max(0f, length - HeadLength);

            var visibleCount = 0;
            var distance = 0f;
            while (distance < lineEndDistance)
            {
                var segmentEndDistance = Mathf.Min(distance + SegmentLength, lineEndDistance);
                SetLineSegment(visibleCount++, start + direction * distance, start + direction * segmentEndDistance, LineWidth);
                distance += SegmentLength + GapLength;
            }

            var baseCenter = end - direction * HeadLength;
            SetLineSegment(visibleCount++, end, baseCenter + normal * HeadWidth * 0.5f, LineWidth);
            SetLineSegment(visibleCount++, end, baseCenter - normal * HeadWidth * 0.5f, LineWidth);
            SetVisibleCount(visibleCount);
        }

        private void SetLineSegment(int index, Vector2 a, Vector2 b, float width)
        {
            var image = GetSegment(index);
            var rect = image.rectTransform;
            var delta = b - a;
            var length = Mathf.Max(1f, delta.magnitude);
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            image.enabled = true;
            image.color = arrowColor;
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.sizeDelta = new Vector2(length, width);
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private Image GetSegment(int index)
        {
            while (segments.Count <= index)
            {
                var segmentObject = new GameObject("Dash", typeof(RectTransform), typeof(Image));
                segmentObject.transform.SetParent(transform, false);

                var rect = segmentObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                var image = segmentObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = arrowColor;
                segments.Add(image);
            }

            return segments[index];
        }

        private void SetVisibleCount(int count)
        {
            for (var i = 0; i < segments.Count; i++)
                segments[i].enabled = i < count;
        }
    }
}
