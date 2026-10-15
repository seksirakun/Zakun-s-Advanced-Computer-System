using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace Zacs
{
    public abstract class MonitorProgram
    {
        public MonitorSession Session { get; internal set; }

        public virtual void Open()
        {
        }

        public virtual void Close()
        {
        }

        public virtual void Tick(float delta)
        {
        }

        public virtual void OnPower(bool on)
        {
        }
    }

    public sealed class MonitorSession
    {
        public string MonitorName { get; internal set; }

        public string SchematicName { get; internal set; }

        public string BlockPath { get; internal set; }

        public bool IsPowered { get; internal set; }

        public Player Owner { get; internal set; }

        public Widget Root { get; set; }

        public event Action<MonitorSession, Player> Claimed;

        public event Action<MonitorSession> Released;

        public event Action<MonitorSession, bool> PowerChanged;

        internal MonitorProgram Program;
        internal TextField Focused;
        internal Widget Hover;
        internal Widget Pressed;
        internal bool LayoutDirty = true;
        internal bool VisualDirty = true;
        internal bool KeyboardOpen;
        internal bool Shift;
        internal bool Faulted;
        internal int NextId = 1;
        internal int OwnerId;
        internal float Away;
        internal Action<bool> PowerRequest;
        internal Action ReleaseRequest;

        public bool HasOwner
        {
            get { return Owner != null; }
        }

        public void Invalidate()
        {
            LayoutDirty = true;
            VisualDirty = true;
        }

        public void Touch()
        {
            VisualDirty = true;
        }

        public Widget Find(string name)
        {
            if (Root == null)
                return null;

            return Root.Find(name);
        }

        public void SetPower(bool on)
        {
            if (PowerRequest != null)
                PowerRequest(on);
        }

        public void Release()
        {
            if (ReleaseRequest != null)
                ReleaseRequest();
        }

        public void CloseField()
        {
            Focused = null;
            KeyboardOpen = false;
            Shift = false;
            LayoutDirty = true;
            VisualDirty = true;
        }

        internal void RaiseClaimed(Player player)
        {
            Action<MonitorSession, Player> handler = Claimed;
            if (handler != null)
                handler(this, player);
        }

        internal void RaiseReleased()
        {
            Action<MonitorSession> handler = Released;
            if (handler != null)
                handler(this);
        }

        internal void RaisePower(bool on)
        {
            Action<MonitorSession, bool> handler = PowerChanged;
            if (handler != null)
                handler(this, on);
        }
    }

    public static class ZacsApi
    {
        static readonly Dictionary<string, Func<MonitorSession, MonitorProgram>> Programs =
            new Dictionary<string, Func<MonitorSession, MonitorProgram>>(StringComparer.OrdinalIgnoreCase);

        internal static Action<string> Changed;

        public static IReadOnlyList<MonitorSession> Sessions
        {
            get { return MonitorServer.Sessions; }
        }

        public static bool IsRegistered(string monitorName)
        {
            if (string.IsNullOrEmpty(monitorName))
                return false;

            return Programs.ContainsKey(monitorName.Trim());
        }

        public static bool Register(string monitorName, Func<MonitorSession, MonitorProgram> create)
        {
            if (string.IsNullOrEmpty(monitorName) || create == null)
                return false;

            string key = monitorName.Trim();
            Programs[key] = create;
            Action<string> changed = Changed;
            if (changed != null)
                changed(key);

            return true;
        }

        public static void Unregister(string monitorName)
        {
            if (string.IsNullOrEmpty(monitorName))
                return;

            string key = monitorName.Trim();
            if (!Programs.Remove(key))
                return;

            Action<string> changed = Changed;
            if (changed != null)
                changed(key);
        }

        public static MonitorSession Find(string schematicName, string blockPath)
        {
            IReadOnlyList<MonitorSession> sessions = Sessions;
            for (int i = 0; i < sessions.Count; i++)
            {
                MonitorSession session = sessions[i];
                if (session == null)
                    continue;

                if (!string.Equals(session.SchematicName, schematicName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(session.BlockPath, blockPath, StringComparison.Ordinal))
                    return session;
            }

            return null;
        }

        internal static bool TryCreate(string monitorName, MonitorSession session, out MonitorProgram program)
        {
            program = null;
            Func<MonitorSession, MonitorProgram> create;
            if (string.IsNullOrEmpty(monitorName) || !Programs.TryGetValue(monitorName.Trim(), out create))
                return false;

            program = create(session);
            return program != null;
        }
    }
}
