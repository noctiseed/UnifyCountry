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

            yield return StartCoroutine(AnimateFormationMoves(moves));

            BuildUi();
            yield return new WaitForSeconds(0.15f);
        }

        private IEnumerator ResolveDeathsAndAdvanceRoutine(List<string> logLines)
        {
            var deadRuntimeIds = CollectDeadRuntimeIds();
            if (deadRuntimeIds.Count == 0)
                yield break;

            AppendDeathLogs(logLines);
            UpdateActiveTurnLog(logLines);
            yield return StartCoroutine(AnimateDeathUnits(deadRuntimeIds));

            RemoveDeadUnitsFromFormation(null);
            yield return StartCoroutine(AdvanceFormationsRoutine(logLines));
        }

        private List<int> CollectDeadRuntimeIds()
        {
            var ids = new List<int>();
            CollectDeadRuntimeIds(playerUnits, ids);
            CollectDeadRuntimeIds(enemyUnits, ids);
            return ids;
        }

        private static void CollectDeadRuntimeIds(List<BattleUnit> units, List<int> ids)
        {
            foreach (var unit in units)
            {
                if (unit != null && unit.IsDead)
                    ids.Add(unit.RuntimeId);
            }
        }

        private IEnumerator AnimateDeathUnits(List<int> deadRuntimeIds)
        {
            var rects = new List<RectTransform>();
            var groups = new List<CanvasGroup>();
            var startScales = new List<Vector3>();

            foreach (var runtimeId in deadRuntimeIds)
            {
                if (!unitViews.TryGetValue(runtimeId, out var rect) || rect == null)
                    continue;

                rects.Add(rect);
                startScales.Add(rect.localScale);
                var group = rect.GetComponent<CanvasGroup>();
                if (group == null)
                    group = rect.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }

            if (rects.Count == 0)
                yield break;

            const float duration = 0.32f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);

                for (var i = 0; i < rects.Count; i++)
                {
                    if (rects[i] == null)
                        continue;

                    rects[i].localScale = Vector3.Lerp(startScales[i], startScales[i] * 0.78f, eased);
                    groups[i].alpha = 1f - eased;
                }

                yield return null;
            }

            foreach (var runtimeId in deadRuntimeIds)
            {
                if (!unitViews.TryGetValue(runtimeId, out var rect) || rect == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(rect.gameObject);
                else
                    DestroyImmediate(rect.gameObject);

                unitViews.Remove(runtimeId);
            }
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
