using System;
using UnityEngine;

namespace Zacs
{
    public class Button : Widget
    {
        public Action<MonitorSession, Button> Click;
        public Color Hover = Palette.Raised;
        public Color Pressed = Palette.BrassDim;

        public Button()
        {
            Background = Palette.Raised;
            Foreground = Palette.Text;
            Align = TextAlign.Center;
            Pad = Pad.Symmetric(16f, 8f);
            FontSize = 26f;
            Hover = Lighten(Background);
        }

        public override float PreferredHeight(float width)
        {
            float text = LineHeight() + Pad.Vertical;
            return text < 46f ? 46f : text;
        }

        public override float PreferredWidth(float height)
        {
            return EstimateWidth(Text, FontSize) + Pad.Horizontal + 20f;
        }

        internal override bool Interactive
        {
            get { return Enabled; }
        }

        internal override Color PlateColor(Draw draw)
        {
            Color color = Background;
            if (!Enabled)
                return Dim(color, 0.45f);

            if (draw != null && ReferenceEquals(draw.Pressed, this))
                color = Pressed;
            else if (draw != null && ReferenceEquals(draw.Hover, this))
                color = Hover.a > 0.01f ? Hover : Lighten(Background);

            return color;
        }

        internal static Color Lighten(Color color)
        {
            return new Color(
                color.r + (1f - color.r) * 0.16f,
                color.g + (1f - color.g) * 0.16f,
                color.b + (1f - color.b) * 0.16f,
                color.a);
        }

        internal static Color Dim(Color color, float amount)
        {
            return new Color(color.r * amount, color.g * amount, color.b * amount, color.a);
        }
    }

    public sealed class Toggle : Widget
    {
        public bool Value;
        public Action<MonitorSession, Toggle> Changed;

        public Toggle()
        {
            FontSize = 26f;
            Foreground = Palette.Text;
            Align = TextAlign.Left;
        }

        public override float PreferredHeight(float width)
        {
            float text = LineHeight();
            return (text < 40f ? 40f : text) + Pad.Vertical;
        }

        internal override bool Interactive
        {
            get { return Enabled; }
        }

        internal void SetValue(MonitorSession session, bool value)
        {
            if (Value == value)
                return;

            Value = value;
            if (Changed != null && session != null)
                Changed(session, this);
        }

        internal override void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null)
                return;

            float box = Box.H < 28f ? Box.H : 28f;
            if (box < 16f)
                box = 16f;

            float y = Box.Y + (Box.H - box) * 0.5f;
            Box mark = new Box { X = Box.X, Y = y, W = box, H = box };
            EmitPlate(draw, 0, mark, Stack(depth, 1), Enabled ? Palette.Field : Button.Dim(Palette.Field, 0.5f));
            if (Value)
            {
                float inset = box * 0.22f;
                EmitPlate(draw, 1, new Box
                {
                    X = mark.X + inset,
                    Y = mark.Y + inset,
                    W = mark.W - inset * 2f,
                    H = mark.H - inset * 2f
                }, Stack(depth, 2), Palette.Brass);
            }

