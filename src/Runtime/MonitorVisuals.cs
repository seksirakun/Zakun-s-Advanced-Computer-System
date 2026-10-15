using System.Collections.Generic;
using Log = LabApi.Features.Console.Logger;
using UnityEngine;
using GlyphToy = LabApi.Features.Wrappers.TextToy;
using PrimitiveFlags = AdminToys.PrimitiveFlags;
using PrimitiveToy = LabApi.Features.Wrappers.PrimitiveObjectToy;

namespace Zacs
{
    internal sealed class MonitorVisuals
    {
        readonly List<PlateSlot> _plates = new List<PlateSlot>(32);
        readonly List<GlyphSlot> _glyphs = new List<GlyphSlot>(32);
        readonly List<PlateReq> _plateReqs = new List<PlateReq>(32);
        readonly List<GlyphReq> _glyphReqs = new List<GlyphReq>(32);
        readonly Draw _draw = new Draw();
        PlateSlot _offPlate;
        GlyphSlot _offGlyph;
        PlateSlot _cursorOuter;
        PlateSlot _cursorInner;
        int _stamp;
        bool _plateLimit;
        bool _glyphLimit;
        bool _failed;

        public Draw Begin(ScreenPose pose, ZacsConfig config, Widget hover, Widget pressed, TextField focused, bool caret)
        {
            _plateReqs.Clear();
            _glyphReqs.Clear();
            _draw.Hover = hover;
            _draw.Pressed = pressed;
            _draw.Focused = focused;
            _draw.CaretOn = caret;
            _draw.Plates = _plateReqs;
            _draw.Glyphs = _glyphReqs;
            _draw.TmpScale = 1f;
            return _draw;
        }

        public void Sync(ScreenPose pose, ZacsConfig config, bool powered)
        {
            if (!pose.Valid)
                return;

            if (!powered)
            {
                HidePlates();
                HideGlyphs();
                HideCursor();
                ShowOff(pose, true);
                return;
            }

            ShowOff(pose, false);
            SyncPlates(pose, config);
            SyncGlyphs(pose, config);
        }

        public void SyncCursor(ScreenPose pose, bool show, float right, float up, ZacsConfig config)
        {
            if (!pose.Valid || !show)
            {
                HideCursor();
                return;
            }

            float diameter = config != null ? config.CursorDiameter : 0.018f;
            if (diameter < 0.008f)
                diameter = 0.008f;
            else if (diameter > 0.06f)
                diameter = 0.06f;

            Quaternion rotation = pose.WorldRotation * Quaternion.Euler(90f, 0f, 0f);
            float front = pose.Lift(FrontLayer() + 1);
            Vector3 outerPos = CursorPos(pose, right, up, front);
            Vector3 innerPos = CursorPos(pose, right, up, front + 0.0016f);
            EnsureCursor(ref _cursorOuter, new Color(0.02f, 0.02f, 0.02f, 1f));
            EnsureCursor(ref _cursorInner, new Color(0.96f, 0.96f, 0.96f, 1f));
            PlaceCursor(_cursorOuter, pose, rotation, outerPos, diameter);
            PlaceCursor(_cursorInner, pose, rotation, innerPos, diameter * 0.46f);
        }

        public void Destroy()
        {
            DestroyPlates(_plates);
            DestroyGlyphs(_glyphs);
            DestroyPlate(ref _offPlate);
            DestroyGlyph(ref _offGlyph);
            DestroyPlate(ref _cursorOuter);
            DestroyPlate(ref _cursorInner);
        }

        void SyncPlates(ScreenPose pose, ZacsConfig config)
        {
            int limit = config != null ? config.MaxPlates : 128;
            if (limit < 8)
                limit = 8;

            int stamp = NextStamp();
            int shown = 0;
            for (int i = 0; i < _plateReqs.Count; i++)
            {
                if (shown >= limit)
                {
                    if (!_plateLimit)
                    {
                        _plateLimit = true;
                        Log.Warn("ZACS plate limit reached.");
                    }

                    break;
                }

                PlateReq req = _plateReqs[i];
                int slot = FindPlate(req.Key);
                if (slot < 0)
                    slot = FindFreePlate();

                if (slot < 0)
                {
                    if (_plates.Count >= limit)
                        continue;

                    slot = _plates.Count;
                    _plates.Add(new PlateSlot());
                }

                _plates[slot].Stamp = stamp;
                ApplyPlate(_plates[slot], pose, req);
                shown++;
            }

            for (int i = 0; i < _plates.Count; i++)
            {
                if (_plates[i].Stamp != stamp)
                    HidePlate(_plates[i]);
            }
        }

