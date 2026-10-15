using System;
using UnityEngine;

namespace Zacs
{
    public static class Zml
    {
        public static Flow Column(params Widget[] children)
        {
            return Stack(false, children);
        }

        public static Flow Row(params Widget[] children)
        {
            return Stack(true, children);
        }

        public static Widget Text(string text)
        {
            Widget widget = new Widget();
            widget.Text = text ?? string.Empty;
            widget.Foreground = Palette.Text;
            widget.Align = TextAlign.Left;
            widget.FillCross = true;
            return widget;
        }

        public static Button Button(string text, Action<MonitorSession, Button> click)
        {
            global::Zacs.Button button = new global::Zacs.Button();
            button.Text = text ?? string.Empty;
            button.Click = click;
            button.Hover = global::Zacs.Button.Lighten(button.Background);
            return button;
        }

        public static Toggle Toggle(string text, bool value, Action<MonitorSession, Toggle> changed)
        {
            Toggle toggle = new Toggle();
            toggle.Text = text ?? string.Empty;
            toggle.Value = value;
            toggle.Changed = changed;
            return toggle;
        }

        public static Slider Slider(float value, Action<MonitorSession, Slider> changed)
        {
            Slider slider = new Slider();
            slider.Value = value;
            slider.Changed = changed;
            return slider;
        }

        public static Progress Progress(float value)
        {
            Progress progress = new Progress();
            progress.Value = Mathf.Clamp01(value);
            return progress;
        }

        public static TextField Field(string placeholder, Action<MonitorSession, string> submit)
        {
            TextField field = new TextField();
            field.Placeholder = placeholder ?? string.Empty;
            field.Submit = submit;
            return field;
        }

        public static Window Window(string title, params Widget[] children)
        {
            Window window = new Window(title);
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null)
                        window.Add(children[i]);
                }
            }

            return window;
        }

        public static Widget Separator()
        {
            Widget line = new Widget();
            line.Height = 6f;
            line.HeightUnit = LengthUnit.Pixel;
            line.Background = Palette.Line;
            line.FillCross = true;
            return line;
        }

        public static Widget Spacer()
        {
            Widget spacer = new Widget();
            spacer.Flex = 1f;
            spacer.FillCross = false;
            spacer.Background = Palette.Clear;
            return spacer;
        }

        public static Flow Gap(this Flow flow, float gap)
        {
            if (flow != null)
                flow.Gap = gap;

            return flow;
        }

        public static Window Gap(this Window window, float gap)
        {
            if (window != null)
                window.Body.Gap = gap;

            return window;
        }

        public static T Pad<T>(this T widget, float all) where T : Widget
        {
            if (widget == null)
                return null;

            Window window = widget as Window;
            if (window != null)
                window.Body.Pad = global::Zacs.Pad.All(all);
            else
                widget.Pad = global::Zacs.Pad.All(all);

            return widget;
        }

        public static T Back<T>(this T widget, Color color) where T : Widget
        {
            if (widget != null)
                widget.Background = color;

            return widget;
        }

        public static T Paint<T>(this T widget, Color color) where T : Widget
        {
            if (widget != null)
                widget.Foreground = color;

            return widget;
        }

        public static T Font<T>(this T widget, float size) where T : Widget
        {
            if (widget != null)
                widget.FontSize = size;

            return widget;
        }

        public static T Flex<T>(this T widget, float flex) where T : Widget
        {
            if (widget != null)
                widget.Flex = flex;

            return widget;
        }

        public static T Named<T>(this T widget, string name) where T : Widget
        {
            if (widget != null)
                widget.Name = name;

            return widget;
        }

        public static T Wide<T>(this T widget, float pixels) where T : Widget
        {
            if (widget == null)
                return null;

            widget.Width = pixels;
            widget.WidthUnit = LengthUnit.Pixel;
            return widget;
        }

        public static T Tall<T>(this T widget, float pixels) where T : Widget
        {
            if (widget == null)
                return null;

            widget.Height = pixels;
            widget.HeightUnit = LengthUnit.Pixel;
            return widget;
        }

        static Flow Stack(bool horizontal, Widget[] children)
        {
            Flow flow = new Flow();
            flow.Horizontal = horizontal;
            if (children == null)
                return flow;

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    flow.Add(children[i]);
            }

            return flow;
        }
    }
}
