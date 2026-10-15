using System;
using System.Collections.Generic;
using Log = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Zacs
{
    internal sealed class ClickBinding
    {
        readonly MonitorServer _server;
        readonly ZacsConfig _config;
        SSKeybindSetting _bind;
        float _check;
        bool _hooked;

        public ClickBinding(MonitorServer server, ZacsConfig config)
        {
            _server = server;
            _config = config;
        }

        public void Enable()
        {
            KeyCode key = KeyCode.E;
            if (!string.IsNullOrEmpty(_config.SuggestedKey))
            {
                try
                {
                    key = (KeyCode)Enum.Parse(typeof(KeyCode), _config.SuggestedKey, true);
                }
                catch (ArgumentException)
                {
                    Log.Warn("ZACS key name is invalid, using E.");
                }
            }

            int id = _config.ClickSettingId == 0 ? 77021 : _config.ClickSettingId;
            _bind = new SSKeybindSetting(id, _config.ClickLabel, key, true, false, _config.ClickHint);
            _hooked = true;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSetting;
            Publish();
        }

        public void Disable()
        {
            if (_hooked)
            {
                ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSetting;
                _hooked = false;
            }

            Remove();
        }

        public void Maintain(float delta)
        {
            _check += delta;
            if (_check < 2f)
                return;

            _check = 0f;
            if (!Listed())
                Publish();
        }

        void OnSetting(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            SSKeybindSetting key = setting as SSKeybindSetting;
            if (key == null || _bind == null || key.SettingId != _bind.SettingId)
                return;

            Player player = Player.Get(hub);
            if (player == null)
                return;

            _server.Enqueue(player, key.SyncIsPressed);
        }

        void Publish()
        {
            if (_bind == null)
                return;

            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings;
            int keep = 0;
            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    ServerSpecificSettingBase entry = current[i];
                    if (entry != null && entry.SettingId != _bind.SettingId)
                        keep++;
                }
            }

            ServerSpecificSettingBase[] next = new ServerSpecificSettingBase[keep + 1];
            int write = 0;
            if (current != null)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    ServerSpecificSettingBase entry = current[i];
                    if (entry == null || entry.SettingId == _bind.SettingId)
                        continue;

                    next[write] = entry;
                    write++;
                }
            }

            next[write] = _bind;
            ServerSpecificSettingsSync.DefinedSettings = next;
            ServerSpecificSettingsSync.SendToAll();
        }

        void Remove()
        {
            if (_bind == null)
                return;

            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings;
            if (current == null)
                return;

            List<ServerSpecificSettingBase> kept = new List<ServerSpecificSettingBase>(current.Length);
            for (int i = 0; i < current.Length; i++)
            {
                ServerSpecificSettingBase entry = current[i];
                if (entry != null && entry.SettingId != _bind.SettingId)
                    kept.Add(entry);
            }

            ServerSpecificSettingsSync.DefinedSettings = kept.ToArray();
            ServerSpecificSettingsSync.SendToAll();
        }

        bool Listed()
        {
            if (_bind == null)
                return false;

            ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings;
            if (current == null)
                return false;

            for (int i = 0; i < current.Length; i++)
            {
                ServerSpecificSettingBase entry = current[i];
                if (entry != null && entry.SettingId == _bind.SettingId)
                    return true;
            }

            return false;
        }
    }
}
