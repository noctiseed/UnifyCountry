using System.Collections.Generic;
using UnifyCountry.Combat;
using UnifyCountry.Config;
using UnityEngine;
using UnityEngine.UI;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private void BuildBoard(Transform parent, bool playerSide, List<BattleUnit> units)
        {
            EnsureFormationSlotCount(units);

            for (var row = 0; row < FormationRows; row++)
            {
                for (var column = 0; column < MaxFormationSlots; column++)
                {
                    var slotIndex = GetSlotIndex(row, column);
                    var slot = CreateImage(parent, $"Slot R{row + 1} C{column + 1}", GetSlotColor(row));
                    SetRect(slot.rectTransform, GetSlotAnchorMin(slotIndex, playerSide), GetSlotAnchorMax(slotIndex, playerSide), Vector2.zero, Vector2.zero);

                    var outline = slot.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.35f, 0.25f, 0.16f, 0.7f);
                    outline.effectDistance = new Vector2(2f, -2f);

                }

                CreateSkillRowTargetZone(parent, row, playerSide);
            }

            for (var i = 0; i < units.Count; i++)
            {
                var battleUnit = units[i];
                if (battleUnit == null)
                    continue;

                var index = i;
                if (animatedSlotOverrides.TryGetValue(battleUnit.RuntimeId, out var overrideSlot))
                    index = overrideSlot;

                var unit = CreateUnitToken(parent, battleUnit, false);
                SetRect(unit, GetUnitAnchorMin(index, playerSide), GetUnitAnchorMax(index, playerSide), Vector2.zero, Vector2.zero);
                unit.localScale = Vector3.one * GetDepthScale(index);
                var targetHandler = unit.gameObject.AddComponent<SkillTargetHandler>();
                targetHandler.Initialize(this, SkillTarget.ForUnit(playerSide, GetSlotRow(i), i, battleUnit));
                unitViews[battleUnit.RuntimeId] = unit;
            }

            if (playerSide && !isResolvingTurn && CountPlayerUnits() < TotalFormationSlots)
            {
                for (var i = 0; i < Mathf.Min(TotalFormationSlots, units.Count); i++)
                {
                    if (units[i] == null)
                        CreatePlayerInsertDropZone(parent, i);
                }

                for (var row = 0; row < FormationRows; row++)
                    CreatePlayerGapDropZonesForRow(parent, row);
            }
        }

        private static void EnsureFormationSlotCount(List<BattleUnit> units)
        {
            if (units == null)
                return;

            while (units.Count < TotalFormationSlots)
                units.Add(null);
        }

        private void BuildPlayerBase(Transform parent)
        {
            var root = CreateImage(parent, "大本营", new Color(0.88f, 0.72f, 0.42f));
            SetRect(root.rectTransform, new Vector2(0.34f, 0.88f), new Vector2(0.66f, 0.98f), Vector2.zero, Vector2.zero);

            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.28f, 0.18f, 0.08f);
            outline.effectDistance = new Vector2(2f, -2f);

            var label = CreateText(root.transform, $"大本营  {playerBaseHp}/{PlayerBaseMaxHp}", 18, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.04f));
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), Vector2.one, Vector2.zero, Vector2.zero);

            CreateHealthBar(root.transform, playerBaseHp, PlayerBaseMaxHp);
        }

        private void CreatePlayerInsertDropZone(Transform parent, int insertIndex)
        {
            var zone = CreateImage(parent, $"Insert {insertIndex}", new Color(0.2f, 0.7f, 0.95f, 0.08f));
            SetRect(zone.rectTransform, GetSlotAnchorMin(insertIndex), GetSlotAnchorMax(insertIndex), Vector2.zero, Vector2.zero);

            var outline = zone.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.45f, 0.85f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            var dropZone = zone.gameObject.AddComponent<BoardInsertDropZone>();
            dropZone.Initialize(this, insertIndex, false, zone);
        }

        private void CreatePlayerGapDropZonesForRow(Transform parent, int row)
        {
            var occupiedSlots = GetOccupiedPlayerSlotsInRow(row);
            if (occupiedSlots.Count == 0 || occupiedSlots.Count >= MaxFormationSlots)
                return;

            for (var i = 0; i < occupiedSlots.Count - 1; i++)
            {
                var leftColumn = GetSlotColumn(occupiedSlots[i]);
                CreatePlayerGapDropZone(parent, row, leftColumn, occupiedSlots);
            }

            var rightmostColumn = GetSlotColumn(occupiedSlots[occupiedSlots.Count - 1]);
            CreatePlayerGapDropZone(parent, row, rightmostColumn, occupiedSlots);
        }

        private void CreatePlayerGapDropZone(Transform parent, int row, int afterColumn, List<int> occupiedSlots)
        {
            var gapIndex = EncodeGapIndex(row, afterColumn);
            var anchorMin = GetGapZoneAnchorMin(gapIndex, occupiedSlots);
            var anchorMax = GetGapZoneAnchorMax(gapIndex, occupiedSlots);
            var zone = CreateImage(parent, $"Gap R{row + 1} C{afterColumn + 1}", Color.clear);
            SetRect(zone.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            zone.transform.SetAsLastSibling();

            var marker = CreateImage(zone.transform, "Insert Marker", new Color(0.02f, 0.42f, 0.18f, 0.95f));
            SetRect(marker.rectTransform, new Vector2(0.46f, 0.08f), new Vector2(0.54f, 0.92f), Vector2.zero, Vector2.zero);
            marker.raycastTarget = false;
            marker.enabled = false;

            var dropZone = zone.gameObject.AddComponent<BoardInsertDropZone>();
            dropZone.Initialize(this, gapIndex, true, zone, marker);
        }

        private static Vector2 GetGapZoneAnchorMin(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var center = GetGapZoneCenter(gapIndex, occupiedSlots);
            const float width = 0.055f;
            return new Vector2(Mathf.Clamp(center.x - width * 0.5f, 0.02f, 0.94f), GetSlotAnchorMin(GetSlotIndex(row, 0)).y);
        }

        private static Vector2 GetGapZoneAnchorMax(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var center = GetGapZoneCenter(gapIndex, occupiedSlots);
            const float width = 0.055f;
            return new Vector2(Mathf.Clamp(center.x + width * 0.5f, 0.06f, 0.98f), GetSlotAnchorMax(GetSlotIndex(row, 0)).y);
        }

        private static Vector2 GetGapZoneCenter(int gapIndex, List<int> occupiedSlots)
        {
            var row = DecodeGapRow(gapIndex);
            var afterColumn = DecodeGapAfterColumn(gapIndex);
            var afterSlot = GetSlotIndex(row, afterColumn);
            var afterMax = GetSlotAnchorMax(afterSlot);
            var nextSlot = -1;
            foreach (var slot in occupiedSlots)
            {
                if (GetSlotColumn(slot) > afterColumn)
                {
                    nextSlot = slot;
                    break;
                }
            }

            if (nextSlot >= 0)
                return new Vector2((afterMax.x + GetSlotAnchorMin(nextSlot).x) * 0.5f, GetSlotCenter(afterSlot).y);

            return new Vector2(afterMax.x + 0.025f, GetSlotCenter(afterSlot).y);
        }

        private static int EncodeGapIndex(int row, int afterColumn)
        {
            return BattleFormation.EncodeGapIndex(row, afterColumn);
        }

        private static int DecodeGapRow(int gapIndex)
        {
            return BattleFormation.DecodeGapRow(gapIndex);
        }

        private static int DecodeGapAfterColumn(int gapIndex)
        {
            return BattleFormation.DecodeGapAfterColumn(gapIndex);
        }

        private static int GetSlotIndex(int row, int column)
        {
            return BattleFormation.GetSlotIndex(row, column);
        }

        private static int GetSlotRow(int slotIndex)
        {
            return BattleFormation.GetSlotRow(slotIndex);
        }

        private static string GetFormationRowName(int row)
        {
            return BattleFormation.GetFormationRowName(row);
        }

        private static int GetSlotColumn(int slotIndex)
        {
            return BattleFormation.GetSlotColumn(slotIndex);
        }

        private static Color GetSlotColor(int row)
        {
            var alpha = 0.34f + row * 0.08f;
            return new Color(1f, 1f, 1f, alpha);
        }

        private static Vector2 GetSlotCenter(int slotIndex, bool playerSide = true)
        {
            var row = GetSlotRow(slotIndex);
            var column = GetSlotColumn(slotIndex);
            var rowOffset = row - 1;
            var playerX = 0.16f + column * 0.165f + rowOffset * 0.055f;
            if (playerSide)
                return new Vector2(playerX, 0.70f - row * 0.25f);

            var mirroredColumn = MaxFormationSlots - 1 - column;
            var mirroredPlayerX = 0.16f + mirroredColumn * 0.165f + rowOffset * 0.055f;
            return new Vector2(1f - mirroredPlayerX, 0.70f - row * 0.25f);
        }

        private static Vector2 GetSlotAnchorMin(int slotIndex, bool playerSide = true)
        {
            var center = GetSlotCenter(slotIndex, playerSide);
            const float width = 0.074f;
            const float height = 0.068f;
            return new Vector2(center.x - width, center.y - height);
        }

        private static Vector2 GetSlotAnchorMax(int slotIndex, bool playerSide = true)
        {
            var center = GetSlotCenter(slotIndex, playerSide);
            const float width = 0.074f;
            const float height = 0.068f;
            return new Vector2(center.x + width, center.y + height);
        }

        private static Vector2 GetUnitAnchorMin(int slotIndex, bool playerSide = true)
        {
            var center = GetSlotCenter(slotIndex, playerSide);
            const float width = 0.058f;
            const float height = 0.15f;
            return new Vector2(center.x - width, center.y - height * 0.55f);
        }

        private static Vector2 GetUnitAnchorMax(int slotIndex, bool playerSide = true)
        {
            var center = GetSlotCenter(slotIndex, playerSide);
            const float width = 0.058f;
            const float height = 0.15f;
            return new Vector2(center.x + width, center.y + height);
        }

        private static float GetDepthScale(int slotIndex)
        {
            return 1f;
        }

        private void BuildUpcomingWaveHint(Transform parent)
        {
            var waves = CurrentWaves;
            var hint = waves != null && nextWaveIndex < waves.Count ? $"下波：{DescribeWave(waves[nextWaveIndex])}" : "已无后续波次";
            var text = CreateText(parent, hint, 20, TextAnchor.MiddleCenter, new Color(0.22f, 0.12f, 0.1f));
            SetRect(text.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
        }

        private string DescribeWave(WaveSpawnRecord wave)
        {
            var names = new List<string>();
            for (var row = 0; row < Mathf.Min(FormationRows, wave.RowCardIds.Length); row++)
            {
                foreach (var cardId in wave.RowCardIds[row])
                {
                    if (cardMap.TryGetValue(cardId, out var card))
                        names.Add($"{card.CardName}({GetFormationRowName(row)})");
                }
            }

            return names.Count == 0 ? "-" : string.Join("、", names);
        }
    }
}
