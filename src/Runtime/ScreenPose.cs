using UnityEngine;

namespace Zacs
{
    internal struct Look
    {
        public bool Hit;
        public float Distance;
        public float Right;
        public float Up;
        public float VirtualX;
        public float VirtualY;
    }

    internal struct ScreenPose
    {
        public bool Valid;
        public Transform Block;
        public Vector3 Center;
        public Vector3 Right;
        public Vector3 Up;
        public Vector3 Out;
        public float Width;
        public float Height;
        public float AxisRight;
        public float AxisUp;
        public float AxisForward;
        public Quaternion WorldRotation;
        public Quaternion TextRotation;
        public int VirtualW;
        public int VirtualH;

        public void Refresh(Transform block, MonitorFace face, float inset, float depth, int virtualHeight)
        {
            Valid = false;
            Block = block;
            if (block == null)
                return;

            Vector3 centerLocal;
            Vector3 outwardLocal;
            Vector3 upLocal;
            Pick(face, out centerLocal, out outwardLocal, out upLocal);

            if (virtualHeight < 480)
                virtualHeight = 480;
            else if (virtualHeight > 2160)
                virtualHeight = 2160;

            Vector3 rawRight = block.TransformVector(Vector3.Cross(upLocal, outwardLocal));
            Vector3 rawUp = block.TransformVector(upLocal);
            Vector3 rawForward = block.TransformVector(outwardLocal);
            AxisRight = rawRight.magnitude;
            AxisUp = rawUp.magnitude;
            AxisForward = rawForward.magnitude;
            if (AxisRight < 0.001f || AxisUp < 0.001f || AxisForward < 0.001f)
                return;

            float limit = Mathf.Min(AxisRight, AxisUp) * 0.45f;
            if (inset < 0f)
                inset = 0f;
            if (inset > limit)
                inset = limit;

            Width = AxisRight - inset * 2f;
            Height = AxisUp - inset * 2f;
            if (Width < 0.02f || Height < 0.02f)
                return;

            Up = rawUp / AxisUp;
            depth = 0.0035f;

            Vector3 axis = rawForward;
            float axisLength = AxisForward;
            Out = axis / axisLength;
            Right = Vector3.Cross(Out, Up);
            float rightLength = Right.magnitude;
            if (rightLength < 0.001f)
                return;

            Right /= rightLength;
            Center = block.TransformPoint(centerLocal) + Out * depth;
            VirtualH = virtualHeight;
            VirtualW = Mathf.RoundToInt(virtualHeight * (Width / Height));
            if (VirtualW < 64)
                VirtualW = 64;

            WorldRotation = Quaternion.LookRotation(Out, Up);
            TextRotation = WorldRotation;
            Valid = true;
        }

        public bool TryHit(Vector3 origin, Vector3 direction, float maxDistance, out Look look)
        {
            look = new Look();
            if (!Valid)
                return false;

            float denom = Vector3.Dot(direction, Out);
            if (denom > -0.08f)
                return false;

            float distance = Vector3.Dot(Center - origin, Out) / denom;
            if (distance < 0.05f || distance > maxDistance)
                return false;

            Vector3 point = origin + direction * distance;
            Vector3 relative = point - Center;
            float x = Vector3.Dot(relative, Right);
            float y = Vector3.Dot(relative, Up);
            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;
            if (x < -halfW || x > halfW || y < -halfH || y > halfH)
                return false;

            look.Hit = true;
            look.Distance = distance;
            look.Right = x;
            look.Up = y;
            look.VirtualX = (x / Width + 0.5f) * VirtualW;
            look.VirtualY = (0.5f - y / Height) * VirtualH;
            return true;
        }

        public void Plate(float x, float y, float w, float h, int layer, out Vector3 worldPos, out Vector3 worldScale)
        {
            float worldW = (w / VirtualW) * Width;
            float worldH = (h / VirtualH) * Height;
            if (worldW < 0.001f)
                worldW = 0.001f;
            if (worldH < 0.001f)
                worldH = 0.001f;

            worldPos = Point(x + w * 0.5f, y + h * 0.5f, layer);
            worldScale = new Vector3(worldW, worldH, 0.00045f);
        }

        public void Glyph(float x, float y, float w, float h, int layer, float unitScale, bool mirror, out Vector3 worldPos, out Vector3 worldScale, out Vector2 display)
        {
            if (w < 1f)
                w = 1f;
            if (h < 1f)
                h = 1f;

            float metersPerPixel = Height / VirtualH;
            float fit = metersPerPixel / 0.09f;
            if (unitScale > 0.05f && unitScale < 5f)
                fit /= unitScale;

            worldPos = Point(x + w * 0.5f, y + h * 0.5f, layer + 2);
            float scaleX = mirror ? fit : -fit;
            worldScale = new Vector3(scaleX, fit, fit);
            display = new Vector2(w, h);
        }

        public float FontUnits(float virtualSize)
        {
            if (virtualSize < 1f)
                return 1f;

            return virtualSize;
        }

        public Vector3 Point(float virtualX, float virtualY, int layer)
        {
            float right = (virtualX / VirtualW - 0.5f) * Width;
            float up = (0.5f - virtualY / VirtualH) * Height;
            return Center + Right * right + Up * up + Out * Lift(layer);
        }

        public float Lift(int layer)
        {
            if (layer < 0)
                layer = 0;
            else if (layer > 28)
                layer = 28;

            return 0.0011f + layer * 0.0022f;
        }

        static void Pick(MonitorFace face, out Vector3 center, out Vector3 outward, out Vector3 up)
        {
            switch (face)
            {
                case MonitorFace.Back:
                    center = new Vector3(0f, 0f, -0.5f);
                    outward = Vector3.back;
                    up = Vector3.up;
                    break;
                case MonitorFace.Left:
                    center = new Vector3(-0.5f, 0f, 0f);
                    outward = Vector3.left;
                    up = Vector3.up;
                    break;
                case MonitorFace.Right:
                    center = new Vector3(0.5f, 0f, 0f);
                    outward = Vector3.right;
                    up = Vector3.up;
                    break;
                case MonitorFace.Up:
                    center = new Vector3(0f, 0.5f, 0f);
                    outward = Vector3.up;
                    up = Vector3.forward;
                    break;
                case MonitorFace.Down:
                    center = new Vector3(0f, -0.5f, 0f);
                    outward = Vector3.down;
                    up = Vector3.back;
                    break;
                default:
                    center = new Vector3(0f, 0f, 0.5f);
                    outward = Vector3.forward;
                    up = Vector3.up;
                    break;
            }
        }
    }
}
