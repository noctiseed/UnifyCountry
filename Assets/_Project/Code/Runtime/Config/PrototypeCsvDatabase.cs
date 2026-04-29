using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnifyCountry.Config
{
    public static class PrototypeCsvDatabase
    {
        public static List<CardRecord> LoadCards(TextAsset cardsCsv)
        {
            var records = new List<CardRecord>();
            if (cardsCsv == null)
                return records;

            var rows = ReadRows(cardsCsv.text);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 12)
                    continue;

                records.Add(new CardRecord
                {
                    CardId = row[0],
                    CardName = row[1],
                    UnitId = row[2],
                    UnitName = row[3],
                    UnitType = ParseEnum(row[4], UnitType.Soldier),
                    Hp = ParseInt(row[5]),
                    Attack = ParseInt(row[6]),
                    Cost = ParseInt(row[7]),
                    Camp = ParseEnum(row[8], CardCamp.Player),
                    Faction = row[9],
                    MaxCopiesInDeck = ParseInt(row[10]),
                    DescriptionKey = row[11]
                });
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
