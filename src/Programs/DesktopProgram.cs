using System;

namespace Zacs.Programs
{
    public sealed class DesktopProgram : MonitorProgram
    {
        Widget _clock;
        Widget _seat;
        TextField _note;
        int _second = -1;

        public override void Open()
        {
            _clock = Zml.Text(DateTime.Now.ToString("HH:mm:ss")).Font(42f).Paint(Palette.Text);
            _seat = Zml.Text("Empty").Font(36f).Paint(Palette.Muted).Flex(1f);
            _note = Zml.Field("Note", OnNote).Font(36f);
            Window window = Zml.Window(
                "Terminal",
                Zml.Text("The first player to click this screen owns it.").Font(40f).Paint(Palette.Muted),
                Zml.Row(
                    Zml.Button("Status", OnStatus).Font(36f).Flex(1f),
                    Zml.Button("Clear", OnClear).Font(36f).Flex(1f)).Gap(16f),
                _note);

            Session.Root = Zml.Column(
                Zml.Row(
                    Zml.Text("ZACS").Font(54f).Paint(Palette.Brass),
                    Zml.Spacer(),
                    _clock).Tall(110f).Pad(22f).Back(Palette.Plate),
                Zml.Separator(),
                window.Flex(1f),
                Zml.Row(
                    Zml.Text("Session").Font(32f).Paint(Palette.Muted).Wide(280f),
                    _seat).Tall(84f).Pad(20f).Back(Palette.Raised))
                .Back(Palette.Background);

            Session.Claimed += OnClaimed;
            Session.Released += OnReleased;
        }

        public override void Close()
        {
            if (Session == null)
                return;

            Session.Claimed -= OnClaimed;
            Session.Released -= OnReleased;
        }

        public override void Tick(float delta)
        {
            int second = DateTime.Now.Second;
            if (second == _second)
                return;

            _second = second;
            _clock.Text = DateTime.Now.ToString("HH:mm:ss");
            Session.Touch();
        }

        void OnClaimed(MonitorSession session, LabApi.Features.Wrappers.Player player)
        {
            _seat.Text = player != null ? player.Nickname : "Taken";
            session.Touch();
        }

        void OnReleased(MonitorSession session)
        {
            _seat.Text = "Empty";
            session.Touch();
        }

        void OnStatus(MonitorSession session, Button button)
        {
            _seat.Text = session.HasOwner ? "Linked" : "Empty";
            session.Touch();
        }

        void OnClear(MonitorSession session, Button button)
        {
            if (_note != null)
                _note.Value = string.Empty;

            session.Touch();
        }

        void OnNote(MonitorSession session, string value)
        {
            _seat.Text = string.IsNullOrEmpty(value) ? "Note empty" : value;
            session.Touch();
        }
    }

}
