using System.Collections;
using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private enum SkillTargetRequirement
        {
            None,
            AllyUnit,
            EnemyUnit,
            AnyUnit,
            AllyRow,
            EnemyRow,
            AnyRow
        }

        private readonly Color skillCardColor = new Color(0.7f, 0.62f, 0.92f);
        private readonly List<SkillTargetHandler> skillTargetHandlers = new List<SkillTargetHandler>();

        private CardRecord castingSkillCard;
        private RectTransform castingSkillCardRect;
        private Image castingSkillCardHighlight;
        private DashedArrowGraphic castingArrow;
        private SkillTarget currentPreviewTarget;

        private bool IsCastingSkill => castingSkillCard != null;

        private void Update()
        {
            if (!IsCastingSkill)
                return;

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSkillCast();
                return;
            }

            if (!hand.Contains(castingSkillCard) || isResolvingTurn || battleEnded)
            {
                CancelSkillCast();
                return;
            }

            UpdateSkillArrow();
        }

        private Color GetCardColor(CardRecord card)
        {
            if (card.CardType == CardType.Skill)
                return skillCardColor;

            return card.CardType == CardType.Unit && card.UnitType == UnitType.Hero ? heroCardColor : soldierCardColor;
        }

        private void PlayCardFromHand(CardRecord card, RectTransform sourceRect)
        {
            if (card == null)
                return;

            if (card.CardType == CardType.Skill)
            {
                BeginSkillCast(card, sourceRect);
                return;
            }

            return;
        }

        private void BeginSkillCast(CardRecord card, RectTransform sourceRect)
        {
            if (card == null || card.CardType != CardType.Skill || card.Camp != CardCamp.Player)
                return;

            if (isResolvingTurn || battleEnded)
                return;

            if (currentEnergy < card.Cost)
            {
                AddBattleLogEntry($"费用不足：{card.CardName} 需要 {card.Cost} 点费用。");
                RefreshHud();
                return;
            }

            if (castingSkillCard == card)
            {
                CancelSkillCast();
                return;
            }

            CancelSkillCast();
            castingSkillCard = card;
            castingSkillCardRect = sourceRect;
            currentPreviewTarget = default;

            AddSkillCardHighlight(sourceRect);

            if (GetSkillTargetRequirement(card) == SkillTargetRequirement.None)
            {
                ResolveSkillCard(card, default);
                return;
            }

            CreateSkillArrow(sourceRect);
            UpdateSkillArrow();
        }

        private void CancelSkillCast()
        {
            castingSkillCard = null;
            castingSkillCardRect = null;
            currentPreviewTarget = default;
            ClearAllSkillTargetHighlights();

            if (castingSkillCardHighlight != null)
            {
                if (Application.isPlaying)
                    Destroy(castingSkillCardHighlight.gameObject);
                else
                    DestroyImmediate(castingSkillCardHighlight.gameObject);
                castingSkillCardHighlight = null;
            }

            if (castingArrow != null)
            {
                if (Application.isPlaying)
                    Destroy(castingArrow.gameObject);
                else
                    DestroyImmediate(castingArrow.gameObject);
                castingArrow = null;
            }
        }

        internal void RegisterSkillTargetHandler(SkillTargetHandler handler)
        {
            if (handler != null && !skillTargetHandlers.Contains(handler))
                skillTargetHandlers.Add(handler);
        }

        internal void UnregisterSkillTargetHandler(SkillTargetHandler handler)
        {
            skillTargetHandlers.Remove(handler);
        }

        internal void PreviewSkillTarget(SkillTarget rawTarget)
        {
            if (!IsCastingSkill)
                return;

            var previewTarget = NormalizeSkillTarget(rawTarget);
            var valid = IsValidSkillTarget(previewTarget);
            currentPreviewTarget = previewTarget;
            SetSkillArrowValid(valid);
            SetSkillTargetHighlights(previewTarget, valid);
        }

        internal void ClearSkillTargetPreview(SkillTarget rawTarget)
        {
            if (!IsCastingSkill)
                return;

            var previewTarget = NormalizeSkillTarget(rawTarget);
            if (IsSameSkillTarget(previewTarget, currentPreviewTarget))
            {
                currentPreviewTarget = default;
                ClearAllSkillTargetHighlights();
                SetSkillArrowValid(false);
            }
        }

        internal void TryConfirmSkillTarget(SkillTarget rawTarget)
        {
            if (!IsCastingSkill)
                return;

            var target = NormalizeSkillTarget(rawTarget);
            if (!IsValidSkillTarget(target))
            {
                AddBattleLogEntry($"{castingSkillCard.CardName} 不能对该目标释放。");
                SetSkillTargetHighlights(target, false);
                SetSkillArrowValid(false);
                return;
            }

            ResolveSkillCard(castingSkillCard, target);
        }

        private void ResetSkillTargetHandlers()
        {
            skillTargetHandlers.Clear();
            currentPreviewTarget = default;
        }

        private void AddSkillCardHighlight(RectTransform sourceRect)
        {
            if (sourceRect == null)
                return;

            var highlightObject = new GameObject("Selected Skill Card Highlight", typeof(RectTransform), typeof(Image), typeof(Outline));
            highlightObject.transform.SetParent(sourceRect, false);
            highlightObject.transform.SetAsLastSibling();

            var rect = highlightObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-4f, -4f);
            rect.offsetMax = new Vector2(4f, 4f);

            castingSkillCardHighlight = highlightObject.GetComponent<Image>();
            castingSkillCardHighlight.color = new Color(1f, 0.88f, 0.3f, 0.16f);
            castingSkillCardHighlight.raycastTarget = false;

            var outline = highlightObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.28f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        private void CreateSkillArrow(RectTransform sourceRect)
        {
            var canvas = sourceRect == null ? GetComponentInChildren<Canvas>() : sourceRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var arrowObject = new GameObject("Skill Casting Arrow", typeof(RectTransform), typeof(DashedArrowGraphic));
            arrowObject.transform.SetParent(canvas.transform, false);
            arrowObject.transform.SetAsLastSibling();

            var rect = arrowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var canvasRect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = canvasRect == null ? referenceResolution : canvasRect.rect.size;

            castingArrow = arrowObject.GetComponent<DashedArrowGraphic>();
            castingArrow.raycastTarget = false;
            castingArrow.color = new Color(0.18f, 1f, 0.38f, 0.72f);
        }

        private void UpdateSkillArrow()
        {
            if (castingArrow == null || castingSkillCardRect == null)
                return;

            var canvas = castingArrow.GetComponentInParent<Canvas>();
            var arrowRect = castingArrow.GetComponent<RectTransform>();
            if (canvas == null || arrowRect == null)
                return;

            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var startScreen = RectTransformUtility.WorldToScreenPoint(camera, castingSkillCardRect.TransformPoint(castingSkillCardRect.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(arrowRect, startScreen, camera, out var start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(arrowRect, Input.mousePosition, camera, out var end);
            if ((end - start).sqrMagnitude < 3600f)
                end = start + new Vector2(0f, 60f);

            castingArrow.SetPoints(start, end);
        }

        private void SetSkillArrowValid(bool valid)
        {
            if (castingArrow == null)
                return;

            castingArrow.color = valid
                ? new Color(0.2f, 1f, 0.38f, 0.82f)
                : new Color(1f, 0.33f, 0.22f, 0.95f);
        }

        private void SetSkillTargetHighlights(SkillTarget target, bool valid)
        {
            foreach (var handler in skillTargetHandlers.ToArray())
            {
                if (handler == null)
                    continue;

                handler.SetSkillHighlight(IsSkillTargetPreviewMatch(handler.Target, target), valid);
            }
        }

        private void ClearAllSkillTargetHighlights()
        {
            foreach (var handler in skillTargetHandlers.ToArray())
            {
                if (handler != null)
                    handler.SetSkillHighlight(false, false);
            }
        }

        private SkillTarget NormalizeSkillTarget(SkillTarget rawTarget)
        {
            var requirement = GetSkillTargetRequirement(castingSkillCard);
            switch (requirement)
            {
                case SkillTargetRequirement.AllyRow:
                case SkillTargetRequirement.EnemyRow:
                case SkillTargetRequirement.AnyRow:
                    return SkillTarget.ForRow(rawTarget.PlayerSide, rawTarget.Row);
                default:
                    return rawTarget;
            }
        }

        private bool IsValidSkillTarget(SkillTarget target)
        {
            if (castingSkillCard == null || target.Kind == SkillTargetKind.None)
                return false;

            var requirement = GetSkillTargetRequirement(castingSkillCard);
            switch (requirement)
            {
                case SkillTargetRequirement.None:
                    return true;
                case SkillTargetRequirement.AllyUnit:
                    return target.Kind == SkillTargetKind.Unit && target.PlayerSide && IsAliveTargetUnit(target);
                case SkillTargetRequirement.EnemyUnit:
                    return target.Kind == SkillTargetKind.Unit && !target.PlayerSide && IsAliveTargetUnit(target);
                case SkillTargetRequirement.AnyUnit:
                    return target.Kind == SkillTargetKind.Unit && IsAliveTargetUnit(target);
                case SkillTargetRequirement.AllyRow:
                    return target.Kind == SkillTargetKind.Row && target.PlayerSide && HasAliveUnitsInRow(playerUnits, target.Row);
                case SkillTargetRequirement.EnemyRow:
                    return target.Kind == SkillTargetKind.Row && !target.PlayerSide && HasAliveUnitsInRow(enemyUnits, target.Row);
                case SkillTargetRequirement.AnyRow:
                    return target.Kind == SkillTargetKind.Row && HasAliveUnitsInRow(target.PlayerSide ? playerUnits : enemyUnits, target.Row);
                default:
                    return false;
            }
        }

        private SkillTargetRequirement GetSkillTargetRequirement(CardRecord card)
        {
            if (card == null || card.CardType != CardType.Skill)
                return SkillTargetRequirement.None;

            foreach (var effect in card.Effects)
            {
                if (effect == null)
                    continue;

                if (effect.EffectType == "DrawCards" || effect.TargetRule == "NoTarget")
                    return SkillTargetRequirement.None;

                var targetRule = effect.TargetRule ?? string.Empty;
                if (targetRule.Contains("Row") || targetRule.Contains("SameRow"))
                {
                    if (targetRule.StartsWith("Ally"))
                        return SkillTargetRequirement.AllyRow;
                    if (targetRule.StartsWith("Enemy"))
                        return SkillTargetRequirement.EnemyRow;
                    return SkillTargetRequirement.AnyRow;
                }

                if (targetRule.StartsWith("Ally") || targetRule == "Self")
                    return SkillTargetRequirement.AllyUnit;
                if (targetRule.StartsWith("Enemy") || targetRule == "CurrentTarget")
                    return SkillTargetRequirement.EnemyUnit;
                if (targetRule.StartsWith("Any"))
                    return SkillTargetRequirement.AnyUnit;
            }

            return SkillTargetRequirement.AnyUnit;
        }

        private void ResolveSkillCard(CardRecord card, SkillTarget target)
        {
            if (card == null || !hand.Remove(card))
            {
                CancelSkillCast();
                return;
            }

            currentEnergy -= card.Cost;
            discardPile.Add(card);

            var logLines = new List<string> { $"释放「{card.CardName}」，消耗 {card.Cost} 点费用。" };
            ResolveSkillCardEffects(card, target, logLines);
            RefreshUnitHealthViews();
            StartCoroutine(FinishSkillCardRoutine(logLines));
        }

        private IEnumerator FinishSkillCardRoutine(List<string> logLines)
        {
            isResolvingTurn = true;
            yield return StartCoroutine(ResolveDeathsAndAdvanceRoutine(logLines));
            CancelSkillCast();
            CommitTurnLog(logLines);
            isResolvingTurn = false;
            RefreshTacticalViews();
            RefreshHud();
        }

        private void ResolveSkillCardEffects(CardRecord card, SkillTarget target, List<string> logLines)
        {
            battleEffectResolver.ResolveSkillCardEffects(card, ToBattleTarget(target), logLines);
        }

        private List<BattleUnit> ResolveSkillTargetUnits(SkillTarget target)
        {
            return battleEffectResolver.ResolveSkillTargetUnits(ToBattleTarget(target));
        }

        private static BattleTarget ToBattleTarget(SkillTarget target)
        {
            switch (target.Kind)
            {
                case SkillTargetKind.Unit:
                    return BattleTarget.ForUnit(target.PlayerSide, target.Row, target.Unit);
                case SkillTargetKind.Row:
                    return BattleTarget.ForRow(target.PlayerSide, target.Row);
                default:
                    return default;
            }
        }

        private static bool IsAliveTargetUnit(SkillTarget target)
        {
            return target.Unit != null && !target.Unit.IsDead;
        }

        private bool HasAliveUnitsInRow(List<BattleUnit> units, int row)
        {
            return battleFormation.HasAliveUnitsInRow(units, row);
        }

        private static bool IsSameSkillTarget(SkillTarget a, SkillTarget b)
        {
            if (a.Kind != b.Kind || a.PlayerSide != b.PlayerSide || a.Row != b.Row)
                return false;

            return a.Kind != SkillTargetKind.Unit || a.Unit == b.Unit;
        }

        private static bool IsSkillTargetPreviewMatch(SkillTarget handlerTarget, SkillTarget previewTarget)
        {
            if (previewTarget.Kind == SkillTargetKind.Row)
                return handlerTarget.PlayerSide == previewTarget.PlayerSide && handlerTarget.Row == previewTarget.Row;

            if (previewTarget.Kind == SkillTargetKind.Unit)
                return handlerTarget.Kind == SkillTargetKind.Unit && handlerTarget.Unit == previewTarget.Unit;

            return false;
        }

        private void CreateSkillRowTargetZone(Transform parent, int row, bool playerSide)
        {
            var min = Vector2.one;
            var max = Vector2.zero;
            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var slotIndex = GetSlotIndex(row, column);
                var slotMin = GetSlotAnchorMin(slotIndex, playerSide);
                var slotMax = GetSlotAnchorMax(slotIndex, playerSide);
                min = Vector2.Min(min, slotMin);
                max = Vector2.Max(max, slotMax);
            }

            min += new Vector2(-0.02f, -0.04f);
            max += new Vector2(0.02f, 0.04f);

            var zone = CreateImage(parent, $"Skill Row Target R{row + 1}", Color.clear);
            zone.raycastTarget = true;
            SetRect(zone.rectTransform, min, max, Vector2.zero, Vector2.zero);

            var handler = zone.gameObject.AddComponent<SkillTargetHandler>();
            handler.Initialize(this, SkillTarget.ForRow(playerSide, row));
        }
    }
}
