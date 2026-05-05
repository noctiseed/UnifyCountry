using System.Collections.Generic;
using UnityEngine;

namespace UnifyCountry.Combat
{
    internal sealed class BattleFormation
    {
        public const int MaxFormationSlots = 5;
        public const int FormationRows = 3;
        public const int TotalFormationSlots = MaxFormationSlots * FormationRows;
        private const int EnemySlotStride = 1000;

        private readonly BattleState state;

        public BattleFormation(BattleState state)
        {
            this.state = state;
        }

        public bool TryInsertPlayerUnitAtGap(BattleUnit unit, int gapIndex)
        {
            var row = DecodeGapRow(gapIndex);
            var afterColumn = DecodeGapAfterColumn(gapIndex);
            if (row < 0 || row >= FormationRows || afterColumn < 0 || afterColumn >= MaxFormationSlots)
                return false;

            var rowSlots = GetOccupiedPlayerSlotsInRow(row);
            if (rowSlots.Count >= MaxFormationSlots)
                return false;

            var afterSlot = GetSlotIndex(row, afterColumn);
            if (state.PlayerUnits[afterSlot] == null || state.PlayerUnits[afterSlot].IsDead)
                return false;

            var targetColumn = afterColumn + 1;
            if (targetColumn >= MaxFormationSlots)
            {
                var emptyLeft = FindEmptyLeftInRow(row, afterColumn);
                if (emptyLeft < 0)
                    return false;

                ShiftLeftInRow(row, emptyLeft, afterColumn);
                state.PlayerUnits[afterSlot] = unit;
                return true;
            }

            var targetSlot = GetSlotIndex(row, targetColumn);
            if (state.PlayerUnits[targetSlot] == null)
            {
                state.PlayerUnits[targetSlot] = unit;
                return true;
            }

            var emptyRight = FindEmptyRightInRow(row, targetColumn);
            if (emptyRight >= 0)
            {
                ShiftRightInRow(row, targetColumn, emptyRight);
                state.PlayerUnits[targetSlot] = unit;
                return true;
            }

            var emptyLeftForGap = FindEmptyLeftInRow(row, afterColumn);
            if (emptyLeftForGap >= 0)
            {
                ShiftLeftInRow(row, emptyLeftForGap, afterColumn);
                state.PlayerUnits[afterSlot] = unit;
                return true;
            }

            return false;
        }

        public List<int> GetOccupiedPlayerSlotsInRow(int row)
        {
            var slots = new List<int>();
            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var slot = GetSlotIndex(row, column);
                if (state.PlayerUnits[slot] != null && !state.PlayerUnits[slot].IsDead)
                    slots.Add(slot);
            }

            return slots;
        }

        public int GetFirstEmptyPlayerSlot()
        {
            var preferredRows = new[] { 1, 0, 2 };
            foreach (var row in preferredRows)
            {
                for (var column = MaxFormationSlots - 1; column >= 0; column--)
                {
                    var slotIndex = GetSlotIndex(row, column);
                    if (state.PlayerUnits[slotIndex] == null)
                        return slotIndex;
                }
            }

            return GetSlotIndex(1, MaxFormationSlots - 1);
        }

        public void AddEnemyUnitToRow(BattleUnit unit, int row)
        {
            if (unit == null || row < 0 || row >= FormationRows)
                return;

            unit.FormationRow = row;
            state.EnemyUnits.Add(unit);
        }

        public int GetEnemyUnitRow(BattleUnit unit)
        {
            if (unit == null)
                return -1;

            return Mathf.Clamp(unit.FormationRow, 0, FormationRows - 1);
        }

        public List<BattleUnit> GetAliveEnemyUnitsInRow(int row)
        {
            var rowUnits = new List<BattleUnit>();
            foreach (var unit in state.EnemyUnits)
            {
                if (unit != null && !unit.IsDead && GetEnemyUnitRow(unit) == row)
                    rowUnits.Add(unit);
            }

            return rowUnits;
        }

        public int CountPlayerUnits()
        {
            return CountUnits(state.PlayerUnits);
        }

        public int CountEnemyUnits()
        {
            return CountUnits(state.EnemyUnits);
        }

        public bool HasAliveUnitsInRow(List<BattleUnit> units, int row)
        {
            if (units == null || row < 0 || row >= FormationRows)
                return false;

            if (ReferenceEquals(units, state.EnemyUnits))
                return GetAliveEnemyUnitsInRow(row).Count > 0;

            for (var column = 0; column < MaxFormationSlots; column++)
            {
                var unit = units[GetSlotIndex(row, column)];
                if (unit != null && !unit.IsDead)
                    return true;
            }

            return false;
        }

