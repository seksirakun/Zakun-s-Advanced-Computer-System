using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Zacs
{
    public class Widget
    {
        const int MaxDepth = 24;

        public string Name;
        public object Tag;
        public string Text = string.Empty;
        public float Width;
        public float Height;
        public LengthUnit WidthUnit = LengthUnit.Auto;
        public LengthUnit HeightUnit = LengthUnit.Auto;
        public float Flex;
        public float Gap;
        public float FontSize = 28f;
        public Pad Pad;
        public Color Background = Palette.Clear;
        public Color Foreground = Palette.Text;
        public TextAlign Align = TextAlign.Left;
        public bool Visible = true;
        public bool Enabled = true;
        public bool FillCross = true;

        public Widget Parent { get; internal set; }

        public List<Widget> Children { get; private set; }

        internal int Id;
        internal Box Box;

        public Widget()
        {
            Children = new List<Widget>(4);
        }

        public virtual Widget Add(Widget child)
        {
            if (child == null || ReferenceEquals(child, this))
                return this;

            if (child.Parent != null)
                child.Parent.Remove(child);

            child.Parent = this;
            Children.Add(child);
            return this;
        }

        public void Remove(Widget child)
        {
            if (child == null)
                return;

            if (Children.Remove(child) && ReferenceEquals(child.Parent, this))
                child.Parent = null;
        }

        public Widget Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return Find(this, name, 0);
        }

        public virtual float PreferredHeight(float width)
        {
            float inner = width - Pad.Horizontal;
            if (inner < 8f)
                inner = 8f;

            int lines = CountLines(WrapToWidth(Text, FontSize, inner));
            float height = FontSize * 1.28f * lines + Pad.Vertical;
            return height < FontSize + Pad.Vertical ? FontSize + Pad.Vertical : height;
        }

        public virtual float PreferredWidth(float height)
        {
            return EstimateWidth(Text, FontSize) + Pad.Horizontal + 8f;
        }

        internal virtual bool Interactive
        {
            get { return false; }
        }

        internal virtual void Arrange(float x, float y, float w, float h, int depth)
        {
            if (w < 0f)
                w = 0f;
            if (h < 0f)
                h = 0f;

            Box = new Box { X = x, Y = y, W = w, H = h };
        }

        internal virtual void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null)
                return;

            EmitPlate(draw, 0, Box, Stack(depth, 0), PlateColor(draw));
            EmitText(draw, 1, Box, Stack(depth, 1), BuildText(draw));
        }

        internal void EnsureIds(ref int next, int depth)
        {
            if (depth > MaxDepth)
                return;

            if (Id == 0)
            {
                Id = next;
                next++;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child != null)
                    child.EnsureIds(ref next, depth + 1);
            }
        }

        internal int Key(int slot)
        {
            return Id * 8 + slot;
        }

        protected float LineHeight()
        {
            int lines = 1;
            if (!string.IsNullOrEmpty(Text))
            {
                for (int i = 0; i < Text.Length; i++)
                {
                    if (Text[i] == '\n')
                        lines++;
                }
            }

            return FontSize * 1.35f * lines;
        }

        internal virtual Color PlateColor(Draw draw)
        {
            return Background;
        }

        internal float DrawnSize(Draw draw)
        {
            if (draw != null && draw.TmpScale > 0f)
                return FontSize * draw.TmpScale;

            return FontSize;
        }

        internal virtual string BuildText(Draw draw)
        {
            if (string.IsNullOrEmpty(Text))
                return null;

            float size = FitFont(Text, DrawnSize(draw), Box.W, Box.H, Text.Length <= 18 && Text.IndexOf('\n') < 0);
            string body = Text.Length <= 18 && Text.IndexOf('\n') < 0
                ? Text
                : WrapToWidth(Text, size, Box.W * 0.94f);
            return RichText.Format(body, Foreground, size, Align, true);
        }

        internal static int Stack(int depth, int local)
        {
            int layer = depth + local;
            if (layer < 0)
                return 0;
            if (layer > 28)
                return 28;

            return layer;
        }

        internal void EmitPlate(Draw draw, int slot, Box box, int layer, Color color)
        {
            if (color.a <= 0.01f || box.W <= 0.5f || box.H <= 0.5f)
                return;

            draw.Plates.Add(new PlateReq
            {
                Key = Key(slot),
                X = box.X,
                Y = box.Y,
                W = box.W,
                H = box.H,
                Layer = layer,
                Color = color
            });
        }

        internal void EmitText(Draw draw, int slot, Box box, int layer, string text)
        {
            if (string.IsNullOrEmpty(text) || box.W <= 0.5f || box.H <= 0.5f)
                return;

            draw.Glyphs.Add(new GlyphReq
            {
                Key = Key(slot),
                X = box.X,
                Y = box.Y,
                W = box.W,
                H = box.H,
                Layer = layer,
                Text = text
            });
        }

        protected static float EstimateWidth(string text, float font)
        {
            if (string.IsNullOrEmpty(text) || font <= 0f)
                return 0f;

            float units = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                    continue;

                if (c == ' ' || c == '.' || c == ',' || c == ':' || c == 'i' || c == 'l' || c == 'ı')
                    units += 0.34f;
                else if (c == 'm' || c == 'w' || c == 'M' || c == 'W' || c == 'ğ' || c == 'ş')
                    units += 0.78f;
                else
                    units += 0.72f;
            }

            return units * font;
        }

        static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            int lines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    lines++;
            }

            return lines;
        }

        protected static float FitFont(string text, float font, float maxWidth, float maxHeight, bool oneLine)
        {
            float size = font;
            if (maxHeight > 4f && size > maxHeight * 0.68f)
                size = maxHeight * 0.68f;

            if (oneLine && maxWidth > 8f && !string.IsNullOrEmpty(text))
            {
                float need = EstimateWidth(text, size);
                if (need > maxWidth * 0.94f)
                    size *= (maxWidth * 0.94f) / need;
            }

            if (size < 14f)
                size = 14f;

            return size;
        }

        protected static string WrapToWidth(string text, float font, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth < 8f || text.IndexOf('\n') >= 0)
                return text;

            string[] words = text.Split(' ');
            System.Text.StringBuilder line = new System.Text.StringBuilder();
            System.Text.StringBuilder all = new System.Text.StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (word.Length == 0)
                    continue;

                if (line.Length == 0)
                {
                    line.Append(word);
                    continue;
                }

                string trial = line + " " + word;
                if (EstimateWidth(trial, font) <= maxWidth)
                {
                    line.Append(' ');
                    line.Append(word);
                }
                else
                {
                    if (all.Length > 0)
                        all.Append('\n');

                    all.Append(line);
                    line.Length = 0;
                    line.Append(word);
                }
            }

            if (line.Length > 0)
            {
                if (all.Length > 0)
                    all.Append('\n');

                all.Append(line);
            }

            return all.Length == 0 ? text : all.ToString();
        }

        static Widget Find(Widget widget, string name, int depth)
        {
            if (widget == null || depth > MaxDepth)
                return null;

            if (string.Equals(widget.Name, name, System.StringComparison.Ordinal))
                return widget;

            List<Widget> children = widget.Children;
            for (int i = 0; i < children.Count; i++)
            {
                Widget found = Find(children[i], name, depth + 1);
                if (found != null)
                    return found;
            }

            return null;
        }
    }

    public sealed class Flow : Widget
    {
        public bool Horizontal;

        public override float PreferredHeight(float width)
        {
            if (HeightUnit == LengthUnit.Pixel)
                return Height;

            float inner = Mathf.Max(0f, width - Pad.Horizontal);
            if (Horizontal)
            {
                float max = 0f;
                for (int i = 0; i < Children.Count; i++)
                {
                    Widget child = Children[i];
                    if (child == null || !child.Visible)
                        continue;

                    float h = child.PreferredHeight(inner);
                    if (h > max)
                        max = h;
                }

                return max + Pad.Vertical;
            }

            float sum = Pad.Vertical;
            int count = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child == null || !child.Visible)
                    continue;

                count++;
                sum += MainPreferred(child, false, inner);
            }

            if (count > 1)
                sum += Gap * (count - 1);

            return sum;
        }

        public override float PreferredWidth(float height)
        {
            if (WidthUnit == LengthUnit.Pixel)
                return Width;

            float inner = Mathf.Max(0f, height - Pad.Vertical);
            if (!Horizontal)
            {
                float max = 0f;
                for (int i = 0; i < Children.Count; i++)
                {
                    Widget child = Children[i];
                    if (child == null || !child.Visible)
                        continue;

                    float w = child.PreferredWidth(inner);
                    if (w > max)
                        max = w;
                }

                return max + Pad.Horizontal;
            }

            float sum = Pad.Horizontal;
            int count = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child == null || !child.Visible)
                    continue;

                count++;
                sum += MainPreferred(child, true, inner);
            }

            if (count > 1)
                sum += Gap * (count - 1);

            return sum;
        }

        internal override void Arrange(float x, float y, float w, float h, int depth)
        {
            base.Arrange(x, y, w, h, depth);
            if (!Visible || depth > 24)
                return;

            float innerW = Mathf.Max(0f, w - Pad.Horizontal);
            float innerH = Mathf.Max(0f, h - Pad.Vertical);
            int count = 0;
            float fixedMain = 0f;
            float flexWeight = 0f;

            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child == null || !child.Visible)
                    continue;

                count++;
                float weight = Weight(child, Horizontal);
                if (weight > 0f)
                    flexWeight += weight;
                else
                    fixedMain += ResolveMain(child, Horizontal, innerW, innerH);
            }

            float gaps = count > 1 ? Gap * (count - 1) : 0f;
            float innerMain = Horizontal ? innerW : innerH;
            float free = innerMain - fixedMain - gaps;
            if (free < 0f)
                free = 0f;

            float pen = Horizontal ? x + Pad.Left : y + Pad.Top;
            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child == null || !child.Visible)
                    continue;

                float weight = Weight(child, Horizontal);
                float main;
                if (weight > 0f && flexWeight > 0f)
                    main = free * (weight / flexWeight);
                else if (weight > 0f)
                    main = 0f;
                else
                    main = ResolveMain(child, Horizontal, innerW, innerH);

                if (main < 0f)
                    main = 0f;

                float cross = CrossSize(child, Horizontal, Horizontal ? innerH : innerW);
                float crossOrigin = Horizontal ? y + Pad.Top : x + Pad.Left;
                float crossSpace = Horizontal ? innerH : innerW;
                if (cross > crossSpace)
                    cross = crossSpace;

                if (child.FillCross && CrossUnit(child, Horizontal) == LengthUnit.Auto)
                    cross = crossSpace;
                else
                    crossOrigin += (crossSpace - cross) * 0.5f;

                if (Horizontal)
                    child.Arrange(pen, crossOrigin, main, cross, depth + 1);
                else
                    child.Arrange(crossOrigin, pen, cross, main, depth + 1);

                pen += main + Gap;
            }
        }

        internal override void Collect(Draw draw, int depth)
        {
            if (!Visible || draw == null || depth > 24)
                return;

            EmitPlate(draw, 0, Box, Stack(depth, 0), Background);
            for (int i = 0; i < Children.Count; i++)
            {
                Widget child = Children[i];
                if (child != null)
                    child.Collect(draw, depth + 1);
            }
        }

        static float Weight(Widget child, bool horizontal)
        {
            LengthUnit unit = horizontal ? child.WidthUnit : child.HeightUnit;
            if (child.Flex > 0f && (unit == LengthUnit.Auto || unit == LengthUnit.Flex))
                return child.Flex;

            if (unit == LengthUnit.Flex)
                return child.Flex > 0f ? child.Flex : 1f;

            return 0f;
        }

        static float MainPreferred(Widget child, bool horizontal, float cross)
        {
            if (horizontal)
                return child.PreferredWidth(cross);

            return child.PreferredHeight(cross);
        }

        static float ResolveMain(Widget child, bool horizontal, float innerW, float innerH)
        {
            LengthUnit unit = horizontal ? child.WidthUnit : child.HeightUnit;
            float value = horizontal ? child.Width : child.Height;
            float innerMain = horizontal ? innerW : innerH;
            float innerCross = horizontal ? innerH : innerW;
            if (unit == LengthUnit.Pixel)
                return value;

            if (unit == LengthUnit.Percent)
                return innerMain * value * 0.01f;

            if (horizontal)
                return child.PreferredWidth(innerCross);

            return child.PreferredHeight(innerCross);
        }

        static float CrossSize(Widget child, bool horizontal, float innerCross)
        {
            LengthUnit unit = CrossUnit(child, horizontal);
            float value = horizontal ? child.Height : child.Width;
            if (unit == LengthUnit.Pixel)
                return value;

            if (unit == LengthUnit.Percent)
                return innerCross * value * 0.01f;

            if (horizontal)
                return child.PreferredHeight(innerCross);

            return child.PreferredWidth(innerCross);
        }

        static LengthUnit CrossUnit(Widget child, bool horizontal)
        {
            return horizontal ? child.HeightUnit : child.WidthUnit;
        }
    }

    public static class RichText
    {
        public static string Format(string text, Color color, float size, TextAlign align, bool escape)
        {
            string body = text ?? string.Empty;
            if (escape)
                body = Escape(body);

            string alignName = "left";
            if (align == TextAlign.Center)
                alignName = "center";
            else if (align == TextAlign.Right)
                alignName = "right";

            if (size < 1f)
                size = 1f;

            return "<align=" + alignName + "><size=" + size.ToString("0.##", CultureInfo.InvariantCulture) + "><color=#" + Hex(color) + ">" + body + "</color></size></align>";
        }

        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.IndexOf('<') < 0 && text.IndexOf('>') < 0)
                return text;

            return text.Replace("<", "‹").Replace(">", "›");
        }

        public static string Hex(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }
    }
}
