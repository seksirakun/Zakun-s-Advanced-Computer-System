using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace Zacs.Mapping
{
    [CustomEditor(typeof(MonitorObject))]
    public class MonitorObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Compiles the schematic, then writes the monitor file into the schematic folder. Press this again after a later schematic compile.",
                MessageType.Info);

            MonitorObject monitor = (MonitorObject)target;
            if (monitor.GetComponent<PrimitiveComponent>() == null)
                EditorGUILayout.HelpBox("This object must be a primitive cube.", MessageType.Warning);

            if (GUILayout.Button("Compile Monitor", GUILayout.Height(32f)))
                Compile(monitor);
        }

        [MenuItem("ZACS/Compile Monitors")]
        static void CompileAll()
        {
            MonitorObject[] monitors = FindObjectsOfType<MonitorObject>();
            if (monitors.Length == 0)
            {
                Debug.LogWarning("No ZACS monitor in the scene.");
                return;
            }

            HashSet<int> done = new HashSet<int>();
            for (int i = 0; i < monitors.Length; i++)
            {
                Schematic schematic = monitors[i].GetComponentInParent<Schematic>();
                if (schematic == null || !done.Add(schematic.GetInstanceID()))
                    continue;

                CompileSchematic(schematic);
            }
        }

        static void Compile(MonitorObject monitor)
        {
            Schematic schematic = monitor.GetComponentInParent<Schematic>();
            if (schematic == null)
            {
                Debug.LogError("ZACS: Monitor Object must sit under a Schematic.");
                return;
            }

            CompileSchematic(schematic);
        }

        static void CompileSchematic(Schematic schematic)
        {
            try
            {
                schematic.CompileSchematic();
            }
            catch (Exception ex)
            {
                Debug.LogError("ZACS schematic compile failed: " + ex.Message);
                return;
            }

            MonitorObject[] monitors = schematic.GetComponentsInChildren<MonitorObject>(false);
            ZacsFile file = new ZacsFile();
            file.Version = 1;
            List<ZacsEntry> entries = new List<ZacsEntry>();
            for (int i = 0; i < monitors.Length; i++)
            {
                MonitorObject monitor = monitors[i];
                if (monitor == null || !monitor.enabled)
                    continue;

                if (string.IsNullOrEmpty(monitor.MonitorName))
                {
                    Debug.LogError("ZACS: monitor name is empty. " + PathOf(monitor.transform), monitor);
                    continue;
                }

                string blockPath = RelativePath(schematic.transform, monitor.transform);
                if (string.IsNullOrEmpty(blockPath))
                {
                    Debug.LogError("ZACS: put the monitor on the cube, not on the schematic root.", monitor);
                    continue;
                }

                string powerPath = string.Empty;
                if (monitor.PowerButton != null)
                {
                    if (monitor.PowerButton.GetComponentInParent<Schematic>() != schematic)
                        Debug.LogError("ZACS: the power button must be inside the same schematic.", monitor.PowerButton);
                    else
                        powerPath = RelativePath(schematic.transform, monitor.PowerButton.transform) ?? string.Empty;
                }

                ZacsEntry entry = new ZacsEntry();
                entry.MonitorName = monitor.MonitorName.Trim();
                entry.Path = blockPath;
                entry.Face = monitor.Face.ToString();
                entry.Inset = monitor.Inset;
                entry.Depth = monitor.Depth;
                entry.StartsOn = monitor.StartsOn;
                entry.PowerPath = powerPath ?? string.Empty;
                entries.Add(entry);
            }

            file.Monitors = entries.ToArray();
            if (file.Monitors.Length == 0)
            {
                Debug.LogError("ZACS: no monitor to write.");
                return;
            }

            string json = JsonUtility.ToJson(file, true);
            string export = SchematicManager.Config.ExportPath;
            if (string.IsNullOrEmpty(export))
                export = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MapEditorReborn_CompiledSchematics");

            string folder = Path.Combine(export, schematic.name);
            string zipPath = folder + ".zip";
            bool zipped = SchematicManager.Config.ZipCompiledSchematics && File.Exists(zipPath);
            if (zipped)
            {
                WriteZip(zipPath, schematic.name + "/ZACS.json", json);
                string beside = Path.Combine(export, schematic.name + ".zacs.json");
                File.WriteAllText(beside, json);
                Debug.Log("ZACS monitor file written: " + zipPath + " and " + beside);
                return;
            }

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string output = Path.Combine(folder, "ZACS.json");
            File.WriteAllText(output, json);
            Debug.Log("ZACS monitor file written: " + output);
        }

        static void WriteZip(string zipPath, string entryName, string json)
        {
            using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry existing = zip.GetEntry(entryName);
                if (existing != null)
                    existing.Delete();

                ZipArchiveEntry entry = zip.CreateEntry(entryName);
                using (Stream stream = entry.Open())
                using (StreamWriter writer = new StreamWriter(stream))
                    writer.Write(json);
            }
        }

        static string RelativePath(Transform root, Transform target)
        {
            List<string> parts = new List<string>();
            Transform walk = target;
            while (walk != null && walk != root)
            {
                if (walk.GetComponent<SchematicBlock>() != null)
                    parts.Add(walk.name);

                walk = walk.parent;
            }

            if (walk != root)
                return null;

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        static string PathOf(Transform transform)
        {
            return transform != null ? transform.name : string.Empty;
        }
    }

    [Serializable]
    class ZacsFile
    {
        public int Version;
        public ZacsEntry[] Monitors;
    }

    [Serializable]
    class ZacsEntry
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
