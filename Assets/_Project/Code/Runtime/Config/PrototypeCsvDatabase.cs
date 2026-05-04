using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnifyCountry.Config
{
    public static class PrototypeCsvDatabase
    {
        public static List<CardRecord> LoadCards(TextAsset cardsCsv, TextAsset unitsCsv, TextAsset effectsCsv = null)
        {
            var records = new List<CardRecord>();
            if (cardsCsv == null)
                return records;

            var unitMap = LoadUnits(unitsCsv);
            var effectMap = LoadEffects(effectsCsv);
            var rows = ReadRows(cardsCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 11)
                    continue;

                var card = new CardRecord
                {
                    CardId = row[0],
                    CardName = row[1],
                    CardType = ParseEnum(row[2], CardType.Unit),
                    Cost = ParseInt(row[3]),
                    Camp = ParseEnum(row[4], CardCamp.Player),
                    Faction = row[5],
                    Rarity = row[6],
                    MaxCopiesInDeck = ParseInt(row[7]),
                    ArtId = row[8],
                    EffectId = row[9],
                    DescriptionKey = row[10]
                };

                if (unitMap.TryGetValue(card.CardId, out var unit))
                {
                    card.Unit = unit;
                    foreach (var effectId in unit.SkillEffectIds)
                    {
                        if (effectMap.TryGetValue(effectId, out var effect))
                            card.Effects.Add(effect);
                    }
                }

                if (!string.IsNullOrWhiteSpace(card.EffectId) && effectMap.TryGetValue(card.EffectId, out var cardEffect) && !card.Effects.Contains(cardEffect))
                    card.Effects.Add(cardEffect);

                records.Add(card);
            }

            return records;
        }

        private static Dictionary<string, UnitRecord> LoadUnits(TextAsset unitsCsv)
        {
            var records = new Dictionary<string, UnitRecord>();
            if (unitsCsv == null)
                return records;

            var rows = ReadRows(unitsCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 8)
                    continue;

                var unit = new UnitRecord
                {
                    CardId = row[0],
                    UnitId = row[1],
                    UnitName = row[2],
                    UnitType = ParseEnum(row[3], UnitType.Soldier),
                    Hp = ParseInt(row[4]),
                    Attack = ParseInt(row[5]),
                    Role = row[6],
                    Tags = row[7]
                };

                if (row.Count > 8)
                    AddIds(unit.SkillEffectIds, row[8]);

                if (!string.IsNullOrWhiteSpace(unit.CardId))
                    records[unit.CardId] = unit;
            }

            return records;
        }

        private static Dictionary<string, EffectRecord> LoadEffects(TextAsset effectsCsv)
        {
            var records = new Dictionary<string, EffectRecord>();
            if (effectsCsv == null)
                return records;

            var rows = ReadRows(effectsCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 9)
                    continue;

                var effect = new EffectRecord
                {
                    EffectId = row[0],
                    EffectName = row[1],
                    Timing = row[2],
                    EffectType = row[3],
                    TargetRule = row[4],
                    Value = ParseInt(row[5]),
                    SecondaryValue = ParseInt(row[6]),
                    Tags = row[7],
                    Description = row[8]
                };

                if (!string.IsNullOrWhiteSpace(effect.EffectId))
                    records[effect.EffectId] = effect;
            }

            return records;
        }

        public static Dictionary<string, int> LoadStartingDeck(TextAsset deckCsv)
        {
            var deck = new Dictionary<string, int>();
            if (deckCsv == null)
                return deck;

            var rows = ReadRows(deckCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 2)
                    continue;

                deck[row[0]] = ParseInt(row[1]);
            }

            return deck;
        }

        public static List<BattleLevelRecord> LoadBattleLevels(TextAsset wavesCsv)
        {
            var levels = new List<BattleLevelRecord>();
            if (wavesCsv == null)
                return levels;

            var levelMap = new Dictionary<string, BattleLevelRecord>();
            var rows = ReadRows(wavesCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 8)
                    continue;

                var levelId = row[0];
                if (string.IsNullOrWhiteSpace(levelId))
                    continue;

                if (!levelMap.TryGetValue(levelId, out var level))
                {
                    level = new BattleLevelRecord { LevelId = levelId };
                    levelMap[levelId] = level;
                    levels.Add(level);
                }

                var wave = new WaveSpawnRecord
                {
                    WaveId = row[1],
                    TurnIndex = ParseInt(row[2]),
                    SpawnTiming = row[3],
                    NoteKey = row[7]
                };

                AddCardIds(wave.RowCardIds[0], row[4]);
                AddCardIds(wave.RowCardIds[1], row[5]);
                AddCardIds(wave.RowCardIds[2], row[6]);
                level.Waves.Add(wave);
            }

            return levels;
        }

        private static void AddCardIds(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            AddIds(values, value);
        }

        private static void AddIds(List<string> values, string value)
        {
            var ids = value.Split('|');
            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    values.Add(id.Trim());
            }
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }

        private static List<List<string>> ReadRows(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = string.Empty;
            var inQuotes = false;

            for (var i = 0; i < csv.Length; i++)
            {
                var c = csv[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    row.Add(cell.Trim());
                    cell = string.Empty;
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;

                    row.Add(cell.Trim());
                    cell = string.Empty;

                    if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
                        rows.Add(row);

                    row = new List<string>();
                }
                else
                {
                    cell += c;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.Trim());
                rows.Add(row);
            }

            return rows;
        }
    }
}
