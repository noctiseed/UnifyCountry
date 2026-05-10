using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private readonly struct CombatHealthSnapshot
        {
            public CombatHealthSnapshot(Dictionary<int, int> unitHp, int playerBaseHp)
            {
                UnitHp = unitHp;
                PlayerBaseHp = playerBaseHp;
            }

            public Dictionary<int, int> UnitHp { get; }
            public int PlayerBaseHp { get; }
        }

        private const float AttackLungeDistance = 42f;
        private const float AttackTargetGap = 64f;
        private const float AttackLungeDuration = 0.2f;
        private const float AttackReturnDuration = 0.24f;
        private const float HitShakeDuration = 0.32f;
        private const float DamageNumberDuration = 0.72f;

        private IEnumerator PlayAttackMotion(BattleUnit attacker, BattleUnit target, bool targetPlayerBase)
        {
            if (attacker == null || !unitViews.TryGetValue(attacker.RuntimeId, out var attackerRect) || attackerRect == null)
                yield break;

            var targetRect = GetTargetRect(target, targetPlayerBase);
            if (targetRect == null && !targetPlayerBase)
                yield break;

            attackerRect.DOKill();
            var originalParent = attackerRect.parent;
            var originalSiblingIndex = attackerRect.GetSiblingIndex();
            var originalAnchorMin = attackerRect.anchorMin;
            var originalAnchorMax = attackerRect.anchorMax;
            var originalOffsetMin = attackerRect.offsetMin;
            var originalOffsetMax = attackerRect.offsetMax;
            var originalAnchoredPosition = attackerRect.anchoredPosition;
            var originalLocalPosition = attackerRect.localPosition;
            var originalScale = attackerRect.localScale;

            var animationLayer = GetCombatAnimationLayer();
            var useAnimationLayer = !targetPlayerBase && animationLayer != null;
            if (useAnimationLayer)
            {
                animationLayer.SetAsLastSibling();
                attackerRect.SetParent(animationLayer, true);
            }
            else
            {
                attackerRect.SetAsLastSibling();
            }

            var start = attackerRect.localPosition;
            var direction = GetAttackDirection(attacker, attackerRect, targetRect);
            var attackPosition = targetPlayerBase
                ? start + new Vector3(direction * AttackLungeDistance, 0f, 0f)
                : GetAttackPositionInFrontOfTarget(attackerRect, targetRect, direction, start);

            var sequence = DOTween.Sequence();
            sequence.Append(attackerRect.DOLocalMove(attackPosition, AttackLungeDuration).SetEase(Ease.OutQuad).SetTarget(attackerRect.gameObject).SetLink(attackerRect.gameObject, LinkBehaviour.KillOnDestroy));
            sequence.Append(attackerRect.DOLocalMove(start, AttackReturnDuration).SetEase(Ease.OutBack).SetTarget(attackerRect.gameObject).SetLink(attackerRect.gameObject, LinkBehaviour.KillOnDestroy));
            sequence.SetLink(attackerRect.gameObject, LinkBehaviour.KillOnDestroy);
            yield return sequence.WaitForCompletion();

            if (attackerRect != null)
            {
                if (useAnimationLayer && originalParent != null)
                {
                    attackerRect.SetParent(originalParent, false);
                    attackerRect.SetSiblingIndex(originalSiblingIndex);
                    attackerRect.anchorMin = originalAnchorMin;
                    attackerRect.anchorMax = originalAnchorMax;
                    attackerRect.offsetMin = originalOffsetMin;
                    attackerRect.offsetMax = originalOffsetMax;
                    attackerRect.anchoredPosition = originalAnchoredPosition;
                    attackerRect.localPosition = originalLocalPosition;
                    attackerRect.localScale = originalScale;
                }
                else
                {
                    attackerRect.localPosition = originalLocalPosition;
                }
            }
        }

        private IEnumerator PlayHitMotion(BattleUnit target, bool targetPlayerBase, int hpDamage)
        {
            var targetRect = GetTargetRect(target, targetPlayerBase);
            if (targetRect == null)
                yield break;

            targetRect.DOKill();
            targetRect.SetAsLastSibling();

            var originalAnchoredPosition = targetRect.anchoredPosition;
            var originalScale = targetRect.localScale;
            var hitColor = hpDamage > 0 ? new Color(1f, 0.24f, 0.16f, 0.56f) : new Color(1f, 0.95f, 0.68f, 0.42f);
            var flash = CreateHitFlash(targetRect, hitColor);

            var sequence = DOTween.Sequence();
            sequence.Join(targetRect.DOShakeAnchorPos(HitShakeDuration, new Vector2(14f, 5f), 18, 75f, false, true).SetTarget(targetRect.gameObject).SetLink(targetRect.gameObject, LinkBehaviour.KillOnDestroy));
            sequence.Join(targetRect.DOPunchScale(Vector3.one * 0.08f, HitShakeDuration, 6, 0.7f).SetTarget(targetRect.gameObject).SetLink(targetRect.gameObject, LinkBehaviour.KillOnDestroy));
            if (flash != null)
                sequence.Join(flash.DOFade(0f, HitShakeDuration).SetEase(Ease.OutQuad).SetTarget(flash.gameObject).SetLink(flash.gameObject, LinkBehaviour.KillOnDestroy));

            sequence.SetLink(targetRect.gameObject, LinkBehaviour.KillOnDestroy);

            if (hpDamage > 0)
                PlayDamageNumber(targetRect, hpDamage);

            yield return sequence.WaitForCompletion();

            if (targetRect != null)
            {
                targetRect.anchoredPosition = originalAnchoredPosition;
                targetRect.localScale = originalScale;
            }

            if (flash != null)
            {
                DOTween.Kill(flash.gameObject);
                Destroy(flash.gameObject);
            }
        }

        private CombatHealthSnapshot CaptureCombatHealthSnapshot()
        {
            var unitHp = new Dictionary<int, int>();
            CaptureCombatHealthSnapshot(playerUnits, unitHp);
            CaptureCombatHealthSnapshot(enemyUnits, unitHp);
            return new CombatHealthSnapshot(unitHp, playerBaseHp);
        }

        private static void CaptureCombatHealthSnapshot(List<BattleUnit> units, Dictionary<int, int> unitHp)
        {
            if (units == null)
                return;

            foreach (var unit in units)
            {
                if (unit != null)
                    unitHp[unit.RuntimeId] = unit.CurrentHp;
            }
        }

        private IEnumerator PlayDamageReactions(CombatHealthSnapshot healthBefore, BattleUnit primaryTarget, bool primaryTargetPlayerBase)
        {
            var playedRuntimeIds = new HashSet<int>();
            var primaryHpDamage = GetHpDamage(healthBefore, primaryTarget);
            if (primaryTarget != null && primaryHpDamage > 0)
            {
                playedRuntimeIds.Add(primaryTarget.RuntimeId);
                yield return StartCoroutine(PlayHitMotion(primaryTarget, false, primaryHpDamage));
            }
            else if (primaryTargetPlayerBase && healthBefore.PlayerBaseHp > playerBaseHp)
            {
                yield return StartCoroutine(PlayHitMotion(null, true, healthBefore.PlayerBaseHp - playerBaseHp));
            }

            foreach (var unit in playerUnits)
            {
                if (unit == null || playedRuntimeIds.Contains(unit.RuntimeId))
                    continue;

                var hpDamage = GetHpDamage(healthBefore, unit);
                if (hpDamage <= 0)
                    continue;

                playedRuntimeIds.Add(unit.RuntimeId);
                yield return StartCoroutine(PlayHitMotion(unit, false, hpDamage));
            }

            foreach (var unit in enemyUnits)
            {
                if (unit == null || playedRuntimeIds.Contains(unit.RuntimeId))
                    continue;

                var hpDamage = GetHpDamage(healthBefore, unit);
                if (hpDamage <= 0)
                    continue;

                playedRuntimeIds.Add(unit.RuntimeId);
                yield return StartCoroutine(PlayHitMotion(unit, false, hpDamage));
            }
        }

        private static int GetHpDamage(CombatHealthSnapshot healthBefore, BattleUnit unit)
        {
            if (unit == null || !healthBefore.UnitHp.TryGetValue(unit.RuntimeId, out var hpBefore))
                return 0;

            return Mathf.Max(0, hpBefore - unit.CurrentHp);
        }

        private RectTransform GetTargetRect(BattleUnit target, bool targetPlayerBase)
        {
            if (target != null && unitViews.TryGetValue(target.RuntimeId, out var rect))
                return rect;

            return targetPlayerBase ? playerBaseView : null;
        }

        private static float GetAttackDirection(BattleUnit attacker, RectTransform attackerRect, RectTransform targetRect)
        {
            if (targetRect != null)
            {
                var worldDelta = targetRect.position.x - attackerRect.position.x;
                if (Mathf.Abs(worldDelta) > 0.01f)
                    return Mathf.Sign(worldDelta);
            }

            return attacker.Camp == CardCamp.Player ? 1f : -1f;
        }

        private static Vector3 GetAttackPositionInFrontOfTarget(RectTransform attackerRect, RectTransform targetRect, float direction, Vector3 fallbackPosition)
        {
            if (targetRect == null)
                return fallbackPosition + new Vector3(direction * AttackLungeDistance, 0f, 0f);

            var targetWorldPosition = targetRect.position;
            var targetLocalPosition = attackerRect.parent.InverseTransformPoint(targetWorldPosition);
            var x = targetLocalPosition.x - direction * AttackTargetGap;
            return new Vector3(x, fallbackPosition.y, fallbackPosition.z);
        }

        private RectTransform GetCombatAnimationLayer()
        {
            if (combatAnimationLayer != null)
                return combatAnimationLayer;

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
                return null;

            var layerObject = new GameObject("Combat Animation Layer", typeof(RectTransform), typeof(CanvasGroup));
            layerObject.transform.SetParent(canvas.transform, false);
            combatAnimationLayer = layerObject.GetComponent<RectTransform>();
            SetRect(combatAnimationLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var group = layerObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            return combatAnimationLayer;
        }

        private Image CreateHitFlash(RectTransform targetRect, Color color)
        {
            var flash = CreateImage(targetRect, "Hit Flash", color);
            flash.raycastTarget = false;
            SetRect(flash.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            flash.transform.SetAsLastSibling();
            return flash;
        }

        private void PlayDamageNumber(RectTransform targetRect, int hpDamage)
        {
            var text = CreateText(targetRect, $"-{hpDamage}", 30, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.72f));
            text.raycastTarget = false;
            text.fontStyle = FontStyle.Bold;

            var rect = text.rectTransform;
            SetRect(rect, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(82f, 42f));
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0.28f, 0.03f, 0.02f, 0.92f);

            var group = text.gameObject.AddComponent<CanvasGroup>();
            text.gameObject.AddComponent<DOTweenTargetKiller>();
            var sequence = DOTween.Sequence();
            sequence.Join(rect.DOAnchorPos(new Vector2(0f, 34f), DamageNumberDuration).SetEase(Ease.OutCubic).SetTarget(text.gameObject).SetLink(text.gameObject, LinkBehaviour.KillOnDestroy));
            sequence.Join(group.DOFade(0f, DamageNumberDuration).SetEase(Ease.InQuad).SetTarget(text.gameObject).SetLink(text.gameObject, LinkBehaviour.KillOnDestroy));
            sequence.SetTarget(text.gameObject);
            sequence.SetLink(text.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.OnComplete(() =>
            {
                if (text != null)
                {
                    DOTween.Kill(text.gameObject);
                    Destroy(text.gameObject);
                }
            });
        }
    }

    internal sealed class DOTweenTargetKiller : MonoBehaviour
    {
        private void OnDestroy()
        {
            DOTween.Kill(gameObject);
        }
    }
}