        public BattleUnit GetPlayerFrontUnit(int row)
        {
            for (var column = MaxFormationSlots - 1; column >= 0; column--)
            {
                var unit = state.PlayerUnits[GetSlotIndex(row, column)];
                if (unit != null && !unit.IsDead)
                    return unit;
            }

            return null;
        }

        public BattleUnit GetEnemyFrontUnit(int row)
        {
            foreach (var unit in state.EnemyUnits)
            {
                if (unit != null && !unit.IsDead && GetEnemyUnitRow(unit) == row)
                    return unit;
            }

            return null;
        }

        public List<FormationMove> AdvanceFormation(List<BattleUnit> units, bool playerSide)
        {
            if (!playerSide)
                return AdvanceEnemyFormation();

            var oldSlots = new Dictionary<int, int>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit != null && !unit.IsDead)
                    oldSlots[unit.RuntimeId] = i;
            }

            for (var row = 0; row < FormationRows; row++)
            {
                var aliveUnits = new List<BattleUnit>();
                for (var column = 0; column < MaxFormationSlots; column++)
                {
                    var unit = units[GetSlotIndex(row, column)];
                    if (unit != null && !unit.IsDead)
                        aliveUnits.Add(unit);

                    units[GetSlotIndex(row, column)] = null;
                }

                var startColumn = MaxFormationSlots - aliveUnits.Count;
                for (var i = 0; i < aliveUnits.Count; i++)
                    units[GetSlotIndex(row, startColumn + i)] = aliveUnits[i];
            }

            var moves = new List<FormationMove>();
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null)
                    continue;

                if (oldSlots.TryGetValue(unit.RuntimeId, out var fromSlot) && fromSlot != i)
                    moves.Add(new FormationMove(unit.RuntimeId, fromSlot, i, playerSide));
            }

            return moves;
        }

        private List<FormationMove> AdvanceEnemyFormation()
        {
            var oldSlots = new Dictionary<int, int>();
            var oldColumnsByRow = new int[FormationRows];
            foreach (var unit in state.EnemyUnits)
            {
                if (unit == null || unit.IsDead)
                    continue;

                var row = GetEnemyUnitRow(unit);
                oldSlots[unit.RuntimeId] = EncodeEnemySlotIndex(row, oldColumnsByRow[row], oldColumnsByRow[row] + 1);
                oldColumnsByRow[row]++;
            }

            var aliveRows = new List<BattleUnit>[FormationRows];
            for (var row = 0; row < FormationRows; row++)
                aliveRows[row] = new List<BattleUnit>();

            foreach (var unit in state.EnemyUnits)
            {
                if (unit == null || unit.IsDead)
                    continue;

                var row = GetEnemyUnitRow(unit);
                unit.FormationRow = row;
                aliveRows[row].Add(unit);
            }

            state.EnemyUnits.Clear();
            for (var row = 0; row < FormationRows; row++)
                state.EnemyUnits.AddRange(aliveRows[row]);

            var moves = new List<FormationMove>();
            var newColumnsByRow = new int[FormationRows];
            foreach (var unit in state.EnemyUnits)
            {
                var row = GetEnemyUnitRow(unit);
                var toSlot = EncodeEnemySlotIndex(row, newColumnsByRow[row], aliveRows[row].Count);
                newColumnsByRow[row]++;
                if (oldSlots.TryGetValue(unit.RuntimeId, out var fromSlot) && fromSlot != toSlot)
                    moves.Add(new FormationMove(unit.RuntimeId, fromSlot, toSlot, false));
            }

            return moves;
        }

        public void AppendDeathLogs(List<string> logLines)
        {
            foreach (var unit in state.EnemyUnits)
            {
                if (unit != null && unit.IsDead)
                    logLines.Add($"{unit.Name} 阵亡。");
            }

            foreach (var unit in state.PlayerUnits)
            {
                if (unit != null && unit.IsDead)
                    logLines.Add($"{unit.Name} 阵亡。");
            }
        }

        public void ResolveEndOfTurnBuffs(List<string> logLines)
        {
            ResolveEndOfTurnBuffs(state.PlayerUnits, logLines);
            ResolveEndOfTurnBuffs(state.EnemyUnits, logLines);
        }

        public bool RemoveDeadUnitsFromFormation(List<string> logLines)
        {
            var removed = false;
            removed |= RemoveDeadUnitsFromFormation(state.EnemyUnits, logLines);
            removed |= RemoveDeadUnitsFromFormation(state.PlayerUnits, logLines);
            return removed;
        }

        public static int GetUnitSlotIndex(List<BattleUnit> units, BattleUnit target)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i] == target)
                    return i;
            }

            return -1;
        }

        public static int EncodeGapIndex(int row, int afterColumn)
        {
            return row * MaxFormationSlots + afterColumn;
        }

        public static int DecodeGapRow(int gapIndex)
        {
            return Mathf.Clamp(gapIndex / MaxFormationSlots, 0, FormationRows - 1);
        }

        public static int DecodeGapAfterColumn(int gapIndex)
        {
            return Mathf.Clamp(gapIndex % MaxFormationSlots, 0, MaxFormationSlots - 1);
        }

        public static int GetSlotIndex(int row, int column)
        {
            return row * MaxFormationSlots + column;
        }

        public static int GetSlotRow(int slotIndex)
        {
            return Mathf.Clamp(slotIndex / MaxFormationSlots, 0, FormationRows - 1);
        }

        public static string GetFormationRowName(int row)
        {
            switch (Mathf.Clamp(row, 0, FormationRows - 1))
            {
                case 0:
                    return "后军";
                case 1:
                    return "中军";
                default:
                    return "前军";
            }
        }

        public static int GetSlotColumn(int slotIndex)
        {
            return Mathf.Clamp(slotIndex % MaxFormationSlots, 0, MaxFormationSlots - 1);
        }

        public static int EncodeEnemySlotIndex(int row, int column)
        {
            return EncodeEnemySlotIndex(row, column, 0);
        }

        public static int EncodeEnemySlotIndex(int row, int column, int rowUnitCount)
        {
            var encodedRow = Mathf.Clamp(row, 0, FormationRows - 1);
            var encodedColumn = Mathf.Max(0, column);
            var encodedCount = Mathf.Max(0, rowUnitCount);
            return encodedRow * EnemySlotStride + encodedCount * 100 + encodedColumn;
        }

        public static int GetEnemySlotRow(int slotIndex)
        {
            return Mathf.Clamp(slotIndex / EnemySlotStride, 0, FormationRows - 1);
        }

        public static int GetEnemySlotColumn(int slotIndex)
        {
            return Mathf.Max(0, slotIndex % 100);
        }

        public static int GetEnemySlotRowUnitCount(int slotIndex)
        {
            return Mathf.Max(0, slotIndex % EnemySlotStride / 100);
        }

        private static int CountUnits(List<BattleUnit> units)
        {
            var count = 0;
            foreach (var unit in units)
            {
                if (unit != null && !unit.IsDead)
                    count++;
            }

            return count;
        }

        private int FindEmptyLeftInRow(int row, int fromColumn)
        {
            for (var column = fromColumn - 1; column >= 0; column--)
            {
                if (state.PlayerUnits[GetSlotIndex(row, column)] == null)
                    return column;
            }

            return -1;
        }

        private int FindEmptyRightInRow(int row, int fromColumn)
        {
            for (var column = fromColumn + 1; column < MaxFormationSlots; column++)
            {
                if (state.PlayerUnits[GetSlotIndex(row, column)] == null)
                    return column;
            }

            return -1;
        }

        private void ShiftRightInRow(int row, int fromColumn, int emptyColumn)
        {
            for (var column = emptyColumn; column > fromColumn; column--)
                state.PlayerUnits[GetSlotIndex(row, column)] = state.PlayerUnits[GetSlotIndex(row, column - 1)];
        }

        private void ShiftLeftInRow(int row, int emptyColumn, int toColumn)
        {
            for (var column = emptyColumn; column < toColumn; column++)
                state.PlayerUnits[GetSlotIndex(row, column)] = state.PlayerUnits[GetSlotIndex(row, column + 1)];
        }

        private static void ResolveEndOfTurnBuffs(List<BattleUnit> units, List<string> logLines)
        {
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || unit.IsDead || unit.Revival <= 0)
                    continue;

                var revivalBefore = unit.Revival;
                var healed = unit.ResolveRevival();
                logLines.Add($"{unit.Name} 触发复苏 {revivalBefore}，恢复 {healed} 点生命，复苏降为 {unit.Revival}。");
            }
        }

        private static bool RemoveDeadUnitsFromFormation(List<BattleUnit> units, List<string> logLines)
        {
            var removed = false;
            for (var i = units.Count - 1; i >= 0; i--)
            {
                var unit = units[i];
                if (unit == null || !unit.IsDead)
                    continue;

                if (logLines != null)
                    logLines.Add($"{unit.Name} 阵亡，移出阵地。");
                units[i] = null;
                removed = true;
            }

            return removed;
        }
    }
}