        void SyncGlyphs(ScreenPose pose, ZacsConfig config)
        {
            int limit = config != null ? config.MaxGlyphs : 96;
            if (limit < 4)
                limit = 4;

            float canvas = config != null && config.TextCanvasScale > 0.00001f ? config.TextCanvasScale : 0.01f;
            bool mirror = config != null && config.MirrorText;
            int stamp = NextStamp();
            int shown = 0;
            for (int i = 0; i < _glyphReqs.Count; i++)
            {
                if (shown >= limit)
                {
                    if (!_glyphLimit)
                    {
                        _glyphLimit = true;
                        Log.Warn("ZACS text limit reached.");
                    }

                    break;
                }

                GlyphReq req = _glyphReqs[i];
                int slot = FindGlyph(req.Key);
                if (slot < 0)
                    slot = FindFreeGlyph();

                if (slot < 0)
                {
                    if (_glyphs.Count >= limit)
                        continue;

                    slot = _glyphs.Count;
                    _glyphs.Add(new GlyphSlot());
                }

                _glyphs[slot].Stamp = stamp;
                ApplyGlyph(_glyphs[slot], pose, req, canvas, mirror);
                shown++;
            }

            for (int i = 0; i < _glyphs.Count; i++)
            {
                if (_glyphs[i].Stamp != stamp)
                    HideGlyph(_glyphs[i]);
            }
        }

        int NextStamp()
        {
            _stamp++;
            if (_stamp == int.MaxValue)
                _stamp = 1;

            return _stamp;
        }

        void ApplyPlate(PlateSlot slot, ScreenPose pose, PlateReq req)
        {
            Vector3 pos;
            Vector3 scale;
            pose.Plate(req.X, req.Y, req.W, req.H, req.Layer, out pos, out scale);
            if (slot.Toy == null)
            {
                slot.Toy = CreatePlate(pose, pos, pose.WorldRotation, scale, req.Color);
                if (slot.Toy == null)
                    return;
            }

            slot.Key = req.Key;
            MovePlate(slot, pos, pose.WorldRotation, scale, req.Color, true);
        }

        void ApplyGlyph(GlyphSlot slot, ScreenPose pose, GlyphReq req, float canvas, bool mirror)
        {
            Vector3 pos;
            Vector3 scale;
            Vector2 display;
            pose.Glyph(req.X, req.Y, req.W, req.H, req.Layer, canvas, mirror, out pos, out scale, out display);
            if (slot.Toy == null)
            {
                slot.Toy = CreateGlyph(pose, pos, pose.TextRotation, scale, display, req.Text);
                if (slot.Toy == null)
                    return;
            }

            slot.Key = req.Key;
            MoveGlyph(slot, pos, pose.TextRotation, scale, display, req.Text, true);
        }

        void ShowOff(ScreenPose pose, bool on)
        {
            if (!on)
            {
                HidePlate(_offPlate);
                HideGlyph(_offGlyph);
                return;
            }

            if (_offPlate == null)
                _offPlate = new PlateSlot();

            if (_offGlyph == null)
                _offGlyph = new GlyphSlot();

            Vector3 pos;
            Vector3 scale;
            pose.Plate(0f, 0f, pose.VirtualW, pose.VirtualH, 0, out pos, out scale);
            if (_offPlate.Toy == null)
                _offPlate.Toy = CreatePlate(pose, pos, pose.WorldRotation, scale, Palette.Background);

            MovePlate(_offPlate, pos, pose.WorldRotation, scale, Palette.Background, true);

            Vector3 textPos;
            Vector3 textScale;
            Vector2 display;
            pose.Glyph(pose.VirtualW * 0.2f, pose.VirtualH * 0.42f, pose.VirtualW * 0.6f, pose.VirtualH * 0.16f, 2, 1f, false, out textPos, out textScale, out display);
            string text = RichText.Format("Power off", Palette.Muted, pose.FontUnits(42f), TextAlign.Center, true);
            if (_offGlyph.Toy == null)
                _offGlyph.Toy = CreateGlyph(pose, textPos, pose.TextRotation, textScale, display, text);

            MoveGlyph(_offGlyph, textPos, pose.TextRotation, textScale, display, text, true);
        }

