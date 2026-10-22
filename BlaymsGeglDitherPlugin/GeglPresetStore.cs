using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace BlaymsGeglPlugin
{
    internal sealed class GeglPresetRecord
    {
        public string EffectId { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public int Levels { get; set; }
        public int Method { get; set; }
        public int RedLevels { get; set; }
        public int GreenLevels { get; set; }
        public int BlueLevels { get; set; }
        public int AlphaLevels { get; set; }
        public int Seed { get; set; }

        public GeglPresetRecord()
        {
            EffectId = String.Empty;
            Name = String.Empty;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class GeglPresetDatabase
    {
        public List<GeglPresetRecord> History { get; set; }
        public List<GeglPresetRecord> Saved { get; set; }

        public GeglPresetDatabase()
        {
            History = new List<GeglPresetRecord>();
            Saved = new List<GeglPresetRecord>();
        }
    }

    internal static class GeglPresetStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static string FilePath
        {
            get
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Blayms",
                    "GeglPNETPlugin");

                return Path.Combine(directory, "presets.json");
            }
        }

        public static List<GeglPresetRecord> GetHistory(string effectId)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();
                List<GeglPresetRecord> result = new List<GeglPresetRecord>();

                for (int i = 0; i < database.History.Count; i++)
                {
                    GeglPresetRecord record = database.History[i];

                    if (String.Equals(record.EffectId, effectId, StringComparison.Ordinal))
                    {
                        result.Add(record);
                    }
                }

                result.Sort(delegate(GeglPresetRecord first, GeglPresetRecord second)
                {
                    return second.Timestamp.CompareTo(first.Timestamp);
                });

                return result;
            }
        }

        public static List<GeglPresetRecord> GetSaved(string effectId)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();
                List<GeglPresetRecord> result = new List<GeglPresetRecord>();

                for (int i = 0; i < database.Saved.Count; i++)
                {
                    GeglPresetRecord record = database.Saved[i];

                    if (String.Equals(record.EffectId, effectId, StringComparison.Ordinal))
                    {
                        result.Add(record);
                    }
                }

                result.Sort(delegate(GeglPresetRecord first, GeglPresetRecord second)
                {
                    return StringComparer.CurrentCultureIgnoreCase.Compare(first.Name, second.Name);
                });

                return result;
            }
        }

        public static void AddHistory(GeglPresetRecord record)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();
                record.Name = "Last used " + record.Timestamp.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                database.History.Add(record);

                List<GeglPresetRecord> effectHistory = new List<GeglPresetRecord>();

                for (int i = 0; i < database.History.Count; i++)
                {
                    if (String.Equals(database.History[i].EffectId, record.EffectId, StringComparison.Ordinal))
                    {
                        effectHistory.Add(database.History[i]);
                    }
                }

                effectHistory.Sort(delegate(GeglPresetRecord first, GeglPresetRecord second)
                {
                    return second.Timestamp.CompareTo(first.Timestamp);
                });

                int kept = 0;

                for (int i = database.History.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(database.History[i].EffectId, record.EffectId, StringComparison.Ordinal))
                    {
                        database.History.RemoveAt(i);
                    }
                }

                for (int i = 0; i < effectHistory.Count && kept < 10; i++)
                {
                    database.History.Add(effectHistory[i]);
                    kept++;
                }

                SaveDatabase(database);
            }
        }

        public static void SavePreset(GeglPresetRecord record)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();

                for (int i = database.Saved.Count - 1; i >= 0; i--)
                {
                    GeglPresetRecord current = database.Saved[i];

                    if (String.Equals(current.EffectId, record.EffectId, StringComparison.Ordinal) &&
                        String.Equals(current.Name, record.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        database.Saved.RemoveAt(i);
                    }
                }

                database.Saved.Add(record);
                SaveDatabase(database);
            }
        }

        public static void RenamePreset(string effectId, string oldName, string newName)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();
                GeglPresetRecord target = null;

                for (int i = 0; i < database.Saved.Count; i++)
                {
                    GeglPresetRecord current = database.Saved[i];

                    if (String.Equals(current.EffectId, effectId, StringComparison.Ordinal) &&
                        String.Equals(current.Name, oldName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        target = current;
                        break;
                    }
                }

                if (target == null)
                {
                    return;
                }

                for (int i = database.Saved.Count - 1; i >= 0; i--)
                {
                    GeglPresetRecord current = database.Saved[i];

                    if (!Object.ReferenceEquals(current, target) &&
                        String.Equals(current.EffectId, effectId, StringComparison.Ordinal) &&
                        String.Equals(current.Name, newName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        database.Saved.RemoveAt(i);
                    }
                }

                target.Name = newName;
                SaveDatabase(database);
            }
        }

        public static void DeletePreset(string effectId, string name)
        {
            lock (SyncRoot)
            {
                GeglPresetDatabase database = LoadDatabase();

                for (int i = database.Saved.Count - 1; i >= 0; i--)
                {
                    GeglPresetRecord current = database.Saved[i];

                    if (String.Equals(current.EffectId, effectId, StringComparison.Ordinal) &&
                        String.Equals(current.Name, name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        database.Saved.RemoveAt(i);
                    }
                }

                SaveDatabase(database);
            }
        }

        private static GeglPresetDatabase LoadDatabase()
        {
            string path = FilePath;

            if (!File.Exists(path))
            {
                return new GeglPresetDatabase();
            }

            try
            {
                string json = File.ReadAllText(path);
                GeglPresetDatabase database = JsonSerializer.Deserialize<GeglPresetDatabase>(json, JsonOptions);

                if (database == null)
                {
                    return new GeglPresetDatabase();
                }

                if (database.History == null)
                {
                    database.History = new List<GeglPresetRecord>();
                }

                if (database.Saved == null)
                {
                    database.Saved = new List<GeglPresetRecord>();
                }

                return database;
            }
            catch (Exception)
            {
                return new GeglPresetDatabase();
            }
        }

        private static void SaveDatabase(GeglPresetDatabase database)
        {
            string path = FilePath;
            string directory = Path.GetDirectoryName(path);

            Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            string json = JsonSerializer.Serialize(database, JsonOptions);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
            {
                File.Move(temporaryPath, path, true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
    }
}
