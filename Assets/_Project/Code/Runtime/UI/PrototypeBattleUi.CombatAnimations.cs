using System.Collections;
using DG.Tweening;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private const float AttackLungeDistance = 42f;
        private const float AttackLungeDuration = 0.12f;
        private const float AttackReturnDuration = 0.14f;
        private const float HitShakeDuration = 0.22f;
        private const float DamageNumberDuration = 0.48f;

        private IEnumerator PlayAttackMotion(BattleUnit attacker, BattleUnit target, bool targetPlayerBase)
        {
            if (attacker == null || !unitViews.TryGetValue(attacker.RuntimeId, out var attackerRect) || attackerRect == null)
                yield break;

            var targetRect = GetTargetRect(target, targetPlayerBase);
            if (targetRect == null && !targetPlayerBase)
                yield break;

            attackerRect.DOKill();
            attackerRect.SetAsLastSibling();

            var start = attackerRect.localPosition;
            var direction = GetAttackDirection(attacker, attackerRect, targetRect);
            var lungeOffset = new Vector3(direction * AttackLungeDistance, 0f, 0f);

            var sequence = DOTween.Sequence();
            sequence.Append(attackerRect.DOLocalMove(start + lungeOffset, AttackLungeDuration).SetEase(Ease.OutQuad));
            sequence.Append(attackerRect.DOLocalMove(start, AttackReturnDuration).SetEase(Ease.OutBack));
            yield return sequence.WaitForCompletion();

            if (attackerRect != null)
                attackerRect.localPosition = start;
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
            sequence.Join(targetRect.DOShakeAnchorPos(HitShakeDuration, new Vector2(14f, 5f), 18, 75f, false, true));
            sequence.Join(targetRect.DOPunchScale(Vector3.one * 0.08f, HitShakeDuration, 6, 0.7f));
            if (flash != null)
                sequence.Join(flash.DOFade(0f, HitShakeDuration).SetEase(Ease.OutQuad));

            if (hpDamage > 0)
                PlayDamageNumber(targetRect, hpDamage);

            yield return sequence.WaitForCompletion();

            if (targetRect != null)
            {
                targetRect.anchoredPosition = originalAnchoredPosition;
                targetRect.localScale = originalScale;
            }

            if (flash != null)
                Destroy(flash.gameObject);
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
            var sequence = DOTween.Sequence();
            sequence.Join(rect.DOAnchorPos(new Vector2(0f, 34f), DamageNumberDuration).SetEase(Ease.OutCubic));
            sequence.Join(group.DOFade(0f, DamageNumberDuration).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                if (text != null)
                    Destroy(text.gameObject);
            });
        }
    }
}
