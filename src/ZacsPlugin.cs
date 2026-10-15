using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using Zacs.Programs;

namespace Zacs
{
    public sealed class ZacsPlugin : Plugin<ZacsConfig>
    {
        public override string Name
        {
            get { return "Zakun's Advanced Computer System"; }
        }

        public override string Description
        {
            get { return "Look-driven panels on map monitors."; }
        }

        public override string Author
        {
            get { return "seksirakun48"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0); }
        }

        public override Version RequiredApiVersion
        {
            get { return new Version(1, 1, 7); }
        }

        public override LoadPriority Priority
        {
            get { return LoadPriority.Low; }
        }

        MonitorServer _server;
        SchematicBridge _bridge;
        ClickBinding _clicks;
        bool _ticking;

        public override void Enable()
        {
            if (Config == null || !Config.IsEnabled)
                return;

            ZacsApi.Register("zacs.desktop", session => new DesktopProgram());
            ZacsApi.Register("zacs.sample", session => new SamplePanel());
            ZacsApi.Changed += OnProgramsChanged;

            _server = new MonitorServer(Config);
            _bridge = new SchematicBridge(_server);
            _clicks = new ClickBinding(_server, Config);
            LabApi.Features.Console.Logger.Warn("ZACS enabled. Waiting for ProjectMER schematic spawned.");
            _bridge.Hook();
            PlayerEvents.Left += OnLeft;
            StaticUnityMethods.OnUpdate += OnUpdate;
            _ticking = true;
            try
            {
                _clicks.Enable();
            }
            catch (Exception ex)
            {
                LabApi.Features.Console.Logger.Error("ZACS keybind setup failed: " + ex.Message);
            }

            _bridge.Scan();
        }

        public override void Disable()
        {
            ZacsApi.Changed -= OnProgramsChanged;
            PlayerEvents.Left -= OnLeft;

            if (_clicks != null)
                _clicks.Disable();

            if (_bridge != null)
                _bridge.Unhook();

            if (_ticking)
            {
                StaticUnityMethods.OnUpdate -= OnUpdate;
                _ticking = false;
            }

            if (_server != null)
                _server.Shutdown();

            _clicks = null;
            _bridge = null;
            _server = null;
        }

        void OnProgramsChanged(string monitorName)
        {
            if (_server != null)
                _server.Reload(monitorName);
        }

        void OnUpdate()
        {
            float delta = UnityEngine.Time.deltaTime;
            if (_clicks != null)
                _clicks.Maintain(delta);

            if (_bridge != null)
                _bridge.Flush();

            if (_server != null)
                _server.Tick(delta);
        }

        void OnLeft(PlayerLeftEventArgs args)
        {
            if (_server == null || args == null || args.Player == null)
                return;

            _server.PlayerLeft(args.Player.PlayerId);
        }
    }
}
