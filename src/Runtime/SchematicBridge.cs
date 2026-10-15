using System;
using System.Collections.Generic;
using System.IO;
using Log = LabApi.Features.Console.Logger;
using ProjectMER.Events.Arguments;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace Zacs
{
    internal sealed class SchematicBridge
    {
        readonly MonitorServer _server;
        readonly List<SchematicObject> _pending = new List<SchematicObject>(8);

        public SchematicBridge(MonitorServer server)
        {
            _server = server;
        }

        public void Hook()
        {
            Schematic.SchematicSpawned += OnSpawned;
            Schematic.SchematicDestroyed += OnDestroyed;
        }

        public void Unhook()
        {
            Schematic.SchematicSpawned -= OnSpawned;
            Schematic.SchematicDestroyed -= OnDestroyed;
        }

        public void Scan()
        {
#pragma warning disable CS0618
            SchematicObject[] found = UnityEngine.Object.FindObjectsOfType<SchematicObject>();
#pragma warning restore CS0618
            Log.Warn("ZACS scan: " + found.Length + " schematic already in the world.");
            for (int i = 0; i < found.Length; i++)
                Queue(found[i], "scan");
        }

        public void Flush()
        {
            if (_pending.Count == 0)
                return;

            for (int i = 0; i < _pending.Count; i++)
            {
                SchematicObject schematic = _pending[i];
                if (schematic == null)
                    continue;

                Attach(schematic);
            }

            _pending.Clear();
        }

        void OnSpawned(SchematicSpawnedEventArgs args)
        {
            if (args == null || args.Schematic == null)
            {
                Log.Warn("ZACS schematic spawned with an empty payload.");
                return;
            }

            Log.Warn("ZACS schematic spawned: " + args.Name + " dir=" + args.Schematic.DirectoryPath);
            Queue(args.Schematic, "spawned");
        }

        void OnDestroyed(SchematicDestroyedEventArgs args)
        {
            if (args == null || args.Schematic == null)
                return;

            _server.RemoveSchematic(args.Schematic);
        }

        void Queue(SchematicObject schematic, string source)
        {
            if (schematic == null)
                return;

            for (int i = 0; i < _pending.Count; i++)
            {
                if (ReferenceEquals(_pending[i], schematic))
                    return;
            }

            _pending.Add(schematic);
            Log.Warn("ZACS queued (" + source + "): " + schematic.Name);
        }

        void Attach(SchematicObject schematic)
        {
            if (schematic == null)
                return;

            if (string.IsNullOrEmpty(schematic.DirectoryPath))
            {
                Log.Warn("ZACS screen not found: " + schematic.Name + " has no directory.");
                return;
            }

            string path = Resolve(schematic);
            if (path == null)
            {
                Log.Warn("ZACS screen not found: no ZACS.json for " + schematic.Name + " in " + schematic.DirectoryPath);
                return;
            }

            Log.Warn("ZACS file: " + path);

            List<ZacsMonitorEntryData> monitors;
            try
            {
                monitors = ReadMonitors(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Log.Warn("ZACS screen not found: could not read ZACS.json. " + ex.Message);
                return;
            }

            Log.Warn("ZACS monitors in file: " + monitors.Count);
            if (monitors.Count == 0)
            {
                Log.Warn("ZACS screen not found: " + schematic.Name + " has no monitors in ZACS.json.");
                return;
            }

            for (int i = 0; i < monitors.Count; i++)
            {
                ZacsMonitorEntryData entry = monitors[i];
                if (entry == null || string.IsNullOrEmpty(entry.MonitorName) || string.IsNullOrEmpty(entry.Path))
                {
                    Log.Warn("ZACS screen not found: empty monitor entry, index " + i);
                    continue;
                }

                MonitorFace face = MonitorFace.Forward;
                try
                {
                    face = (MonitorFace)Enum.Parse(typeof(MonitorFace), string.IsNullOrEmpty(entry.Face) ? "Forward" : entry.Face, true);
                }
                catch (ArgumentException)
                {
                    Log.Warn("ZACS face unknown, using Forward: " + entry.Face);
                    face = MonitorFace.Forward;
                }

                Log.Warn("ZACS entry: " + entry.MonitorName + " path=" + entry.Path + " face=" + face);
                try
                {
                    _server.Spawn(schematic, entry.MonitorName.Trim(), entry.Path, entry.PowerPath, face, entry.Inset, entry.Depth, entry.StartsOn);
                }
                catch (Exception ex)
                {
                    Log.Warn("ZACS screen not found: spawn failed. " + ex);
                }
            }
        }

        static List<ZacsMonitorEntryData> ReadMonitors(string json)
        {
            List<ZacsMonitorEntryData> list = new List<ZacsMonitorEntryData>();
            if (string.IsNullOrEmpty(json))
                return list;

            int index = 0;
            while (index < json.Length)
            {
                int nameAt = json.IndexOf("\"MonitorName\"", index, StringComparison.Ordinal);
                if (nameAt < 0)
                    break;

                int next = json.IndexOf("\"MonitorName\"", nameAt + 12, StringComparison.Ordinal);
                int end = next < 0 ? json.Length : next;
                string slice = json.Substring(nameAt, end - nameAt);
                ZacsMonitorEntryData entry = new ZacsMonitorEntryData();
                entry.MonitorName = ReadString(slice, "MonitorName");
                entry.Path = ReadString(slice, "Path");
                entry.Face = ReadString(slice, "Face");
                entry.PowerPath = ReadString(slice, "PowerPath");
                entry.Inset = ReadFloat(slice, "Inset");
                entry.Depth = ReadFloat(slice, "Depth");
                entry.StartsOn = ReadBool(slice, "StartsOn", true);
                list.Add(entry);
                index = nameAt + 12;
            }

            return list;
        }

        static string ReadString(string json, string key)
        {
            int keyAt = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (keyAt < 0)
                return string.Empty;

            int colon = json.IndexOf(':', keyAt + key.Length + 2);
            if (colon < 0)
                return string.Empty;

            int first = json.IndexOf('"', colon + 1);
            if (first < 0)
                return string.Empty;

            int second = json.IndexOf('"', first + 1);
            if (second < 0)
                return string.Empty;

            return json.Substring(first + 1, second - first - 1);
        }

        static float ReadFloat(string json, string key)
        {
            string raw = ReadRaw(json, key);
            float value;
            if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                return value;

            return 0f;
        }

        static bool ReadBool(string json, string key, bool fallback)
        {
            string raw = ReadRaw(json, key);
            if (raw == "true")
                return true;
            if (raw == "false")
                return false;

            return fallback;
        }

        static string ReadRaw(string json, string key)
        {
            int keyAt = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (keyAt < 0)
                return string.Empty;

            int colon = json.IndexOf(':', keyAt + key.Length + 2);
            if (colon < 0)
                return string.Empty;

            int start = colon + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;

            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != '\n' && json[end] != '\r')
                end++;

            return json.Substring(start, end - start).Trim();
        }

        static string Resolve(SchematicObject schematic)
        {
            string inside = Path.Combine(schematic.DirectoryPath, "ZACS.json");
            if (File.Exists(inside))
                return inside;

            string parent = Path.GetDirectoryName(schematic.DirectoryPath);
            if (string.IsNullOrEmpty(parent))
                return null;

            string beside = Path.Combine(parent, schematic.Name + ".zacs.json");
            if (File.Exists(beside))
                return beside;

            return null;
        }
    }

    [Serializable]
    public class ZacsMonitorFileData
    {
        public int Version;
        public ZacsMonitorEntryData[] Monitors;
    }

    [Serializable]
    public class ZacsMonitorEntryData
    {
        public string MonitorName;
        public string Path;
        public string Face;
        public float Inset;
        public float Depth;
        public bool StartsOn;
        public string PowerPath;
    }
}
