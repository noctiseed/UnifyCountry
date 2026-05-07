using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private RectTransform CreateUnitToken(Transform parent, BattleUnit unit, bool compact)
        {
            var hasSprite = TryGetUnitSprite(unit, out var unitSprite);
            var root = CreateImage(parent, unit.Name, hasSprite ? new Color(1f, 1f, 1f, 0f) : unit.Camp == CardCamp.Enemy ? enemyCardColor : heroCardColor);
            if (!hasSprite)
                root.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.16f, 0.1f);

            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.45f);
            shadow.effectDistance = new Vector2(6f, -8f);

            if (hasSprite)
            {
                var spriteImage = CreateUnitSpriteImage(root.transform, ShouldMirrorEnemyUnitSprite(unit));
                spriteImage.sprite = unitSprite;
                spriteImage.preserveAspect = true;
                SetRect(spriteImage.rectTransform, new Vector2(-0.12f, 0.08f), new Vector2(1.12f, 1.14f), Vector2.zero, Vector2.zero);
                spriteImage.raycastTarget = false;
            }

            var textColor = hasSprite ? Color.white : new Color(0.12f, 0.08f, 0.05f);
            var name = CreateText(root.transform, unit.Name, compact ? 15 : 16, TextAnchor.MiddleCenter, textColor);
            SetRect(name.rectTransform, hasSprite ? new Vector2(-0.06f, 0.96f) : new Vector2(0.02f, 0.92f), hasSprite ? new Vector2(1.06f, 1.12f) : new Vector2(0.98f, 1.08f), Vector2.zero, Vector2.zero);
            if (hasSprite)
                name.gameObject.AddComponent<Outline>().effectColor = new Color(0.12f, 0.06f, 0.04f, 0.9f);

            CreateUnitAttackIcon(root.transform, unit, hasSprite, compact);
            CreateUnitDefenseBuffIcons(root.transform, unit, hasSprite);

            var statusText = GetUnitStatusText(unit);
            if (!string.IsNullOrEmpty(statusText))
            {
                var status = CreateText(root.transform, statusText, compact ? 12 : 13, TextAnchor.MiddleCenter, hasSprite ? Color.white : new Color(0.08f, 0.2f, 0.34f));
                var defenseBuffIconCount = GetVisibleBuffIconCount(unit);
                var statusMinX = defenseBuffIconCount > 0 ? 0.32f + defenseBuffIconCount * 0.26f : hasSprite ? 0.3f : 0.32f;
                SetRect(status.rectTransform, hasSprite ? new Vector2(statusMinX, 0.2f) : new Vector2(statusMinX, 0.22f), hasSprite ? new Vector2(1.08f, 0.34f) : new Vector2(1f, 0.36f), Vector2.zero, Vector2.zero);
                if (hasSprite)
                    status.gameObject.AddComponent<Outline>().effectColor = new Color(0.04f, 0.1f, 0.18f, 0.9f);
            }

            CreateHealthBar(root.transform, unit.CurrentHp, unit.MaxHp, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.18f));

            return root.rectTransform;
        }

        private void CreateUnitAttackIcon(Transform parent, BattleUnit unit, bool hasSprite, bool compact)
        {
            var hasAttackSprite = TryGetAttackIconSprite(out var attackSprite);
            var icon = CreateImage(parent, "Attack Icon", hasAttackSprite ? Color.clear : hasSprite ? new Color(0.12f, 0.06f, 0.04f, 0.82f) : new Color(0.45f, 0.09f, 0.06f, 0.92f));
            icon.raycastTarget = true;
            SetRect(icon.rectTransform, new Vector2(0.06f, 0.2f), new Vector2(0.28f, 0.42f), Vector2.zero, Vector2.zero);

            if (hasAttackSprite)
            {
                var spriteImage = CreateImage(icon.transform, "Attack Icon Sprite", Color.white);
                spriteImage.sprite = attackSprite;
                spriteImage.preserveAspect = true;
                spriteImage.raycastTarget = false;
                SetRect(spriteImage.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            }
            else
            {
                icon.sprite = GetRoundedButtonSprite();
                icon.type = Image.Type.Sliced;

                var outline = icon.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.08f, 0.03f, 0.02f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                CreateSwordIconShape(icon.transform);
            }

            var attackDelta = unit.Attack - unit.BaseAttack;
            var deltaText = attackDelta == 0 ? string.Empty : attackDelta > 0 ? $"（基础 {unit.BaseAttack}，+{attackDelta}）" : $"（基础 {unit.BaseAttack}，{attackDelta}）";
            var tooltip = icon.gameObject.AddComponent<PrototypeTooltipHandler>();
            tooltip.Initialize($"攻击力 {unit.Attack}{deltaText}", uiFont);
        }

        private static bool ShouldMirrorEnemyUnitSprite(BattleUnit unit)
        {
            return unit != null && unit.Camp == CardCamp.Enemy;
        }

        private UnitSpriteImage CreateUnitSpriteImage(Transform parent, bool mirrorX)
        {
            var gameObject = new GameObject("Unit Sprite", typeof(RectTransform), typeof(UnitSpriteImage));
            gameObject.transform.SetParent(parent, false);

            var image = gameObject.GetComponent<UnitSpriteImage>();
            image.color = Color.white;
            image.MirrorX = mirrorX;
            return image;
        }

        private void CreateUnitDefenseBuffIcons(Transform parent, BattleUnit unit, bool hasSprite)
        {
            var iconIndex = 0;
            if (unit.Shield > 0)
            {
                var hasShieldSprite = TryGetShieldIconSprite(out var shieldSprite);
                CreateUnitBuffIcon(
                    parent,
                    iconIndex++,
                    hasShieldSprite ? Color.clear : hasSprite ? new Color(0.04f, 0.2f, 0.32f, 0.86f) : new Color(0.08f, 0.35f, 0.52f, 0.92f),
                    iconParent =>
                    {
                        if (hasShieldSprite)
                            CreateBuffIconSprite(iconParent, shieldSprite);
                        else
                            CreateShieldIconShape(iconParent);
                    },
                    !hasShieldSprite,
                    $"护盾 {unit.Shield} 层：每层抵挡一次敌方攻击");
            }

            if (unit.AttackImmunityCharges > 0)
            {
                CreateUnitBuffIcon(
                    parent,
                    iconIndex,
                    hasSprite ? new Color(0.17f, 0.12f, 0.34f, 0.86f) : new Color(0.32f, 0.2f, 0.58f, 0.92f),
                    CreateImmunityIconShape,
                    true,
                    $"免疫 {unit.AttackImmunityCharges} 次敌方攻击");
            }

            if (unit.Revival > 0)
            {
                var hasRegenerationSprite = TryGetRegenerationIconSprite(out var regenerationSprite);
                CreateUnitBuffIcon(
                    parent,
                    iconIndex,
                    hasRegenerationSprite ? Color.clear : hasSprite ? new Color(0.08f, 0.34f, 0.16f, 0.86f) : new Color(0.1f, 0.5f, 0.22f, 0.92f),
                    iconParent =>
                    {
                        if (hasRegenerationSprite)
                            CreateBuffIconSprite(iconParent, regenerationSprite);
                        else
                            CreateBuffTextIcon(iconParent, "复", new Color(0.78f, 1f, 0.72f));
                    },
                    !hasRegenerationSprite,
                    $"复苏 {unit.Revival}：回合最后结算时恢复等同于当前复苏层数的生命，然后复苏 -1");
            }
        }

        private static int GetVisibleBuffIconCount(BattleUnit unit)
        {
            return (unit.Shield > 0 ? 1 : 0)
                + (unit.AttackImmunityCharges > 0 ? 1 : 0)
                + (unit.Revival > 0 ? 1 : 0);
        }

        private void CreateUnitBuffIcon(Transform parent, int iconIndex, Color backgroundColor, System.Action<Transform> createShape, bool useBackgroundFrame, string tooltipText)
        {
            var icon = CreateImage(parent, "Buff Icon", backgroundColor);
            icon.raycastTarget = true;

            var minX = 0.32f + iconIndex * 0.26f;
            SetRect(icon.rectTransform, new Vector2(minX, 0.2f), new Vector2(minX + 0.22f, 0.42f), Vector2.zero, Vector2.zero);

            if (useBackgroundFrame)
            {
                icon.sprite = GetRoundedButtonSprite();
                icon.type = Image.Type.Sliced;

                var outline = icon.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.04f, 0.04f, 0.06f, 0.75f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            createShape(icon.transform);

            var tooltip = icon.gameObject.AddComponent<PrototypeTooltipHandler>();
            tooltip.Initialize(tooltipText, uiFont);
        }

        private void CreateBuffIconSprite(Transform parent, Sprite sprite)
        {
            var spriteImage = CreateImage(parent, "Buff Icon Sprite", Color.white);
            spriteImage.sprite = sprite;
            spriteImage.preserveAspect = true;
            spriteImage.raycastTarget = false;
            SetRect(spriteImage.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);
        }

        private void CreateBuffTextIcon(Transform parent, string value, Color color)
        {
            var text = CreateText(parent, value, 18, TextAnchor.MiddleCenter, color);
            text.raycastTarget = false;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void CreateShieldIconShape(Transform parent)
        {
            var top = CreateImage(parent, "Shield Top", new Color(0.74f, 0.92f, 1f));
            top.raycastTarget = false;
            top.rectTransform.anchorMin = new Vector2(0.24f, 0.42f);
            top.rectTransform.anchorMax = new Vector2(0.76f, 0.78f);
            top.rectTransform.offsetMin = Vector2.zero;
            top.rectTransform.offsetMax = Vector2.zero;

            var bottom = CreateImage(parent, "Shield Bottom", new Color(0.52f, 0.78f, 0.98f));
            bottom.raycastTarget = false;
            bottom.rectTransform.anchorMin = new Vector2(0.34f, 0.16f);
            bottom.rectTransform.anchorMax = new Vector2(0.66f, 0.5f);
            bottom.rectTransform.offsetMin = Vector2.zero;
            bottom.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateImmunityIconShape(Transform parent)
        {
            var vertical = CreateImage(parent, "Immunity Spark Vertical", new Color(0.9f, 0.86f, 1f));
            vertical.raycastTarget = false;
            vertical.rectTransform.anchorMin = new Vector2(0.46f, 0.18f);
            vertical.rectTransform.anchorMax = new Vector2(0.54f, 0.82f);
            vertical.rectTransform.offsetMin = Vector2.zero;
            vertical.rectTransform.offsetMax = Vector2.zero;

            var horizontal = CreateImage(parent, "Immunity Spark Horizontal", new Color(0.9f, 0.86f, 1f));
            horizontal.raycastTarget = false;
            horizontal.rectTransform.anchorMin = new Vector2(0.18f, 0.46f);
            horizontal.rectTransform.anchorMax = new Vector2(0.82f, 0.54f);
            horizontal.rectTransform.offsetMin = Vector2.zero;
            horizontal.rectTransform.offsetMax = Vector2.zero;

            var core = CreateImage(parent, "Immunity Spark Core", new Color(1f, 0.96f, 0.64f));
            core.raycastTarget = false;
            core.rectTransform.anchorMin = new Vector2(0.38f, 0.38f);
            core.rectTransform.anchorMax = new Vector2(0.62f, 0.62f);
            core.rectTransform.offsetMin = Vector2.zero;
            core.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateSwordIconShape(Transform parent)
        {
            var blade = CreateImage(parent, "Sword Blade", new Color(0.96f, 0.92f, 0.78f));
            blade.raycastTarget = false;
            blade.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            blade.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            blade.rectTransform.pivot = new Vector2(0.5f, 0f);
            blade.rectTransform.anchoredPosition = new Vector2(1f, -3f);
            blade.rectTransform.sizeDelta = new Vector2(5f, 25f);
            blade.rectTransform.localEulerAngles = new Vector3(0f, 0f, 42f);

            var guard = CreateImage(parent, "Sword Guard", new Color(0.98f, 0.72f, 0.24f));
            guard.raycastTarget = false;
            guard.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            guard.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            guard.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            guard.rectTransform.anchoredPosition = new Vector2(-5f, -8f);
            guard.rectTransform.sizeDelta = new Vector2(18f, 4f);
            guard.rectTransform.localEulerAngles = new Vector3(0f, 0f, 42f);

            var grip = CreateImage(parent, "Sword Grip", new Color(0.25f, 0.13f, 0.08f));
            grip.raycastTarget = false;
            grip.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            grip.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            grip.rectTransform.pivot = new Vector2(0.5f, 1f);
            grip.rectTransform.anchoredPosition = new Vector2(-9f, -11f);
            grip.rectTransform.sizeDelta = new Vector2(5f, 10f);
            grip.rectTransform.localEulerAngles = new Vector3(0f, 0f, 42f);
        }

        private static string GetUnitStatusText(BattleUnit unit)
        {
            var statuses = new List<string>();
            foreach (var effect in unit.Effects)
            {
                if (effect.Timing == "BeforeDamaged" && effect.EffectType == "DamageCap")
                    statuses.Add($"限{effect.Value}");
            }

            return string.Join("  ", statuses);
        }

        private void CreateHealthBar(Transform parent, int currentHp, int maxHp)
        {
            CreateHealthBar(parent, currentHp, maxHp, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.27f));
        }

        private void CreateHealthBar(Transform parent, int currentHp, int maxHp, Vector2 anchorMin, Vector2 anchorMax)
        {
            var frame = CreateImage(parent, "Health Bar", new Color(0.22f, 0.04f, 0.035f));
            SetRect(frame.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            frame.gameObject.AddComponent<Outline>().effectColor = new Color(0.08f, 0.02f, 0.015f);

            var fill = CreateImage(frame.transform, "Health Fill", new Color(0.83f, 0.08f, 0.06f));
            var hpRatio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)currentHp / maxHp);
            SetRect(fill.rectTransform, Vector2.zero, new Vector2(hpRatio, 1f), Vector2.zero, Vector2.zero);

            var text = CreateText(frame.transform, $"{currentHp}/{maxHp}", 15, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void RefreshUnitHealthViews()
        {
            RefreshUnitHealthViews(playerUnits);
            RefreshUnitHealthViews(enemyUnits);
        }

        private void RefreshUnitHealthViews(List<BattleUnit> units)
        {
            foreach (var unit in units)
            {
                if (unit == null || !unitViews.TryGetValue(unit.RuntimeId, out var rect) || rect == null)
                    continue;

                RefreshUnitHealthView(rect, unit);
            }
        }

        private static void RefreshUnitHealthView(RectTransform unitRect, BattleUnit unit)
        {
            var healthBar = unitRect.Find("Health Bar");
            if (healthBar == null)
                return;

            var fill = healthBar.Find("Health Fill") as RectTransform;
            if (fill != null)
            {
                var hpRatio = unit.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)unit.CurrentHp / unit.MaxHp);
                fill.anchorMax = new Vector2(hpRatio, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }

            var text = healthBar.GetComponentInChildren<Text>();
            if (text != null)
                text.text = $"{unit.CurrentHp}/{unit.MaxHp}";
        }
    }

    internal sealed class UnitSpriteImage : Image
    {
        public bool MirrorX { get; set; }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            if (!MirrorX)
                return;

            var centerX = rectTransform.rect.center.x;
            var vertex = new UIVertex();
            for (var i = 0; i < toFill.currentVertCount; i++)
            {
                toFill.PopulateUIVertex(ref vertex, i);
                vertex.position.x = centerX * 2f - vertex.position.x;
                toFill.SetUIVertex(vertex, i);
            }
        }
    }
}