        void EnsureCursor(ref PlateSlot slot, Color color)
        {
            if (slot == null)
                slot = new PlateSlot { Color = color };
        }

        void PlaceCursor(PlateSlot slot, ScreenPose pose, Quaternion rotation, Vector3 world, float diameter)
        {
            if (slot == null)
                return;

            Vector3 pos = world;
            Vector3 scale = new Vector3(diameter, 0.0012f, diameter);

            if (slot.Toy == null)
            {
                slot.Toy = CreatePlate(pose, pos, rotation, scale, slot.Color);
                if (slot.Toy == null)
                    return;

                slot.Toy.Base.NetworkMovementSmoothing = 28;
                slot.Toy.SyncInterval = 0.04f;
            }

            MovePlate(slot, pos, rotation, scale, slot.Color, true);
        }

        Vector3 CursorPos(ScreenPose pose, float right, float up, float lift)
        {
            return pose.Center + pose.Right * right + pose.Up * up + pose.Out * lift;
        }

        int FrontLayer()
        {
            int top = 2;
            for (int i = 0; i < _plateReqs.Count; i++)
            {
                if (_plateReqs[i].Layer > top)
                    top = _plateReqs[i].Layer;
            }

            for (int i = 0; i < _glyphReqs.Count; i++)
            {
                int layer = _glyphReqs[i].Layer + 2;
                if (layer > top)
                    top = layer;
            }

            return top;
        }

        PrimitiveToy CreatePlate(ScreenPose pose, Vector3 pos, Quaternion rotation, Vector3 scale, Color color)
        {
            if (_failed || pose.Block == null)
                return null;

            try
            {
                PrimitiveToy toy = PrimitiveToy.Create(pos, rotation, scale, null, true);
                toy.Type = PrimitiveType.Cube;
                toy.Color = color;
                toy.Flags = PrimitiveFlags.Visible;
                toy.Base.NetworkMovementSmoothing = 0;
                toy.SyncInterval = 0.12f;
                return toy;
            }
            catch (System.Exception ex)
            {
                if (!_failed)
                {
                    _failed = true;
                    Log.Warn("ZACS screen draw failed: " + ex);
                }

                return null;
            }
        }

        GlyphToy CreateGlyph(ScreenPose pose, Vector3 pos, Quaternion rotation, Vector3 scale, Vector2 display, string text)
        {
            if (_failed || pose.Block == null)
                return null;

            try
            {
                GlyphToy toy = GlyphToy.Create(pos, rotation, scale, null, true);
                toy.DisplaySize = display;
                toy.TextFormat = text ?? string.Empty;
                toy.Base.NetworkMovementSmoothing = 0;
                toy.SyncInterval = 0.12f;
                return toy;
            }
            catch (System.Exception ex)
            {
                if (!_failed)
                {
                    _failed = true;
                    Log.Warn("ZACS text draw failed: " + ex);
                }

                return null;
            }
        }

        static void MovePlate(PlateSlot slot, Vector3 pos, Quaternion rotation, Vector3 scale, Color color, bool visible)
        {
            if (slot == null || slot.Toy == null || slot.Toy.IsDestroyed)
                return;

            if (slot.Toy.Parent != null)
                slot.Toy.Parent = null;

            if (!Near(slot.LocalPos, pos))
            {
                slot.Toy.Position = pos;
                slot.LocalPos = pos;
            }

            if (Quaternion.Dot(slot.LocalRot, rotation) < 0.9995f)
            {
                slot.Toy.Rotation = rotation;
                slot.LocalRot = rotation;
            }

            if (!Near(slot.LocalScale, scale))
            {
                slot.Toy.Scale = scale;
                slot.LocalScale = scale;
            }

            if (!Same(slot.Color, color))
            {
                slot.Toy.Color = color;
                slot.Color = color;
            }

            if (slot.Visible != visible)
            {
                slot.Toy.Flags = visible ? PrimitiveFlags.Visible : PrimitiveFlags.None;
                slot.Visible = visible;
            }
        }