            Box label = new Box
            {
                X = Box.X + box + 12f,
                Y = Box.Y,
                W = Mathf.Max(0f, Box.W - box - 12f),
                H = Box.H
            };
            EmitText(draw, 2, label, Stack(depth, 3), BuildText(draw));
        }
    }

    public sealed class Slider : Widget
    {
        public float Min;
        public float Max = 1f;
        public float Value = 1f;
        public bool ShowValue = true;
        public Action<MonitorSession, Slider> Changed;

        internal float TrackMin;
        internal float TrackMax;

        public Slider()
        {
            FontSize = 22f;
            Foreground = Palette.Muted;
        }

        public override float PreferredHeight(float width)
        {
            return 36f + Pad.Vertical;
        }

        internal override bool Interactive
        {
            get { return Enabled; }
        }

        public float Normalized
        {
            get
            {
                float span = Max - Min;
                if (Mathf.Abs(span) < 0.0001f)
                    return 0f;

                return Mathf.Clamp01((Value - Min) / span);
            }
        }

        internal void SetNormalized(MonitorSession session, float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            float next = Min + (Max - Min) * clamped;
            if (Mathf.Abs(next - Value) < 0.0005f)
                return;

            Value = next;
            if (Changed != null && session != null)
                Changed(session, this);
        }

        internal override void Arrange(float x, float y, float w, float h, int depth)
        {
            base.Arrange(x, y, w, h, depth);
            float label = ShowValue ? 72f : 0f;
            TrackMin = x + Pad.Left;
            TrackMax = x + w - Pad.Right - label;
            if (TrackMax < TrackMin)
                TrackMax = TrackMin;
        }

        internal override void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null)
                return;

            float trackH = 8f;
            float y = Box.Y + (Box.H - trackH) * 0.5f;
            float trackW = TrackMax - TrackMin;
            EmitPlate(draw, 0, new Box { X = TrackMin, Y = y, W = trackW, H = trackH }, Stack(depth, 1), Palette.Field);
            float fill = trackW * Normalized;
            if (fill > 0.5f)
                EmitPlate(draw, 1, new Box { X = TrackMin, Y = y, W = fill, H = trackH }, Stack(depth, 2), Palette.Brass);

            float knob = 18f;
            float knobX = TrackMin + fill - knob * 0.5f;
            if (knobX < TrackMin)
                knobX = TrackMin;
            if (knobX + knob > TrackMax)
                knobX = TrackMax - knob;

            EmitPlate(draw, 2, new Box
            {
                X = knobX,
                Y = Box.Y + (Box.H - knob) * 0.5f,
                W = knob,
                H = knob
            }, Stack(depth, 3), Palette.Text);

            if (ShowValue)
            {
                EmitText(draw, 3, new Box
                {
                    X = TrackMax + 8f,
                    Y = Box.Y,
                    W = 64f,
                    H = Box.H
                }, Stack(depth, 4), RichText.Format(Value.ToString("0.##"), Palette.Muted, DrawnSize(draw), TextAlign.Right, true));
            }
        }
    }

    public sealed class Progress : Widget
    {
        public float Value = 1f;

        public Progress()
        {
            FontSize = 20f;
        }

        public override float PreferredHeight(float width)
        {
            return 16f + Pad.Vertical;
        }

        internal override void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null)
                return;

            float h = Box.H < 10f ? Box.H : 10f;
            float y = Box.Y + (Box.H - h) * 0.5f;
            EmitPlate(draw, 0, new Box { X = Box.X, Y = y, W = Box.W, H = h }, Stack(depth, 1), Palette.Field);
            float fill = Box.W * Mathf.Clamp01(Value);
            if (fill > 0.5f)
                EmitPlate(draw, 1, new Box { X = Box.X, Y = y, W = fill, H = h }, Stack(depth, 2), Palette.Ok);
        }
    }

    public sealed class TextField : Widget
    {
        public string Value = string.Empty;
        public string Placeholder = string.Empty;
        public Action<MonitorSession, string> Submit;

        public TextField()
        {
            Background = Palette.Field;
            Foreground = Palette.Text;
            FontSize = 26f;
            Pad = Pad.Symmetric(12f, 8f);
            Align = TextAlign.Left;
        }

        public override float PreferredHeight(float width)
        {
            return 48f + Pad.Vertical;
        }

        internal override bool Interactive
        {
            get { return Enabled; }
        }

        internal void Type(string text, int limit)
        {
            if (text == null)
                return;

            if (limit < 1)
                limit = 1;

            if (Value == null)
                Value = string.Empty;

            if (Value.Length >= limit)
                return;

            int room = limit - Value.Length;
            Value = room >= text.Length ? Value + text : Value + text.Substring(0, room);
        }

        internal void Backspace()
        {
            if (string.IsNullOrEmpty(Value))
                return;

            Value = Value.Substring(0, Value.Length - 1);
        }

        internal void SubmitValue(MonitorSession session)
        {
            if (Submit != null && session != null)
                Submit(session, Value ?? string.Empty);
        }

        internal override string BuildText(Draw draw)
        {
            bool focused = draw != null && ReferenceEquals(draw.Focused, this);
            string shown;
            Color color;
            if (string.IsNullOrEmpty(Value))
            {
                shown = Placeholder ?? string.Empty;
                color = Palette.Muted;
            }
            else
            {
                shown = Value;
                color = Foreground;
            }

            if (focused && draw.CaretOn)
                shown = (shown ?? string.Empty) + "|";

            if (string.IsNullOrEmpty(shown))
                return null;

            float size = FitFont(shown, DrawnSize(draw), Box.W - Pad.Horizontal, Box.H - Pad.Vertical, true);
            return RichText.Format(shown, color, size, Align, true);
        }
    }

    public sealed class Window : Widget
    {
        public readonly Widget Title;
        public readonly Flow Body;

        public Window(string title)
        {
            Background = Palette.Plate;
            Title = new Widget();
            Title.Text = title ?? string.Empty;
            Title.FontSize = 48f;
            Title.Foreground = Palette.Brass;
            Title.Align = TextAlign.Center;
            Title.FillCross = true;
            Body = new Flow();
            Body.Horizontal = false;
            Body.Gap = 12f;
            Body.Pad = Pad.All(18f);
            Children.Add(Title);
            Children.Add(Body);
        }

        public override Widget Add(Widget child)
        {
            Body.Add(child);
            return this;
        }

        internal override void Arrange(float x, float y, float w, float h, int depth)
        {
            Box = new Box { X = x, Y = y, W = w < 0f ? 0f : w, H = h < 0f ? 0f : h };
            if (!Visible || depth > 24)
                return;

            const float titleH = 72f;
            float bar = titleH;
            if (bar > Box.H)
                bar = Box.H;

            Title.Arrange(x, y, Box.W, bar, depth + 1);
            Body.Arrange(x, y + bar, Box.W, Box.H - bar, depth + 1);
        }

        internal override void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null)
                return;

            EmitPlate(draw, 0, Box, Stack(depth, 0), Background);
            float bar = Title.Box.H;
            if (bar > 1f)
            {
                EmitPlate(draw, 1, new Box { X = Box.X, Y = Box.Y, W = Box.W, H = bar }, Stack(depth, 1), Palette.Raised);
            }

            Title.Collect(draw, depth + 1);
            Body.Collect(draw, depth + 1);
        }
    }

    internal sealed class KeyCap : Button
    {
        public string Value;
        public KeyKind Kind;
    }

    internal enum KeyKind
    {
        Character = 0,
        Backspace = 1,
        Submit = 2,
        Shift = 3,
        Space = 4
    }
}
