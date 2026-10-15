using System;
using System.Collections.Generic;
using AdminToys;
using Log = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace Zacs
{
    internal sealed class MonitorHost
    {
        static readonly string[] DigitRow = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        static readonly string[] RowQ = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" };
        static readonly string[] RowA = { "a", "s", "d", "f", "g", "h", "j", "k", "l" };
        static readonly string[] RowZ = { "z", "x", "c", "v", "b", "n", "m" };

        public SchematicObject Schematic;
        public MonitorSession Session;
        public ZacsConfig Config;
        public Transform Block;
        public Transform Power;
        public MonitorFace Face;
        public float Inset;
        public float Depth;

        readonly MonitorVisuals _visuals = new MonitorVisuals();
        readonly List<KeyCap> _keys = new List<KeyCap>(48);
        ScreenPose _pose;
        Flow _keyboard;
        InteractableToy _powerToy;
        Action<Player> _onPower;
        Player _preview;
        float _caretTime;
        bool _caretOn = true;
        bool _closed;

        public void Bind()
        {
            Session.PowerRequest = SetPower;
            Session.ReleaseRequest = Release;
            if (Power == null)
                return;

            InvisibleInteractableToy raw;
            if (!Power.TryGetComponent(out raw))
            {
                Log.Warn("ZACS power button is not an interactable: " + Session.BlockPath);
                return;
            }

            _powerToy = InteractableToy.Get(raw);
            if (_powerToy == null)
                return;

            _onPower = delegate { SetPower(!Session.IsPowered); };
            _powerToy.OnInteracted += _onPower;
        }

        public void UnbindPower()
        {
            if (_powerToy == null || _onPower == null || _powerToy.IsDestroyed)
                return;

            _powerToy.OnInteracted -= _onPower;
            _onPower = null;
        }

        public bool TryLook(Player player, float maxDistance, out Look look)
        {
            look = new Look();
            if (_closed || player == null || player.Camera == null)
                return false;

            _pose.Refresh(Block, Face, Inset, Depth, Config.VirtualHeight);
            if (!_pose.Valid)
                return false;

            return _pose.TryHit(player.Camera.position, player.Camera.forward, Reach(maxDistance), out look);
        }

        public void Build()
        {
            if (_closed || Block == null)
                return;

            _pose.Refresh(Block, Face, Inset, Depth, Config.VirtualHeight);
            if (!_pose.Valid)
            {
                Log.Warn("ZACS screen setup failed, cube scale is invalid: " + Session.BlockPath);
                return;
            }

            Session.Invalidate();
            Redraw();
            Log.Warn("ZACS screen ready: " + Session.MonitorName + " dist=" + Vector3.Distance(_pose.Center, Block.position).ToString("F2"));
        }

        public void Tick(float delta, Player[] players, int playerCount)
        {
            if (_closed || Block == null || Session == null)
                return;

            _pose.Refresh(Block, Face, Inset, Depth, Config.VirtualHeight);
            if (!_pose.Valid)
                return;

            Look ownerLook = new Look();
            bool ownerHit = false;
            if (Session.HasOwner)
            {
                Player owner = FindPlayer(players, playerCount, Session.OwnerId);
                if (owner == null || !owner.IsAlive)
                {
                    Release();
                }
                else
                {
                    ownerHit = owner.Camera != null && _pose.TryHit(owner.Camera.position, owner.Camera.forward, Reach(Config.MaxDistance), out ownerLook);
                    if (ownerHit)
                        Session.Away = 0f;
                    else
                    {
                        Session.Away += delta;
                        if (Session.Away >= Config.ReleaseDelay)
                            Release();
                    }
                }
            }

            Look preview = new Look();
            bool previewHit = false;
            if (!Session.HasOwner)
                previewHit = PickPreview(players, playerCount, out preview);

            if (Session.IsPowered && Session.Pressed is Slider && Session.HasOwner && ownerHit)
            {
                Slider slider = (Slider)Session.Pressed;
                float span = slider.TrackMax - slider.TrackMin;
                if (span > 0.001f)
                {
                    slider.SetNormalized(Session, (ownerLook.VirtualX - slider.TrackMin) / span);
                    Session.Touch();
                }
            }

            Widget hover = null;
            Look active = new Look();
            bool showCursor = false;
            if (Session.IsPowered && Session.HasOwner && ownerHit)
            {
                active = ownerLook;
                showCursor = true;
                hover = Hit(ownerLook.VirtualX, ownerLook.VirtualY);
            }
            else if (Session.IsPowered && !Session.HasOwner && previewHit)
            {
                active = preview;
                showCursor = true;
                hover = Hit(preview.VirtualX, preview.VirtualY);
            }

            if (!ReferenceEquals(Session.Hover, hover))
            {
                Session.Hover = hover;
                Session.Touch();
            }

            TickCaret(delta);
            if (Session.IsPowered && Session.Program != null && !Session.Faulted)
            {
                try
                {
                    Session.Program.Tick(delta);
                }
                catch (Exception ex)
                {
                    Fault(ex);
                }
            }

            if (Session.LayoutDirty || Session.VisualDirty)
                Redraw();

            _visuals.SyncCursor(_pose, showCursor && Session.IsPowered, active.Right, active.Up, Config);
        }

        public void Press(Player player, Look look)
        {
            if (_closed || !Session.IsPowered || player == null || !look.Hit)
                return;

            if (Session.HasOwner && Session.OwnerId != player.PlayerId)
            {
                MonitorServer.Deny(player);
                return;
            }

            if (!Session.HasOwner)
                Claim(player);

            Widget hit = Hit(look.VirtualX, look.VirtualY);
            Session.Pressed = hit;
            Session.Touch();
            try
            {
                Activate(hit, look);
            }
            catch (Exception ex)
            {
                Fault(ex);
            }
        }

        public void ReleasePress()
        {
            if (Session.Pressed == null)
                return;

            Session.Pressed = null;
            Session.Touch();
        }

        public void SetPower(bool on)
        {
            if (Session.IsPowered == on)
                return;

            Session.IsPowered = on;
            if (!on)
            {
                Release();
                Session.CloseField();
            }

            Session.Invalidate();
            try
            {
                if (Session.Program != null)
                    Session.Program.OnPower(on);
            }
            catch (Exception ex)
            {
                Fault(ex);
            }

            Session.RaisePower(on);
        }

        public void Release()
        {
            if (!Session.HasOwner && Session.Owner == null)
                return;

            Session.Owner = null;
            Session.OwnerId = 0;
            Session.Away = 0f;
            Session.Hover = null;
            Session.Pressed = null;
            Session.CloseField();
            Session.RaiseReleased();
        }

        public void Destroy()
        {
            if (_closed)
                return;

            _closed = true;
            if (Session.Program != null)
            {
                try
                {
                    Session.Program.Close();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            _visuals.Destroy();
        }

        public void LoadProgram()
        {
            if (Session.Program != null)
            {
                try
                {
                    Session.Program.Close();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                Session.Program = null;
            }

            Session.Faulted = false;
            Session.Root = null;
            Session.CloseField();
            MonitorProgram program;
            if (!ZacsApi.TryCreate(Session.MonitorName, Session, out program))
            {
                Session.Root = Missing(Session.MonitorName);
                Session.Invalidate();
                return;
            }

            program.Session = Session;
            Session.Program = program;
            try
            {
                program.Open();
            }
            catch (Exception ex)
            {
                Fault(ex);
                return;
            }

            if (Session.Root == null)
                Session.Root = Missing(Session.MonitorName);

            Session.Invalidate();
        }

        void Claim(Player player)
        {
            Session.Owner = player;
            Session.OwnerId = player.PlayerId;
            Session.Away = 0f;
            Session.RaiseClaimed(player);
        }

        void Activate(Widget hit, Look look)
        {
            KeyCap key = hit as KeyCap;
            if (key != null)
            {
                Type(key);
                return;
            }

            if (!(hit is TextField) && Session.KeyboardOpen)
                Session.CloseField();

            TextField field = hit as TextField;
            if (field != null)
            {
                Session.Focused = field;
                Session.KeyboardOpen = true;
                Session.Shift = false;
                Session.Invalidate();
                return;
            }

            Slider slider = hit as Slider;
            if (slider != null)
            {
                float span = slider.TrackMax - slider.TrackMin;
                if (span > 0.001f)
                    slider.SetNormalized(Session, (look.VirtualX - slider.TrackMin) / span);

                Session.Touch();
                return;
            }

            Toggle toggle = hit as Toggle;
            if (toggle != null)
            {
                toggle.SetValue(Session, !toggle.Value);
                Session.Touch();
                return;
            }

            Button button = hit as Button;
            if (button != null && button.Enabled && button.Click != null)
            {
                try
                {
                    button.Click(Session, button);
                }
                catch (Exception ex)
                {
                    Fault(ex);
                }
            }
        }

        void Type(KeyCap key)
        {
            if (key.Kind == KeyKind.Shift)
            {
                Session.Shift = !Session.Shift;
                PaintKeys();
                Session.Touch();
                return;
            }

            TextField field = Session.Focused;
            if (field == null)
                return;

            if (key.Kind == KeyKind.Backspace)
                field.Backspace();
            else if (key.Kind == KeyKind.Submit)
            {
                field.SubmitValue(Session);
                Session.CloseField();
                return;
            }
            else
            {
                string value = key.Kind == KeyKind.Space ? " " : key.Value;
                if (Session.Shift && key.Kind == KeyKind.Character)
                    value = Shift(value);

                field.Type(value, Config.FieldLimit);
            }

            Session.Touch();
        }

        void TickCaret(float delta)
        {
            if (Session.Focused == null)
                return;

            _caretTime += delta;
            if (_caretTime < 0.45f)
                return;

            _caretTime = 0f;
            _caretOn = !_caretOn;
            Session.Touch();
        }

        void Redraw()
        {
            if (!_pose.Valid)
                return;

            float keyboardH = 0f;
            if (Session.KeyboardOpen)
            {
                EnsureKeyboard();
                keyboardH = 292f;
                float limit = _pose.VirtualH * 0.48f;
                if (keyboardH > limit)
                    keyboardH = limit;

                _keyboard.Arrange(0f, _pose.VirtualH - keyboardH, _pose.VirtualW, keyboardH, 0);
            }

            if (Session.Root != null)
                Session.Root.Arrange(0f, 0f, _pose.VirtualW, Mathf.Max(1f, _pose.VirtualH - keyboardH), 0);

            if (Session.Root != null)
                Session.Root.EnsureIds(ref Session.NextId, 0);

            if (_keyboard != null)
                _keyboard.EnsureIds(ref Session.NextId, 0);

            Draw draw = _visuals.Begin(_pose, Config, Session.Hover, Session.Pressed, Session.Focused, _caretOn);
            if (Session.IsPowered && Session.Root != null)
                Session.Root.Collect(draw, 0);

            if (Session.IsPowered && Session.KeyboardOpen && _keyboard != null)
                _keyboard.Collect(draw, 12);

            _visuals.Sync(_pose, Config, Session.IsPowered);
            Session.LayoutDirty = false;
            Session.VisualDirty = false;
        }

        Widget Hit(float x, float y)
        {
            if (Session.KeyboardOpen && _keyboard != null)
            {
                Widget key = HitWidget(_keyboard, x, y, 0);
                if (key != null)
                    return key;
            }

            if (Session.Root == null)
                return null;

            return HitWidget(Session.Root, x, y, 0);
        }

        static Widget HitWidget(Widget widget, float x, float y, int depth)
        {
            if (widget == null || !widget.Visible || depth > 24 || !widget.Box.Contains(x, y))
                return null;

            List<Widget> children = widget.Children;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                Widget hit = HitWidget(children[i], x, y, depth + 1);
                if (hit != null)
                    return hit;
            }

            if (widget.Interactive)
                return widget;

            return null;
        }

        bool PickPreview(Player[] players, int count, out Look best)
        {
            best = new Look();
            float bestDistance = float.MaxValue;
            bool found = false;
            if (_preview != null)
            {
                Look held;
                if (_preview.IsAlive && _preview.Camera != null && _pose.TryHit(_preview.Camera.position, _preview.Camera.forward, Reach(Config.MaxDistance), out held))
                {
                    best = held;
                    bestDistance = held.Distance;
                    found = true;
                }
                else
                    _preview = null;
            }

            for (int i = 0; i < count; i++)
            {
                Player player = players[i];
                Look look;
                if (player == null || player.Camera == null || !_pose.TryHit(player.Camera.position, player.Camera.forward, Reach(Config.MaxDistance), out look))
                    continue;

                if (!found || look.Distance + 0.35f < bestDistance)
                {
                    best = look;
                    bestDistance = look.Distance;
                    _preview = player;
                    found = true;
                }
            }

            return found;
        }

        void EnsureKeyboard()
        {
            if (_keyboard != null)
                return;

            _keyboard = new Flow();
            _keyboard.Horizontal = false;
            _keyboard.Gap = 6f;
            _keyboard.Pad = Pad.Symmetric(10f, 8f);
            _keyboard.Background = Palette.Plate;
            _keyboard.Add(KeyRow(DigitRow));
            _keyboard.Add(KeyRow(RowQ));
            _keyboard.Add(KeyRow(RowA));
            _keyboard.Add(KeyRow(RowZ));
            _keyboard.Add(CommandRow());
            PaintKeys();
        }

        Flow KeyRow(string[] values)
        {
            Flow row = new Flow();
            row.Horizontal = true;
            row.Gap = 6f;
            row.Flex = 1f;
            row.HeightUnit = LengthUnit.Flex;
            for (int i = 0; i < values.Length; i++)
            {
                KeyCap key = Cap(values[i], values[i], KeyKind.Character);
                key.Flex = 1f;
                row.Add(key);
                _keys.Add(key);
            }

            return row;
        }

        Flow CommandRow()
        {
            Flow row = new Flow();
            row.Horizontal = true;
            row.Gap = 6f;
            row.Flex = 1f;
            row.HeightUnit = LengthUnit.Flex;
            KeyCap shift = Cap("Shift", "shift", KeyKind.Shift);
            shift.Flex = 1.4f;
            KeyCap space = Cap("Space", " ", KeyKind.Space);
            space.Flex = 4f;
            KeyCap back = Cap("Back", "backspace", KeyKind.Backspace);
            back.Flex = 1.4f;
            KeyCap enter = Cap("Enter", "enter", KeyKind.Submit);
            enter.Flex = 1.6f;
            enter.Background = Palette.BrassDim;
            enter.Hover = Palette.Brass;
            row.Add(shift);
            row.Add(space);
            row.Add(back);
            row.Add(enter);
            _keys.Add(shift);
            _keys.Add(space);
            _keys.Add(back);
            _keys.Add(enter);
            return row;
        }

        KeyCap Cap(string label, string value, KeyKind kind)
        {
            KeyCap key = new KeyCap();
            key.Text = label;
            key.Value = value;
            key.Kind = kind;
            key.FontSize = 22f;
            key.FillCross = true;
            key.Background = Palette.Raised;
            key.Hover = Button.Lighten(Palette.Raised);
            return key;
        }

        void PaintKeys()
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                KeyCap key = _keys[i];
                if (key.Kind == KeyKind.Character)
                    key.Text = Session.Shift ? Shift(key.Value) : key.Value;
                else if (key.Kind == KeyKind.Shift)
                    key.Background = Session.Shift ? Palette.BrassDim : Palette.Raised;
            }
        }

        static string Shift(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value == "i")
                return "I";

            char c = value[0];
            if (c >= 'a' && c <= 'z')
                return ((char)(c - 32)).ToString();

            return value;
        }

        static Widget Missing(string name)
        {
            return Zml.Column(
                Zml.Text(string.IsNullOrEmpty(name) ? "ZACS" : name).Font(40f).Paint(Palette.Brass),
                Zml.Text("Program is not registered.").Font(24f).Paint(Palette.Muted))
                .Pad(36f)
                .Gap(14f)
                .Back(Palette.Background);
        }

        void Fault(Exception ex)
        {
            Session.Faulted = true;
            Log.Error(ex);
            Session.Root = Zml.Text("Program stopped.").Font(32f).Paint(Palette.Danger).Pad(28f);
            Session.CloseField();
            Session.Invalidate();
        }

        float Reach(float distance)
        {
            if (distance < 0.4f)
                return 0.4f;

            if (distance > 12f)
                return 12f;

            return distance;
        }

        static Player FindPlayer(Player[] players, int count, int playerId)
        {
            for (int i = 0; i < count; i++)
            {
                Player player = players[i];
                if (player != null && player.PlayerId == playerId)
                    return player;
            }

            return null;
        }
    }
}
