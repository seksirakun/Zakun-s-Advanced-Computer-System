using UnityEngine;

namespace Zacs
{
    public enum LengthUnit
    {
        Auto = 0,
        Pixel = 1,
        Percent = 2,
        Flex = 3
    }

    public enum TextAlign
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    public struct Pad
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public float Horizontal
        {
            get { return Left + Right; }
        }

        public float Vertical
        {
            get { return Top + Bottom; }
        }

        public static Pad All(float value)
        {
            return new Pad
            {
                Left = value,
                Top = value,
                Right = value,
                Bottom = value
            };
        }

        public static Pad Symmetric(float horizontal, float vertical)
        {
            return new Pad
            {
                Left = horizontal,
                Top = vertical,
                Right = horizontal,
                Bottom = vertical
            };
        }
    }

    public struct Box
    {
        public float X;
        public float Y;
        public float W;
        public float H;

        public float CenterX
        {
            get { return X + W * 0.5f; }
        }

        public float CenterY
        {
            get { return Y + H * 0.5f; }
        }

        public bool Contains(float x, float y)
        {
            return x >= X && y >= Y && x < X + W && y < Y + H;
        }
    }

    public static class Palette
    {
        public static readonly Color Background = new Color(0.07f, 0.08f, 0.10f, 1f);
        public static readonly Color Plate = new Color(0.11f, 0.13f, 0.16f, 1f);
        public static readonly Color Raised = new Color(0.20f, 0.23f, 0.28f, 1f);
        public static readonly Color Line = new Color(0.220f, 0.260f, 0.310f, 1f);
        public static readonly Color Text = new Color(0.910f, 0.930f, 0.950f, 1f);
        public static readonly Color Muted = new Color(0.560f, 0.610f, 0.670f, 1f);
        public static readonly Color Brass = new Color(0.855f, 0.690f, 0.365f, 1f);
        public static readonly Color BrassDim = new Color(0.420f, 0.330f, 0.160f, 1f);
        public static readonly Color Field = new Color(0.040f, 0.048f, 0.060f, 1f);
        public static readonly Color Ok = new Color(0.400f, 0.680f, 0.500f, 1f);
        public static readonly Color Danger = new Color(0.730f, 0.310f, 0.290f, 1f);
        public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    }

    internal struct PlateReq
    {
        public int Key;
        public float X;
        public float Y;
        public float W;
        public float H;
        public int Layer;
        public Color Color;
    }

    internal struct GlyphReq
    {
        public int Key;
        public float X;
        public float Y;
        public float W;
        public float H;
        public int Layer;
        public string Text;
    }

    internal sealed class Draw
    {
        public Widget Hover;
        public Widget Pressed;
        public TextField Focused;
        public bool CaretOn;
        public float TmpScale = 1f;
        public System.Collections.Generic.List<PlateReq> Plates;
        public System.Collections.Generic.List<GlyphReq> Glyphs;
    }

    public enum MonitorFace
    {
        Forward = 0,
        Back = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        Down = 5
    }
}