        static void MoveGlyph(GlyphSlot slot, Vector3 pos, Quaternion rotation, Vector3 scale, Vector2 display, string text, bool visible)
        {
            if (slot == null || slot.Toy == null || slot.Toy.IsDestroyed)
                return;

            if (slot.Toy.Parent != null)
                slot.Toy.Parent = null;

            if (!Near(slot.LocalPos, pos))
            {
                slot.Toy.Position = pos;
                slot.LocalPos = pos;
            }

            if (Quaternion.Dot(slot.LocalRot, rotation) < 0.9995f)
            {
                slot.Toy.Rotation = rotation;
                slot.LocalRot = rotation;
            }

            if (!Near(slot.LocalScale, scale))
            {
                slot.Toy.Scale = scale;
                slot.LocalScale = scale;
            }

            if ((slot.Display - display).sqrMagnitude > 0.01f)
            {
                slot.Toy.DisplaySize = display;
                slot.Display = display;
            }

            if (!string.Equals(slot.Text, text, System.StringComparison.Ordinal))
            {
                slot.Toy.TextFormat = text ?? string.Empty;
                slot.Text = text;
            }

            if (slot.Visible != visible)
                slot.Visible = visible;

            if (!visible)
            {
                if (!string.IsNullOrEmpty(slot.Text))
                {
                    slot.Toy.TextFormat = string.Empty;
                    slot.Text = string.Empty;
                }

                if (slot.Display.sqrMagnitude > 0.01f)
                {
                    slot.Toy.DisplaySize = Vector2.zero;
                    slot.Display = Vector2.zero;
                }
            }
        }

        void HidePlates()
        {
            for (int i = 0; i < _plates.Count; i++)
                HidePlate(_plates[i]);
        }

        void HideGlyphs()
        {
            for (int i = 0; i < _glyphs.Count; i++)
                HideGlyph(_glyphs[i]);
        }

        void HideCursor()
        {
            HidePlate(_cursorOuter);
            HidePlate(_cursorInner);
        }

        static void HidePlate(PlateSlot slot)
        {
            if (slot == null || slot.Toy == null || !slot.Visible)
                return;

            slot.Toy.Flags = PrimitiveFlags.None;
            slot.Visible = false;
            slot.Key = -1;
        }

        static void HideGlyph(GlyphSlot slot)
        {
            if (slot == null || slot.Toy == null || !slot.Visible)
                return;

            slot.Toy.TextFormat = string.Empty;
            slot.Text = string.Empty;
            slot.Visible = false;
            slot.Key = -1;
        }

        int FindPlate(int key)
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                if (_plates[i].Key == key)
                    return i;
            }

            return -1;
        }

        int FindGlyph(int key)
        {
            for (int i = 0; i < _glyphs.Count; i++)
            {
                if (_glyphs[i].Key == key)
                    return i;
            }

            return -1;
        }

        int FindFreePlate()
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                if (_plates[i].Key < 0)
                    return i;
            }

            return -1;
        }

        int FindFreeGlyph()
        {
            for (int i = 0; i < _glyphs.Count; i++)
            {
                if (_glyphs[i].Key < 0)
                    return i;
            }

            return -1;
        }

        static void DestroyPlates(List<PlateSlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
                DestroyToy(slots[i].Toy);

            slots.Clear();
        }

        static void DestroyGlyphs(List<GlyphSlot> slots)
        {
            for (int i = 0; i < slots.Count; i++)
                DestroyToy(slots[i].Toy);

            slots.Clear();
        }

        static void DestroyPlate(ref PlateSlot slot)
        {
            if (slot != null)
                DestroyToy(slot.Toy);

            slot = null;
        }

        static void DestroyGlyph(ref GlyphSlot slot)
        {
            if (slot != null)
                DestroyToy(slot.Toy);

            slot = null;
        }

        static void DestroyToy(LabApi.Features.Wrappers.AdminToy toy)
        {
            if (toy == null || toy.IsDestroyed)
                return;

            toy.Destroy();
        }

        static bool Near(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude < 0.0000004f;
        }

        static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.004f
                && Mathf.Abs(a.g - b.g) < 0.004f
                && Mathf.Abs(a.b - b.b) < 0.004f
                && Mathf.Abs(a.a - b.a) < 0.004f;
        }

        sealed class PlateSlot
        {
            public int Key = -1;
            public int Stamp;
            public PrimitiveToy Toy;
            public Vector3 LocalPos;
            public Quaternion LocalRot = Quaternion.identity;
            public Vector3 LocalScale;
            public Color Color;
            public bool Visible;
        }

        sealed class GlyphSlot
        {
            public int Key = -1;
            public int Stamp;
            public GlyphToy Toy;
            public Vector3 LocalPos;
            public Quaternion LocalRot = Quaternion.identity;
            public Vector3 LocalScale;
            public Vector2 Display;
            public string Text = string.Empty;
            public bool Visible;
        }
    }
}
