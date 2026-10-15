using System;
using System.Collections.Generic;
using Log = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace Zacs
{
    internal sealed class MonitorServer
    {
        static MonitorServer _current;
        static readonly List<MonitorSession> SessionView = new List<MonitorSession>(16);
        static readonly Dictionary<int, float> HintAt = new Dictionary<int, float>(16);

        readonly List<MonitorHost> _hosts = new List<MonitorHost>(16);
        readonly List<PendingClick> _clicks = new List<PendingClick>(8);
        readonly ZacsConfig _config;
        Player[] _players = new Player[16];
        MonitorHost _heldHost;
        int _heldPlayer;

        public MonitorServer(ZacsConfig config)
        {
            _config = config ?? new ZacsConfig();
            _current = this;
        }

        public static IReadOnlyList<MonitorSession> Sessions
        {
            get { return SessionView; }
        }

        public static void Deny(Player player)
        {
            if (player == null)
                return;

            float now = Time.time;
            float until;
            if (HintAt.TryGetValue(player.PlayerId, out until) && until > now)
                return;

            HintAt[player.PlayerId] = now + 2.5f;
            player.SendHint("This terminal is in use.", 2f);
        }

        public void Enqueue(Player player, bool pressed)
        {
            if (player == null)
                return;

            _clicks.Add(new PendingClick { Player = player, Pressed = pressed });
        }

        public void Tick(float delta)
        {
            int count = CollectPlayers();
            DrainClicks(count);
            for (int i = 0; i < _hosts.Count; i++)
                _hosts[i].Tick(delta, _players, count);
        }

        public void Spawn(SchematicObject schematic, string monitorName, string blockPath, string powerPath, MonitorFace face, float inset, float depth, bool startsOn)
        {
            if (schematic == null || string.IsNullOrEmpty(blockPath))
                return;

            for (int i = 0; i < _hosts.Count; i++)
            {
                MonitorHost existing = _hosts[i];
                if (ReferenceEquals(existing.Schematic, schematic) && existing.Session.BlockPath == blockPath)
                    return;
            }

            Transform block = Walk(schematic.transform, blockPath);
            if (block == null)
            {
                Log.Warn("ZACS screen not found: " + schematic.Name + " / " + blockPath + " | objects: " + Describe(schematic.transform));
                return;
            }

            MonitorSession session = new MonitorSession();
            session.MonitorName = monitorName;
            session.SchematicName = schematic.Name;
            session.BlockPath = blockPath;
            session.IsPowered = startsOn;

            MonitorHost host = new MonitorHost();
            host.Schematic = schematic;
            host.Session = session;
            host.Config = _config;
            host.Block = block;
            host.Face = face;
            host.Inset = inset;
            host.Depth = depth;
            if (!string.IsNullOrEmpty(powerPath))
                host.Power = Walk(schematic.transform, powerPath);

            host.Bind();
            host.LoadProgram();
            _hosts.Add(host);
            try
            {
                host.Build();
            }
            catch (Exception ex)
            {
                Log.Warn("ZACS screen draw failed: " + ex);
            }
            SessionView.Add(session);
            Log.Info("ZACS monitor ready: " + monitorName + " (" + schematic.Name + "/" + blockPath + ")");
        }

        public void RemoveSchematic(SchematicObject schematic)
        {
            for (int i = _hosts.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_hosts[i].Schematic, schematic))
                    continue;

                Drop(i);
            }

            if (_heldHost != null && ReferenceEquals(_heldHost.Schematic, schematic))
            {
                _heldHost = null;
                _heldPlayer = 0;
            }
        }

        public void Reload(string monitorName)
        {
            for (int i = 0; i < _hosts.Count; i++)
            {
                MonitorHost host = _hosts[i];
                if (string.Equals(host.Session.MonitorName, monitorName, StringComparison.OrdinalIgnoreCase))
                    host.LoadProgram();
            }
        }

        public void PlayerLeft(int playerId)
        {
            if (_heldPlayer == playerId)
            {
                if (_heldHost != null)
                    _heldHost.ReleasePress();

                _heldHost = null;
                _heldPlayer = 0;
            }

            for (int i = 0; i < _hosts.Count; i++)
            {
                if (_hosts[i].Session.OwnerId == playerId)
                    _hosts[i].Release();
            }

            HintAt.Remove(playerId);
        }

        public void Shutdown()
        {
            for (int i = _hosts.Count - 1; i >= 0; i--)
                Drop(i);

            _clicks.Clear();
            HintAt.Clear();
            if (ReferenceEquals(_current, this))
                _current = null;
        }

        void DrainClicks(int playerCount)
        {
            for (int i = 0; i < _clicks.Count; i++)
            {
                PendingClick click = _clicks[i];
                Player player = click.Player;
                if (player == null || !player.IsAlive)
                    continue;

                if (click.Pressed)
                {
                    Look look;
                    MonitorHost host = Closest(player, out look);
                    if (host == null)
                        continue;

                    _heldHost = host;
                    _heldPlayer = player.PlayerId;
                    host.Press(player, look);
                }
                else if (_heldHost != null && _heldPlayer == player.PlayerId)
                {
                    _heldHost.ReleasePress();
                    _heldHost = null;
                    _heldPlayer = 0;
                }
            }

            _clicks.Clear();
        }

        MonitorHost Closest(Player player, out Look best)
        {
            best = new Look();
            MonitorHost chosen = null;
            float bestDistance = float.MaxValue;
            float maxDistance = _config.MaxDistance > 0.4f ? _config.MaxDistance : 0.4f;
            for (int i = 0; i < _hosts.Count; i++)
            {
                Look look;
                if (!_hosts[i].TryLook(player, maxDistance, out look))
                    continue;

                if (look.Distance < bestDistance)
                {
                    bestDistance = look.Distance;
                    best = look;
                    chosen = _hosts[i];
                }
            }

            return chosen;
        }

        int CollectPlayers()
        {
            int count = 0;
            foreach (Player player in Player.List)
            {
                if (player == null || !player.IsAlive || player.IsNpc || player.IsHost || player.Camera == null)
                    continue;

                if (count == _players.Length)
                {
                    Player[] bigger = new Player[_players.Length * 2];
                    Array.Copy(_players, bigger, _players.Length);
                    _players = bigger;
                }

                _players[count] = player;
                count++;
            }

            return count;
        }

        void Drop(int index)
        {
            MonitorHost host = _hosts[index];
            host.UnbindPower();
            host.Destroy();
            SessionView.Remove(host.Session);
            _hosts.RemoveAt(index);
        }

        static Transform Walk(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            string[] parts = path.Split('/');
            Transform current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i];
                if (string.IsNullOrEmpty(name))
                    continue;

                Transform next = null;
                int childCount = current.childCount;
                for (int c = 0; c < childCount; c++)
                {
                    Transform child = current.GetChild(c);
                    if (child.name == name)
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                    return null;

                current = next;
            }

            return current == root ? null : current;
        }

        static string Describe(Transform root)
        {
            if (root == null)
                return "(none)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                if (builder.Length > 0)
                    builder.Append(", ");

                builder.Append(root.GetChild(i).name);
                if (builder.Length > 360)
                {
                    builder.Append("...");
                    break;
                }
            }

            if (count == 0)
                builder.Append("(none)");

            return builder.ToString();
        }

        struct PendingClick
        {
            public Player Player;
            public bool Pressed;
        }
    }
}
