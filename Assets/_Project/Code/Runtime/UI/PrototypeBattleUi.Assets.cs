using System.Collections.Generic;
using System.Linq;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void RebuildCardPortraitMap()
        {
            if (cardPortraits == null)
            {
                cardPortraitMap = new Dictionary<string, Sprite>();
                return;
            }

            cardPortraitMap = cardPortraits
                .Where(entry => entry != null
                    && !string.IsNullOrWhiteSpace(entry.cardId)
                    && entry.portrait != null)
                .GroupBy(entry => entry.cardId)
                .ToDictionary(group => group.Key, group => group.First().portrait);
        }

        private bool TryGetCardPortrait(CardRecord card, out Sprite portrait)
        {
            if (cardPortraitMap != null && cardPortraitMap.TryGetValue(card.CardId, out portrait))
                return true;

#if UNITY_EDITOR
            portrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_Project/Art/Cards/Portraits/{(string.IsNullOrWhiteSpace(card.ArtId) ? card.CardId : card.ArtId)}.png");
            return portrait != null;
#else
            portrait = null;
            return false;
#endif
        }

        private bool TryGetUnitSprite(BattleUnit unit, out Sprite sprite)
        {
#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/_Project/Art/Units/Sprites/{unit.UnitId}.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }

        private bool TryGetAttackIconSprite(out Sprite sprite)
        {
            if (attackIconSprite != null)
            {
                sprite = attackIconSprite;
                return true;
            }

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/UI/Icons/icon_attack_sword.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }

        private bool TryGetShieldIconSprite(out Sprite sprite)
        {
            if (shieldIconSprite != null)
            {
                sprite = shieldIconSprite;
                return true;
            }

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/UI/Icons/icon_shield.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }

        private bool TryGetRegenerationIconSprite(out Sprite sprite)
        {
            if (regenerationIconSprite != null)
            {
                sprite = regenerationIconSprite;
                return true;
            }

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/UI/Icons/icon_regeneration.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }

        private bool TryGetBurnIconSprite(out Sprite sprite)
        {
            if (burnIconSprite != null)
            {
                sprite = burnIconSprite;
                return true;
            }

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Project/Art/UI/Icons/icon_burn.png");
            return sprite != null;
#else
            sprite = null;
            return false;
#endif
        }
    }
}
