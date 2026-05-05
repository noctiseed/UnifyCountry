using System.Collections;
using System.Collections.Generic;
using UnifyCountry.Combat;
using UnityEngine;

namespace UnifyCountry.UI
{
    public sealed partial class PrototypeBattleUi
    {
        private IEnumerator AdvanceFormationsRoutine(List<string> logLines)
        {
            var moves = new List<FormationMove>();
            moves.AddRange(battleFormation.AdvanceFormation(playerUnits, true));
            moves.AddRange(battleFormation.AdvanceFormation(enemyUnits, false));

            if (moves.Count == 0)
                yield break;

            logLines.Add("阵型向前补位。");
            UpdateActiveTurnLog(logLines);

            animatedSlotOverrides.Clear();
            foreach (var move in moves)
                animatedSlotOverrides[move.UnitRuntimeId] = move.FromSlotIndex;

            BuildUi();
            yield return StartCoroutine(AnimateFormationMoves(moves));
            animatedSlotOverrides.Clear();

            BuildUi();
            yield return new WaitForSeconds(0.15f);
        }

        private IEnumerator AnimateFormationMoves(List<FormationMove> moves)
        {
            var rects = new List<RectTransform>();
            var startMins = new List<Vector2>();
            var startMaxes = new List<Vector2>();
            var targetMins = new List<Vector2>();
            var targetMaxes = new List<Vector2>();

            foreach (var move in moves)
            {
                if (!unitViews.TryGetValue(move.UnitRuntimeId, out var rect))
                    continue;

                rects.Add(rect);
                startMins.Add(rect.anchorMin);
                startMaxes.Add(rect.anchorMax);
                targetMins.Add(GetUnitAnchorMin(move.ToSlotIndex, move.PlayerSide));
                targetMaxes.Add(GetUnitAnchorMax(move.ToSlotIndex, move.PlayerSide));
            }

            var elapsed = 0f;
            while (elapsed < FormationMoveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / FormationMoveDuration);
                t = t * t * (3f - 2f * t);

                for (var i = 0; i < rects.Count; i++)
                {
                    rects[i].anchorMin = Vector2.Lerp(startMins[i], targetMins[i], t);
                    rects[i].anchorMax = Vector2.Lerp(startMaxes[i], targetMaxes[i], t);
                    rects[i].offsetMin = Vector2.zero;
                    rects[i].offsetMax = Vector2.zero;
                }

                yield return null;
            }
        }

        private void AppendDeathLogs(List<string> logLines)
        {
            battleFormation.AppendDeathLogs(logLines);
        }

        private void ResolveEndOfTurnBuffs(List<string> logLines)
        {
            battleFormation.ResolveEndOfTurnBuffs(logLines);
        }

        private bool RemoveDeadUnitsFromFormation(List<string> logLines)
        {
            return battleFormation.RemoveDeadUnitsFromFormation(logLines);
        }
    }
}
