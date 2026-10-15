namespace Zacs.Programs
{
    public sealed class SamplePanel : MonitorProgram
    {
        Widget _status;
        Toggle _lock;
        Slider _brightness;
        Progress _level;

        public override void Open()
        {
            _status = Zml.Text("Ready").Font(24f).Paint(Palette.Text);
            _lock = Zml.Toggle("Lock", false, OnLock);
            _brightness = Zml.Slider(0.65f, OnBrightness);
            _level = Zml.Progress(0.65f);

            Session.Root = Zml.Column(
                Zml.Text("FACILITY").Font(42f).Paint(Palette.Brass),
                Zml.Text("Control surface").Font(22f).Paint(Palette.Muted),
                Zml.Separator(),
                _status,
                Zml.Row(
                    Zml.Button("Door", OnDoor).Flex(1f),
                    Zml.Button("Light", OnLight).Flex(1f),
                    Zml.Button("Alarm", OnAlarm).Flex(1f)).Gap(12f),
                _lock,
                Zml.Text("Brightness").Font(22f).Paint(Palette.Muted),
                _brightness,
                _level,
                Zml.Field("Query", OnQuery))
                .Pad(28f)
                .Gap(14f)
                .Back(Palette.Background);
        }

        void OnDoor(MonitorSession session, Button button)
        {
            _status.Text = "Door command";
            session.Touch();
        }

        void OnLight(MonitorSession session, Button button)
        {
            _status.Text = "Light command";
            session.Touch();
        }

        void OnAlarm(MonitorSession session, Button button)
        {
            _status.Text = _status.Text == "Alarm on" ? "Alarm off" : "Alarm on";
            _status.Foreground = _status.Text == "Alarm on" ? Palette.Danger : Palette.Text;
            session.Touch();
        }

        void OnLock(MonitorSession session, Toggle toggle)
        {
            _status.Text = toggle.Value ? "Locked" : "Unlocked";
            session.Touch();
        }

        void OnBrightness(MonitorSession session, Slider slider)
        {
            _level.Value = slider.Normalized;
            session.Touch();
        }

        void OnQuery(MonitorSession session, string value)
        {
            _status.Text = string.IsNullOrEmpty(value) ? "Query empty" : value;
            session.Touch();
        }
    }
}
