using UnityEngine;

namespace Zacs.Mapping
{
    public enum MonitorFace
    {
        Forward = 0,
        Back = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        Down = 5
    }

    [AddComponentMenu("ZACS/Monitor Object")]
    [DisallowMultipleComponent]
    public class MonitorObject : MonoBehaviour
    {
        [Tooltip("Program opened in-game. Example: zacs.desktop")]
        public string MonitorName = "zacs.desktop";

        [Tooltip("Which face of the cube shows the screen.")]
        public MonitorFace Face = MonitorFace.Forward;

        [Tooltip("Inset from the face edges, in meters.")]
        public float Inset = 0.012f;

        [Tooltip("Distance out from the face, in meters.")]
        public float Depth = 0.008f;

        [Tooltip("State at spawn. With no power button, on stays on and off stays off.")]
        public bool StartsOn = true;

        [Tooltip("Interactable that toggles the monitor.")]
        public InteractableComponent PowerButton;

        void OnValidate()
        {
            if (Inset < 0f)
                Inset = 0f;

            if (Depth < 0f)
                Depth = 0f;

            if (MonitorName != null)
                MonitorName = MonitorName.Trim();
        }

        void OnDrawGizmos()
        {
            DrawFace(false);
        }

        void OnDrawGizmosSelected()
        {
            DrawFace(true);
        }

        void DrawFace(bool selected)
        {
            Vector3 center;
            Vector3 right;
            Vector3 up;
            float width;
            float height;
            if (!TryGetFace(out center, out right, out up, out width, out height))
                return;

            Gizmos.color = selected ? new Color(0.86f, 0.69f, 0.36f, 1f) : new Color(0.86f, 0.69f, 0.36f, 0.45f);
            Vector3 a = center + right * (width * 0.5f) + up * (height * 0.5f);
            Vector3 b = center - right * (width * 0.5f) + up * (height * 0.5f);
            Vector3 c = center - right * (width * 0.5f) - up * (height * 0.5f);
            Vector3 d = center + right * (width * 0.5f) - up * (height * 0.5f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            Gizmos.DrawLine(center, center + up * (height * 0.18f));

#if UNITY_EDITOR
            if (selected)
            {
                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.Label(center, string.IsNullOrEmpty(MonitorName) ? "ZACS" : MonitorName);
            }
#endif
        }

        public bool TryGetFace(out Vector3 center, out Vector3 right, out Vector3 up, out float width, out float height)
        {
            Vector3 centerLocal;
            Vector3 outward;
            Vector3 faceUp;
            Pick(Face, out centerLocal, out outward, out faceUp);
            Quaternion rotation = Quaternion.LookRotation(-outward, faceUp);
            Vector3 rawRight = transform.TransformVector(rotation * Vector3.right);
            Vector3 rawUp = transform.TransformVector(rotation * Vector3.up);
            Vector3 rawForward = transform.TransformVector(rotation * Vector3.forward);
            float fullWidth = rawRight.magnitude;
            float fullHeight = rawUp.magnitude;
            if (fullWidth < 0.001f || fullHeight < 0.001f || rawForward.sqrMagnitude < 0.000001f)
            {
                center = transform.position;
                right = transform.right;
                up = transform.up;
                width = 0f;
                height = 0f;
                return false;
            }

            float inset = Inset;
            float limit = Mathf.Min(fullWidth, fullHeight) * 0.45f;
            if (inset > limit)
                inset = limit;

            width = fullWidth - inset * 2f;
            height = fullHeight - inset * 2f;
            right = rawRight / fullWidth;
            up = rawUp / fullHeight;
            center = transform.TransformPoint(centerLocal) + (-rawForward.normalized) * Mathf.Max(0f, Depth);
            return width > 0.01f && height > 0.01f;
        }

        public static void Pick(MonitorFace face, out Vector3 center, out Vector3 outward, out Vector3 up)
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
